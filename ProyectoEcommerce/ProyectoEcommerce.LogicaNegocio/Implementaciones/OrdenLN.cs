using System.Data;
using System.Net.Mail;
using System.Text.RegularExpressions;
using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProyectoEcommerce.Dominio.Entidades;
using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Dominio.InterfacesAD;
using ProyectoEcommerce.Dominio.InterfazLN;
using ProyectoEcommerce.Recursos;
using ProyectoEcommerce.Utilidades;

namespace ProyectoEcommerce.LogicaNegocio.Implementaciones;

// aqui se coordina checkout, venta, inventario, factura y correo
// la compra usa SQL directo y una transaccion para no dejar ordenes o stock a medias
public class OrdenLN : IOrdenLN
{
    // PROFORMA es cotizacion, PENDIENTE todavia no confirma y CONFIRMADA ya resto inventario
    // FACTURADA tiene PDF registrado y CANCELADA ya no sigue el flujo
    private static readonly string[] EstadosPermitidos = ["PROFORMA", "PENDIENTE", "CONFIRMADA", "FACTURADA", "CANCELADA"];
    private readonly IUnidadTrabajoEF _unidadDeTrabajo;
    private readonly ILogger<OrdenLN> _logger;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;
    private readonly ICarritoLN _carritoLN;
    private readonly IFacturaLN _facturaLN;
    private readonly ICorreoFacturaLN _correoFacturaLN;
    private readonly IWebHostEnvironment _entorno;

    public OrdenLN(
        IUnidadTrabajoEF unidadTrabajo,
        ILogger<OrdenLN> logger,
        IMapper mapper,
        IConfiguration configuration,
        ICarritoLN carritoLN,
        IFacturaLN facturaLN,
        ICorreoFacturaLN correoFacturaLN,
        IWebHostEnvironment entorno)
    {
        _unidadDeTrabajo = unidadTrabajo;
        _logger = logger;
        _mapper = mapper;
        _configuration = configuration;
        _carritoLN = carritoLN;
        _facturaLN = facturaLN;
        _correoFacturaLN = correoFacturaLN;
        _entorno = entorno;
    }

    // recibe el usuario del JWT y junta sus datos con el carrito actual
    // solo prepara la pantalla, todavia no crea una orden ni resta stock
    public async Task<Respuesta<TCheckoutPreparacion>> PrepararCheckoutAsync(int usuarioId)
    {
        if (usuarioId <= 0) return Error<TCheckoutPreparacion>(Mensajes.SesionInvalidaCarrito);
        try
        {
            // busca un usuario activo y despues pide al carrito sus precios y descuentos actuales
            var usuario = await _unidadDeTrabajo.TUsuario.ObtenerEntidadAsync(x => x.UsuarioId == usuarioId && x.Activo);
            if (usuario.Data == null || !string.IsNullOrEmpty(usuario.Error))
                return Error<TCheckoutPreparacion>(Mensajes.SesionInvalidaCarrito);

            var carrito = await _carritoLN.ObtenerActualAsync(usuarioId);
            if (carrito.Data == null || !string.IsNullOrEmpty(carrito.Error))
                return Error<TCheckoutPreparacion>(carrito.Error.Length > 0 ? carrito.Error : Mensajes.ErrorCarrito);

            return new Respuesta<TCheckoutPreparacion>
            {
                Data = new TCheckoutPreparacion
                {
                    Cliente = new TCheckoutCliente
                    {
                        NombreCompleto = $"{usuario.Data.Nombre} {usuario.Data.Apellidos}".Trim(),
                        Correo = usuario.Data.Correo,
                        Telefono = usuario.Data.Telefono,
                        Direccion = usuario.Data.Direccion
                    },
                    Carrito = carrito.Data
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al preparar el checkout del usuario autenticado.");
            return Error<TCheckoutPreparacion>("No fue posible preparar la compra.");
        }
    }

    // valida el formulario, confirma venta e inventario y despues genera PDF y manda correo
    // devuelve numero, total y si cada paso posterior se pudo completar
    public async Task<Respuesta<TCompraCompletada>> ConfirmarCompraAsync(
        TConfirmarCompra datos,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        // limpia correo, direccion y metodo antes de abrir conexion o transaccion
        var validacion = ValidarConfirmacion(datos, usuarioId);
        if (validacion != null) return Error<TCompraCompletada>(validacion);

        TFacturaDatos factura;
        // primero confirma venta e inventario
        // un fallo posterior del PDF o SMTP no devuelve una compra que ya fue confirmada
        try
        {
            factura = await CrearVentaAsync(datos, usuarioId, cancellationToken);
        }
        // el procedimiento usa el numero 51001 para avisar que el stock cambio
        catch (SqlException ex) when (ex.Number == 51001)
        {
            _logger.LogWarning("Stock insuficiente al confirmar el carrito del UsuarioId {UsuarioId}.", usuarioId);
            return Error<TCompraCompletada>("Uno o más productos ya no tienen stock suficiente. Revisa tu carrito.");
        }
        catch (CompraInvalidaException ex)
        {
            return Error<TCompraCompletada>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transaccional al confirmar la compra del UsuarioId {UsuarioId}.", usuarioId);
            return Error<TCompraCompletada>("No fue posible confirmar la compra. No se creó una orden incompleta.");
        }

        var facturaGenerada = false;
        var correoEnviado = false;
        var mensaje = "Compra realizada correctamente.";
        // arma una ruta conocida dentro de wwwroot para guardar y luego descargar el PDF
        var nombreArchivo = $"Factura-{factura.NumeroOrden}.pdf";
        var rutaRelativa = $"documentos/facturas/{nombreArchivo}";
        var raizWeb = _entorno.WebRootPath ?? Path.Combine(_entorno.ContentRootPath, "wwwroot");
        var rutaAbsoluta = Path.Combine(raizWeb, "documentos", "facturas", nombreArchivo);

        try
        {
            // CreateDirectory tambien funciona cuando la carpeta ya existe
            Directory.CreateDirectory(Path.GetDirectoryName(rutaAbsoluta)!);
            var contenido = _facturaLN.Generar(factura);
            // guarda los bytes del PDF y despues registra la ruta en la tabla Documentos
            await File.WriteAllBytesAsync(rutaAbsoluta, contenido, cancellationToken);
            await RegistrarFacturaAsync(factura, rutaRelativa, usuarioId, cancellationToken);
            facturaGenerada = true;

            // SMTP se intenta solamente cuando el PDF ya existe y esta registrado
            var correo = await _correoFacturaLN.EnviarAsync(factura, rutaAbsoluta, cancellationToken);
            correoEnviado = correo.Enviado;
            if (correoEnviado)
                await MarcarCorreoEnviadoAsync(factura.OrdenId, usuarioId, cancellationToken);
            else
                mensaje = "La compra fue realizada, pero no fue posible enviar la factura por correo.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "La OrdenId {OrdenId} fue confirmada, pero falló su facturación posterior.", factura.OrdenId);
            mensaje = "La compra fue realizada, pero no fue posible completar la generación de la factura.";
            // si quedo un archivo incompleto intenta limpiarlo sin esconder el error principal
            if (!facturaGenerada && File.Exists(rutaAbsoluta))
            {
                try { File.Delete(rutaAbsoluta); }
                catch (Exception limpiezaEx) {
                _logger.LogWarning(limpiezaEx, "No fue posible limpiar un PDF incompleto de OrdenId {OrdenId}.", factura.OrdenId); 
                }
            }
        }

        return new Respuesta<TCompraCompletada>
        {
            Data = new TCompraCompletada
            {
                OrdenId = factura.OrdenId,
                NumeroOrden = factura.NumeroOrden,
                Total = factura.Total,
                CorreoDestino = factura.Correo,
                FacturaGenerada = facturaGenerada,
                CorreoEnviado = correoEnviado,
                Mensaje = mensaje
            }
        };
    }

    // crea Orden, detalles y pago, confirma inventario y cierra carrito dentro de una sola transaccion
    // devuelve los datos historicos que luego se usan para armar la factura
    private async Task<TFacturaDatos> CrearVentaAsync(
        TConfirmarCompra datos,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        // await using cierra conexion y transaccion aunque el metodo salga por una excepcion
        await using var conexion = new SqlConnection(CadenaConexion());
        await conexion.OpenAsync(cancellationToken);
        // Serializable evita que dos compras confirmen al mismo tiempo el mismo stock disponible
        await using var transaccion = (SqlTransaction)await conexion.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        try
        {
            // cada consulta usa la misma conexion y transaccion para ver un estado consistente
            var cliente = await ConsultarClienteAsync(conexion, transaccion, usuarioId, cancellationToken)
                ?? throw new CompraInvalidaException(Mensajes.SesionInvalidaCarrito);
            var carritoId = await ConsultarCarritoAbiertoAsync(conexion, transaccion, usuarioId, cancellationToken);
            if (!carritoId.HasValue)
                throw new CompraInvalidaException("Tu carrito está vacío.");

            // vuelve a leer producto, precio e inventario, nunca confia en importes enviados por Angular
            var items = await ConsultarItemsCompraAsync(conexion, transaccion, carritoId.Value, cancellationToken);
            if (items.Count == 0)
                throw new CompraInvalidaException("Tu carrito está vacío.");
            // Any devuelve true apenas encuentra un producto invalido o sin stock suficiente
            if (items.Any(x => !x.ProductoActivo || x.Cantidad > x.StockDisponible))
                throw new CompraInvalidaException("Uno o más productos ya no tienen stock suficiente. Revisa tu carrito.");

            // suma los valores recalculados en el servidor para formar la cabecera de la orden
            var subtotal = items.Sum(x => x.Subtotal);
            var descuentos = items.Sum(x => x.Descuento);
            var impuestos = items.Sum(x => x.Impuestos);
            var total = items.Sum(x => x.TotalLinea);
            var fecha = DateTime.UtcNow;

            // OUTPUT devuelve el ID y la fecha exacta que SQL puso al crear la orden
            var insertarOrden = CrearComando(conexion, transaccion, """
                INSERT INTO dbo.Ordenes (UsuarioId, FechaOrden, Estado, TipoOrden, DireccionEnvio, Moneda, Total, DescuentoTotal)
                OUTPUT INSERTED.OrdenId, INSERTED.FechaOrden
                VALUES (@UsuarioId, SYSDATETIME(), N'PENDIENTE', N'VENTA', @DireccionEnvio, 'CRC', @Total, @DescuentoTotal);
                """);
            insertarOrden.Parameters.AddWithValue("@UsuarioId", usuarioId);
            insertarOrden.Parameters.AddWithValue("@DireccionEnvio", datos.DireccionEnvio);
            insertarOrden.Parameters.Add(DecimalParametro("@Total", total));
            insertarOrden.Parameters.Add(DecimalParametro("@DescuentoTotal", descuentos));
            int ordenId;
            // ExecuteReaderAsync se usa porque el INSERT devuelve dos valores con OUTPUT
            await using (var lector = await insertarOrden.ExecuteReaderAsync(cancellationToken))
            {
                if (!await lector.ReadAsync(cancellationToken)) throw new InvalidOperationException("No se creó la orden.");
                ordenId = lector.GetInt32(0);
                fecha = lector.GetDateTime(1);
            }

            // recorre cada item y guarda los valores historicos usados en esta compra
            foreach (var item in items)
            {
                var insertarDetalle = CrearComando(conexion, transaccion, """
                    INSERT INTO dbo.OrdenDetalle
                        (OrdenId, ProductoId, Cantidad, PrecioUnitario, PorcentajeImpuesto,
                         PorcentajeDescuento, Subtotal, TotalLinea)
                    VALUES
                        (@OrdenId, @ProductoId, @Cantidad, @PrecioUnitario, @PorcentajeImpuesto,
                         @PorcentajeDescuento, @Subtotal, @TotalLinea);
                    """);
                insertarDetalle.Parameters.AddWithValue("@OrdenId", ordenId);
                insertarDetalle.Parameters.AddWithValue("@ProductoId", item.ProductoId);
                insertarDetalle.Parameters.AddWithValue("@Cantidad", item.Cantidad);
                insertarDetalle.Parameters.Add(DecimalParametro("@PrecioUnitario", item.PrecioUnitario));
                insertarDetalle.Parameters.Add(DecimalParametro("@PorcentajeImpuesto", item.PorcentajeImpuesto, 5));
                insertarDetalle.Parameters.Add(DecimalParametro("@PorcentajeDescuento", item.PorcentajeDescuento, 5));
                insertarDetalle.Parameters.Add(DecimalParametro("@Subtotal", item.Subtotal));
                insertarDetalle.Parameters.Add(DecimalParametro("@TotalLinea", item.TotalLinea));
                // ExecuteNonQueryAsync ejecuta el INSERT y no espera filas como respuesta
                await insertarDetalle.ExecuteNonQueryAsync(cancellationToken);
            }

            // el pago es una simulacion academica y se deja PENDIENTE sin procesar dinero real
            var insertarPago = CrearComando(conexion, transaccion, """
                INSERT INTO dbo.Pagos (OrdenId, Fecha, Monto, Metodo, Estado, Referencia)
                VALUES (@OrdenId, SYSDATETIME(), @Monto, @Metodo, N'PENDIENTE',
                        N'Simulación académica sin procesamiento financiero real.');
                """);
            insertarPago.Parameters.AddWithValue("@OrdenId", ordenId);
            insertarPago.Parameters.Add(DecimalParametro("@Monto", total));
            insertarPago.Parameters.AddWithValue("@Metodo", datos.MetodoPago);
            await insertarPago.ExecuteNonQueryAsync(cancellationToken);

            // el stored procedure vuelve a revisar stock, evita sobreventa, resta inventario y confirma
            var confirmar = CrearComando(conexion, transaccion, "dbo.sp_ConfirmarOrdenVenta");
            confirmar.CommandType = CommandType.StoredProcedure;
            confirmar.Parameters.AddWithValue("@OrdenId", ordenId);
            confirmar.Parameters.AddWithValue("@UsuarioId", usuarioId);
            await confirmar.ExecuteNonQueryAsync(cancellationToken);

            // el carrito se marca CONVERTIDO dentro de la misma transaccion
            var cerrarCarrito = CrearComando(conexion, transaccion, """
                UPDATE dbo.Carritos SET Estado = N'CONVERTIDO'
                WHERE CarritoId = @CarritoId AND UsuarioId = @UsuarioId AND Estado = N'ACTIVO';
                """);
            cerrarCarrito.Parameters.AddWithValue("@CarritoId", carritoId.Value);
            cerrarCarrito.Parameters.AddWithValue("@UsuarioId", usuarioId);
            if (await cerrarCarrito.ExecuteNonQueryAsync(cancellationToken) != 1)
                throw new InvalidOperationException("No se pudo cerrar el carrito convertido.");

            // Commit deja definitivos orden, detalles, pago, inventario y cierre del carrito juntos
            await transaccion.CommitAsync(cancellationToken);

            return new TFacturaDatos
            {
                OrdenId = ordenId,
                NumeroOrden = ordenId.ToString("D6"),
                NumeroFactura = $"FAC-{ordenId:D6}",
                Fecha = fecha,
                Cliente = cliente.NombreCompleto,
                Correo = datos.CorreoDestino,
                DireccionEnvio = datos.DireccionEnvio,
                MetodoPago = datos.MetodoPago,
                Subtotal = subtotal,
                Impuestos = impuestos,
                Descuentos = descuentos,
                Total = total,
                Items = items
            };
        }
        catch
        {
            // cualquier error antes del Commit hace Rollback para no guardar la venta a medias
            if (transaccion.Connection != null) await transaccion.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    // bloquea y trae el Cliente activo durante la confirmacion
    private static async Task<ClienteCompra?> ConsultarClienteAsync(
        SqlConnection conexion, SqlTransaction transaccion, int usuarioId, CancellationToken cancellationToken)
    {
        var comando = CrearComando(conexion, transaccion, """
            SELECT Nombre, Apellidos
            -- UPDLOCK y HOLDLOCK conservan el bloqueo hasta terminar la transaccion
            FROM dbo.Usuarios WITH (UPDLOCK, HOLDLOCK)
            WHERE UsuarioId = @UsuarioId AND Activo = 1;
            """);
        comando.Parameters.AddWithValue("@UsuarioId", usuarioId);
        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        if (!await lector.ReadAsync(cancellationToken)) return null;
        return new ClienteCompra { NombreCompleto = $"{lector.GetString(0)} {lector.GetString(1)}".Trim() };
    }

    // busca y bloquea el carrito ACTIVO del usuario, devuelve null si esta vacio o ya se convirtio
    private static async Task<int?> ConsultarCarritoAbiertoAsync(
        SqlConnection conexion, SqlTransaction transaccion, int usuarioId, CancellationToken cancellationToken)
    {
        var comando = CrearComando(conexion, transaccion, """
            SELECT CarritoId FROM dbo.Carritos WITH (UPDLOCK, HOLDLOCK)
            WHERE UsuarioId = @UsuarioId AND Estado = N'ACTIVO';
            """);
        comando.Parameters.AddWithValue("@UsuarioId", usuarioId);
        // ExecuteScalarAsync trae solamente el primer valor de la primera fila
        var valor = await comando.ExecuteScalarAsync(cancellationToken);
        return valor == null || valor == DBNull.Value ? null : Convert.ToInt32(valor);
    }

    // bloquea y lee los productos del carrito para validar stock y capturar precios actuales
    // despues busca candidatos de descuento y calcula cada linea
    private static async Task<List<TFacturaItemCompra>> ConsultarItemsCompraAsync(
        SqlConnection conexion, SqlTransaction transaccion, int carritoId, CancellationToken cancellationToken)
    {
        var comando = CrearComando(conexion, transaccion, """
            SELECT cd.ProductoId, p.Nombre, cd.Cantidad, p.PrecioVenta, p.Stock, p.Activo,
                   CASE WHEN i.Activo = 1
                              AND i.FechaInicio <= CONVERT(date, SYSDATETIME())
                              AND (i.FechaFin IS NULL OR i.FechaFin >= CONVERT(date, SYSDATETIME()))
                        THEN i.Porcentaje ELSE CONVERT(decimal(5,2), 0) END AS PorcentajeImpuesto
            FROM dbo.CarritoDetalle cd
            INNER JOIN dbo.Productos p WITH (UPDLOCK, HOLDLOCK) ON p.ProductoId = cd.ProductoId
            INNER JOIN dbo.Impuestos i ON i.ImpuestoId = p.ImpuestoId
            WHERE cd.CarritoId = @CarritoId
            ORDER BY cd.CarritoDetalleId;
            """);
        comando.Parameters.AddWithValue("@CarritoId", carritoId);
        var items = new List<TFacturaItemCompra>();
        await using (var lector = await comando.ExecuteReaderAsync(cancellationToken))
        {
            // ReadAsync avanza una fila a la vez hasta terminar los productos del carrito
            while (await lector.ReadAsync(cancellationToken))
            {
                var cantidad = lector.GetInt32(2);
                var precio = lector.GetDecimal(3);
                var porcentajeImpuesto = lector.GetDecimal(6);
                items.Add(new TFacturaItemCompra
                {
                    ProductoId = lector.GetInt32(0),
                    Nombre = lector.GetString(1),
                    Cantidad = cantidad,
                    PrecioUnitario = precio,
                    StockDisponible = lector.GetInt32(4),
                    ProductoActivo = lector.GetBoolean(5),
                    PorcentajeImpuesto = porcentajeImpuesto,
                    PorcentajeDescuento = 0m
                });
            }
        }

        // crea una lista vacia de candidatos por producto para llenarla con la segunda consulta
        var candidatosPorProducto = items.ToDictionary(x => x.ProductoId, _ => new List<TDescuentoCandidato>());
        var descuentosComando = CrearComando(conexion, transaccion, """
            SELECT cd.ProductoId, d.DescuentoId, d.TipoDescuento, d.Nombre, d.Porcentaje
            FROM dbo.CarritoDetalle cd
            INNER JOIN dbo.Productos p ON p.ProductoId = cd.ProductoId
            INNER JOIN dbo.Categorias c ON c.CategoriaId = p.CategoriaId
            INNER JOIN dbo.Descuentos d ON
                (d.TipoDescuento IN (N'PRODUCTO', N'PROMOCIONAL') AND d.ProductoId = p.ProductoId) OR
                (d.TipoDescuento = N'CATEGORIA' AND d.CategoriaId = p.CategoriaId) OR
                (d.TipoDescuento = N'FAMILIA' AND d.FamiliaId = c.FamiliaId)
            WHERE cd.CarritoId = @CarritoId
              AND d.Activo = 1
              AND d.FechaInicio <= SYSDATETIME()
              AND d.FechaFin >= SYSDATETIME();
            """);
        descuentosComando.Parameters.AddWithValue("@CarritoId", carritoId);
        await using (var lectorDescuentos = await descuentosComando.ExecuteReaderAsync(cancellationToken))
        {
            while (await lectorDescuentos.ReadAsync(cancellationToken))
            {
                var productoId = lectorDescuentos.GetInt32(0);
                // TryGetValue encuentra la lista correcta y continue ignora un ID inesperado
                if (!candidatosPorProducto.TryGetValue(productoId, out var candidatos)) continue;
                candidatos.Add(new TDescuentoCandidato
                {
                    DescuentoId = lectorDescuentos.GetInt32(1),
                    TipoDescuento = lectorDescuentos.GetString(2),
                    Nombre = lectorDescuentos.GetString(3),
                    Porcentaje = lectorDescuentos.GetDecimal(4)
                });
            }
        }

        // recorre los productos y deja precio, descuento e impuesto listos para guardar
        foreach (var item in items)
        {
            var descuento = ResolucionDescuentos.Calcular(
                item.ProductoId,
                item.PrecioUnitario,
                candidatosPorProducto[item.ProductoId]);
            var desglose = CalculoPrecioIncluido.Calcular(
                item.PrecioUnitario,
                item.Cantidad,
                item.PorcentajeImpuesto,
                descuento.Porcentaje);
            item.PorcentajeDescuento = descuento.Porcentaje;
            item.Subtotal = desglose.Subtotal;
            item.Impuestos = desglose.Impuestos;
            item.Descuento = desglose.Descuento;
            item.TotalLinea = desglose.Total;
        }
        return items;
    }

    // cambia la orden de CONFIRMADA a FACTURADA y registra numero, ruta y correo del PDF
    // usa otra transaccion porque este paso ocurre despues de confirmar la compra
    private async Task RegistrarFacturaAsync(
        TFacturaDatos factura,
        string rutaRelativa,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        await using var conexion = new SqlConnection(CadenaConexion());
        await conexion.OpenAsync(cancellationToken);
        await using var transaccion = (SqlTransaction)await conexion.BeginTransactionAsync(cancellationToken);
        try
        {
            var actualizar = CrearComando(conexion, transaccion, """
                UPDATE dbo.Ordenes SET Estado = N'FACTURADA'
                WHERE OrdenId = @OrdenId AND Estado = N'CONFIRMADA';
                """);
            actualizar.Parameters.AddWithValue("@OrdenId", factura.OrdenId);
            // debe cambiar exactamente una fila para asegurar que la orden estaba confirmada
            if (await actualizar.ExecuteNonQueryAsync(cancellationToken) != 1)
                throw new InvalidOperationException("La orden no estaba confirmada para facturar.");

            var documento = CrearComando(conexion, transaccion, """
                INSERT INTO dbo.Documentos
                    (OrdenId, CompraProveedorId, Tipo, Numero, Ruta, CorreoDestino, EnviadoCorreo, FechaCreacion)
                VALUES
                    (@OrdenId, NULL, N'FACTURA', @Numero, @Ruta, @CorreoDestino, 0, SYSDATETIME());
                """);
            documento.Parameters.AddWithValue("@OrdenId", factura.OrdenId);
            documento.Parameters.AddWithValue("@Numero", factura.NumeroFactura);
            documento.Parameters.AddWithValue("@Ruta", rutaRelativa);
            documento.Parameters.AddWithValue("@CorreoDestino", factura.Correo);
            await documento.ExecuteNonQueryAsync(cancellationToken);

            await InsertarBitacoraAsync(conexion, transaccion, usuarioId, "GENERAR_FACTURA", factura.OrdenId,
                $"Documento {factura.NumeroFactura} generado.", cancellationToken);
            // confirma estado, documento y bitacora juntos
            await transaccion.CommitAsync(cancellationToken);
        }
        catch
        {
            if (transaccion.Connection != null) await transaccion.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    // despues de que SMTP responde bien marca EnviadoCorreo y deja la bitacora
    private async Task MarcarCorreoEnviadoAsync(int ordenId, int usuarioId, CancellationToken cancellationToken)
    {
        await using var conexion = new SqlConnection(CadenaConexion());
        await conexion.OpenAsync(cancellationToken);
        await using var transaccion = (SqlTransaction)await conexion.BeginTransactionAsync(cancellationToken);
        try
        {
            var documento = CrearComando(conexion, transaccion, """
                UPDATE dbo.Documentos SET EnviadoCorreo = 1
                WHERE OrdenId = @OrdenId AND Tipo = N'FACTURA';
                """);
            documento.Parameters.AddWithValue("@OrdenId", ordenId);
            await documento.ExecuteNonQueryAsync(cancellationToken);
            await InsertarBitacoraAsync(conexion, transaccion, usuarioId, "ENVIAR_FACTURA_CORREO", ordenId,
                "Factura enviada mediante SMTP.", cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
        }
        catch
        {
            if (transaccion.Connection != null) await transaccion.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    // pasa el UsuarioId para que la consulta agregue el filtro de dueño
    public Task<Respuesta<TPagina<TOrdenResumen>>> ListarClienteAsync(TFiltroOrdenes filtro, int usuarioId) =>
        ListarOrdenesAsync(filtro, usuarioId);

    // pasa null para que el Administrador vea ordenes de todos los clientes
    public Task<Respuesta<TPagina<TOrdenResumen>>> ListarAdministracionAsync(TFiltroOrdenes filtro) =>
        ListarOrdenesAsync(filtro, null);

    // arma una consulta SQL con los filtros permitidos y devuelve total mas una pagina de resumenes
    private async Task<Respuesta<TPagina<TOrdenResumen>>> ListarOrdenesAsync(TFiltroOrdenes filtro, int? usuarioId)
    {
        NormalizarFiltro(filtro);
        if (!string.IsNullOrEmpty(filtro.Estado) && !EstadosPermitidos.Contains(filtro.Estado))
            return Error<TPagina<TOrdenResumen>>("El estado indicado no es válido.");
        if (filtro.FechaDesde.HasValue && filtro.FechaHasta.HasValue && filtro.FechaHasta < filtro.FechaDesde)
            return Error<TPagina<TOrdenResumen>>("La fecha final no puede ser anterior a la fecha inicial.");

        try
        {
            await using var conexion = new SqlConnection(CadenaConexion());
            await conexion.OpenAsync();
            // empieza con la condicion fija y agrega solo las partes de filtros que tienen valor
            var condiciones = new List<string> { "o.TipoOrden = N'VENTA'" };
            var comando = conexion.CreateCommand();
            if (usuarioId.HasValue)
            {
                condiciones.Add("o.UsuarioId = @UsuarioId");
                comando.Parameters.AddWithValue("@UsuarioId", usuarioId.Value);
            }
            if (!string.IsNullOrEmpty(filtro.Estado))
            {
                condiciones.Add("o.Estado = @Estado");
                comando.Parameters.AddWithValue("@Estado", filtro.Estado);
            }
            if (filtro.FechaDesde.HasValue)
            {
                condiciones.Add("o.FechaOrden >= @FechaDesde");
                comando.Parameters.AddWithValue("@FechaDesde", filtro.FechaDesde.Value.ToDateTime(TimeOnly.MinValue));
            }
            if (filtro.FechaHasta.HasValue)
            {
                condiciones.Add("o.FechaOrden < @FechaHasta");
                comando.Parameters.AddWithValue("@FechaHasta", filtro.FechaHasta.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));
            }
            if (!string.IsNullOrEmpty(filtro.Cliente))
            {
                condiciones.Add("(u.Nombre LIKE @Cliente OR u.Apellidos LIKE @Cliente OR u.Correo LIKE @Cliente)");
                comando.Parameters.AddWithValue("@Cliente", $"%{filtro.Cliente}%");
            }
            if (!string.IsNullOrEmpty(filtro.Numero))
            {
                // Where deja solo digitos para aceptar numeros mostrados como 000012 o FAC-000012
                var digitos = new string(filtro.Numero.Where(char.IsDigit).ToArray());
                if (!int.TryParse(digitos, out var ordenId)) condiciones.Add("1 = 0");
                else
                {
                    condiciones.Add("o.OrdenId = @OrdenId");
                    comando.Parameters.AddWithValue("@OrdenId", ordenId);
                }
            }

            // junta las condiciones controladas por el codigo y los valores viajan como parametros
            var donde = string.Join(" AND ", condiciones);
            comando.CommandText = $"""
                SELECT COUNT(*) FROM dbo.Ordenes o INNER JOIN dbo.Usuarios u ON u.UsuarioId = o.UsuarioId WHERE {donde};

                SELECT o.OrdenId, o.FechaOrden, o.Estado, COALESCE(o.Total, 0),
                       CONCAT(u.Nombre, N' ', u.Apellidos), COALESCE(d.CorreoDestino, u.Correo),
                       COALESCE(c.CantidadProductos, 0), p.Metodo,
                       CONVERT(bit, CASE WHEN d.DocumentoId IS NULL THEN 0 ELSE 1 END)
                FROM dbo.Ordenes o
                INNER JOIN dbo.Usuarios u ON u.UsuarioId = o.UsuarioId
                -- OUTER APPLY calcula datos relacionados sin perder una orden que aun no tenga ese dato
                OUTER APPLY (SELECT SUM(od.Cantidad) CantidadProductos FROM dbo.OrdenDetalle od WHERE od.OrdenId = o.OrdenId) c
                OUTER APPLY (SELECT TOP (1) pg.Metodo FROM dbo.Pagos pg WHERE pg.OrdenId = o.OrdenId ORDER BY pg.PagoId DESC) p
                OUTER APPLY (SELECT TOP (1) doc.DocumentoId, doc.CorreoDestino FROM dbo.Documentos doc WHERE doc.OrdenId = 
                o.OrdenId AND doc.Tipo = N'FACTURA' ORDER BY doc.DocumentoId DESC) d
                WHERE {donde}
                ORDER BY o.FechaOrden DESC, o.OrdenId DESC
                -- OFFSET salta paginas anteriores y FETCH trae solo el tamaño pedido
                OFFSET @Omitir ROWS FETCH NEXT @Tomar ROWS ONLY;
                """;
            comando.Parameters.AddWithValue("@Omitir", (filtro.Pagina - 1) * filtro.TamanoPagina);
            comando.Parameters.AddWithValue("@Tomar", filtro.TamanoPagina);

            var items = new List<TOrdenResumen>();
            var total = 0;
            await using var lector = await comando.ExecuteReaderAsync();
            // la primera respuesta es el total y NextResult pasa a la lista paginada
            if (await lector.ReadAsync()) total = lector.GetInt32(0);
            await lector.NextResultAsync();
            while (await lector.ReadAsync()) items.Add(MapearResumen(lector));
            return new Respuesta<TPagina<TOrdenResumen>>
            {
                Data = new TPagina<TOrdenResumen>
                {
                    Items = items,
                    Pagina = filtro.Pagina,
                    TamanoPagina = filtro.TamanoPagina,
                    Total = total
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al listar órdenes de venta.");
            return Error<TPagina<TOrdenResumen>>("No fue posible cargar las órdenes.");
        }
    }

    // trae la cabecera y despues sus productos si pertenece al Cliente o consulta un Administrador
    public async Task<Respuesta<TOrdenDetalleConsulta>> ObtenerDetalleAsync(int ordenId, int usuarioId, bool administrador)
    {
        if (ordenId <= 0 || usuarioId <= 0) return Error<TOrdenDetalleConsulta>(Mensajes.RegistroNoEncontrado);
        try
        {
            await using var conexion = new SqlConnection(CadenaConexion());
            await conexion.OpenAsync();
            var cabecera = conexion.CreateCommand();
            cabecera.CommandText = """
                SELECT o.OrdenId, o.FechaOrden, o.Estado, COALESCE(o.Total, 0), o.DireccionEnvio,
                       CONCAT(u.Nombre, N' ', u.Apellidos), COALESCE(d.CorreoDestino, u.Correo),
                       p.Metodo, d.Numero, CONVERT(bit, COALESCE(d.EnviadoCorreo, 0)),
                       CONVERT(bit, CASE WHEN d.DocumentoId IS NULL THEN 0 ELSE 1 END)
                FROM dbo.Ordenes o
                INNER JOIN dbo.Usuarios u ON u.UsuarioId = o.UsuarioId
                OUTER APPLY (SELECT TOP (1) pg.Metodo FROM dbo.Pagos pg WHERE pg.OrdenId = o.OrdenId ORDER BY pg.PagoId DESC) p
                OUTER APPLY (SELECT TOP (1) doc.DocumentoId, doc.Numero, doc.CorreoDestino, 
                doc.EnviadoCorreo FROM dbo.Documentos doc WHERE doc.OrdenId = o.OrdenId AND doc.Tipo =
                N'FACTURA' ORDER BY doc.DocumentoId DESC) d
                WHERE o.OrdenId = @OrdenId AND o.TipoOrden = N'VENTA'
                  -- el Administrador puede ver cualquiera, el Cliente solo la orden que coincide con su ID
                  AND (@Administrador = 1 OR o.UsuarioId = @UsuarioId);
                """;
            cabecera.Parameters.AddWithValue("@OrdenId", ordenId);
            cabecera.Parameters.AddWithValue("@Administrador", administrador);
            cabecera.Parameters.AddWithValue("@UsuarioId", usuarioId);
            TOrdenDetalleConsulta? resultado = null;
            await using (var lector = await cabecera.ExecuteReaderAsync())
            {
                if (await lector.ReadAsync())
                {
                    resultado = new TOrdenDetalleConsulta
                    {
                        OrdenId = lector.GetInt32(0),
                        NumeroOrden = lector.GetInt32(0).ToString("D6"),
                        FechaOrden = lector.GetDateTime(1),
                        Estado = lector.GetString(2),
                        Total = lector.GetDecimal(3),
                        DireccionEnvio = lector.IsDBNull(4) ? string.Empty : lector.GetString(4),
                        Cliente = lector.GetString(5).Trim(),
                        Correo = lector.GetString(6),
                        MetodoPago = lector.IsDBNull(7) ? null : lector.GetString(7),
                        NumeroFactura = lector.IsDBNull(8) ? null : lector.GetString(8),
                        CorreoEnviado = lector.GetBoolean(9),
                        FacturaDisponible = lector.GetBoolean(10)
                    };
                }
            }
            if (resultado == null) return Error<TOrdenDetalleConsulta>(Mensajes.RegistroNoEncontrado);

            // despues de encontrar una cabecera autorizada trae las lineas historicas de esa orden
            var detalles = conexion.CreateCommand();
            detalles.CommandText = """
                SELECT od.ProductoId, p.Nombre, od.Cantidad, od.PrecioUnitario,
                       od.PorcentajeImpuesto, od.PorcentajeDescuento, od.Subtotal, od.TotalLinea
                FROM dbo.OrdenDetalle od INNER JOIN dbo.Productos p ON p.ProductoId = od.ProductoId
                WHERE od.OrdenId = @OrdenId ORDER BY od.OrdenDetalleId;
                """;
            detalles.Parameters.AddWithValue("@OrdenId", ordenId);
            var productos = new List<TOrdenProductoConsulta>();
            await using (var lector = await detalles.ExecuteReaderAsync())
            {
                while (await lector.ReadAsync())
                {
                    var cantidad = lector.GetInt32(2);
                    var precio = lector.GetDecimal(3);
                    var subtotal = lector.GetDecimal(6);
                    var totalLinea = lector.GetDecimal(7);
                    var baseLinea = Redondear(precio * cantidad);
                    var porcentajeDescuento = lector.GetDecimal(5);
                    productos.Add(new TOrdenProductoConsulta
                    {
                        ProductoId = lector.GetInt32(0),
                        Nombre = lector.GetString(1),
                        Cantidad = cantidad,
                        PrecioUnitario = precio,
                        PorcentajeImpuesto = lector.GetDecimal(4),
                        PorcentajeDescuento = porcentajeDescuento,
                        Subtotal = subtotal,
                        Descuentos = Redondear(baseLinea * porcentajeDescuento / 100m),
                        Impuestos = totalLinea - subtotal,
                        TotalLinea = totalLinea
                    });
                }
            }
            // suma las lineas para completar el resumen que se muestra arriba de la tabla
            resultado.Productos = productos;
            resultado.CantidadProductos = productos.Sum(x => x.Cantidad);
            resultado.Subtotal = productos.Sum(x => x.Subtotal);
            resultado.Impuestos = productos.Sum(x => x.Impuestos);
            resultado.Descuentos = productos.Sum(x => x.Descuentos);
            return new Respuesta<TOrdenDetalleConsulta> { Data = resultado };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el detalle protegido de OrdenId {OrdenId}.", ordenId);
            return Error<TOrdenDetalleConsulta>("No fue posible cargar la orden.");
        }
    }

    // busca la ruta y numero de la ultima factura autorizada para esa orden
    public async Task<Respuesta<TArchivoFactura>> ObtenerFacturaAsync(int ordenId, int usuarioId, bool administrador)
    {
        try
        {
            await using var conexion = new SqlConnection(CadenaConexion());
            await conexion.OpenAsync();
            var comando = conexion.CreateCommand();
            comando.CommandText = """
                SELECT TOP (1) d.Ruta, d.Numero
                FROM dbo.Documentos d INNER JOIN dbo.Ordenes o ON o.OrdenId = d.OrdenId
                WHERE o.OrdenId = @OrdenId AND o.TipoOrden = N'VENTA' AND d.Tipo = N'FACTURA'
                  AND (@Administrador = 1 OR o.UsuarioId = @UsuarioId)
                ORDER BY d.DocumentoId DESC;
                """;
            comando.Parameters.AddWithValue("@OrdenId", ordenId);
            comando.Parameters.AddWithValue("@Administrador", administrador);
            comando.Parameters.AddWithValue("@UsuarioId", usuarioId);
            await using var lector = await comando.ExecuteReaderAsync();
            if (!await lector.ReadAsync()) return Error<TArchivoFactura>(Mensajes.RegistroNoEncontrado);
            return new Respuesta<TArchivoFactura>
            {
                Data = new TArchivoFactura { RutaRelativa = lector.GetString(0), NumeroFactura = lector.GetString(1) }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al localizar la factura protegida de OrdenId {OrdenId}.", ordenId);
            return Error<TArchivoFactura>("No fue posible obtener la factura.");
        }
    }

    // intenta cambiar a CANCELADA solo una orden propia que todavia esta PENDIENTE
    public async Task<Respuesta<bool>> CancelarPendienteAsync(int ordenId, int usuarioId)
    {
        if (ordenId <= 0 || usuarioId <= 0) return Error<bool>(Mensajes.RegistroNoEncontrado);
        try
        {
            await using var conexion = new SqlConnection(CadenaConexion());
            await conexion.OpenAsync();
            await using var transaccion = (SqlTransaction)await conexion.BeginTransactionAsync(IsolationLevel.Serializable);
            var comando = CrearComando(conexion, transaccion, """
                UPDATE dbo.Ordenes SET Estado = N'CANCELADA'
                WHERE OrdenId = @OrdenId AND UsuarioId = @UsuarioId
                  AND TipoOrden = N'VENTA' AND Estado = N'PENDIENTE';
                """);
            comando.Parameters.AddWithValue("@OrdenId", ordenId);
            comando.Parameters.AddWithValue("@UsuarioId", usuarioId);
            // cero filas significa que no existe, no es del usuario o ya cambio de estado
            if (await comando.ExecuteNonQueryAsync() != 1)
            {
                await transaccion.RollbackAsync();
                return Error<bool>("Solo puedes cancelar una orden propia que todavía esté PENDIENTE.");
            }
            await InsertarBitacoraAsync(conexion, transaccion, usuarioId, "CANCELAR_ORDEN", ordenId,
                "Orden pendiente cancelada por el cliente.", CancellationToken.None);
            await transaccion.CommitAsync();
            return new Respuesta<bool> { Data = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cancelar OrdenId {OrdenId}.", ordenId);
            return Error<bool>("No fue posible cancelar la orden.");
        }
    }

    // operaciones administrativas conservadas por la arquitectura original de ordenes
    // este metodo completa valores basicos y crea una orden directamente
    public async Task<Respuesta<TOrden>> InsertarAsync(TOrden datos)
    {
        try
        {
            datos.FechaOrden = DateTime.UtcNow;
            datos.Estado = string.IsNullOrWhiteSpace(datos.Estado) ? "PENDIENTE" : datos.Estado;
            datos.TipoOrden = string.IsNullOrWhiteSpace(datos.TipoOrden) ? "VENTA" : datos.TipoOrden;
            datos.Moneda = string.IsNullOrWhiteSpace(datos.Moneda) ? "CRC" : datos.Moneda;
            var entidad = _mapper.Map<Orden>(datos);
            var respuesta = await _unidadDeTrabajo.TOrden.InsertarAsync(entidad);
            return respuesta.Data == null || !string.IsNullOrEmpty(respuesta.Error)
                ? Error<TOrden>(Mensajes.ErrorOperacion)
                : new Respuesta<TOrden> { Data = _mapper.Map<TOrden>(respuesta.Data) };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al insertar una orden administrativa.");
            return Error<TOrden>(Mensajes.ErrorOperacion);
        }
    }

    // lista simple de ordenes para compatibilidad con el mantenimiento original
    public async Task<Respuesta<IEnumerable<TOrden>>> ListarAsync()
    {
        var respuesta = await _unidadDeTrabajo.TOrden.ListarAsync();
        return string.IsNullOrEmpty(respuesta.Error)
            ? new Respuesta<IEnumerable<TOrden>> { Data = _mapper.Map<IEnumerable<TOrden>>(respuesta.Data ?? []) }
            : Error<IEnumerable<TOrden>>(Mensajes.ErrorOperacion);
    }

    // busca por ID y copia los cambios si la orden existe
    public async Task<Respuesta<TOrden>> ModificarAsync(TOrden datos)
    {
        var actual = await _unidadDeTrabajo.TOrden.ObtenerEntidadAsync(x => x.OrdenId == datos.OrdenId);
        if (actual.Data == null) return Error<TOrden>(Mensajes.RegistroNoExisteModificar);
        _mapper.Map(datos, actual.Data);
        var respuesta = await _unidadDeTrabajo.TOrden.ModificarAsync(actual.Data);
        return respuesta.Data == null ? Error<TOrden>(Mensajes.ErrorOperacion) : new Respuesta<TOrden> {
        Data = _mapper.Map<TOrden>(respuesta.Data) };
    }

    // borrado fisico conservado para el contrato administrativo original
    public async Task<Respuesta<bool>> EliminarAsync(TOrden datos)
    {
        var entidad = await _unidadDeTrabajo.TOrden.ObtenerEntidadAsync(x => x.OrdenId == datos.OrdenId);
        if (entidad.Data == null) return Error<bool>(Mensajes.RegistroNoExisteEliminar);
        var respuesta = await _unidadDeTrabajo.TOrden.EliminarAsync(entidad.Data);
        return !string.IsNullOrEmpty(respuesta.Error) ? Error<bool>(Mensajes.ErrorOperacion) : new Respuesta<bool> { Data = respuesta.Data };
    }

    // busca ordenes cuyo estado contiene el texto recibido
    public async Task<Respuesta<IEnumerable<TOrden>>> BuscarAsync(TOrden datos)
    {
        var estado = datos.Estado ?? string.Empty;
        var respuesta = await _unidadDeTrabajo.TOrden.BuscarAsync(x => x.Estado.Contains(estado));
        return string.IsNullOrEmpty(respuesta.Error)
            ? new Respuesta<IEnumerable<TOrden>> { Data = _mapper.Map<IEnumerable<TOrden>>(respuesta.Data ?? []) }
            : Error<IEnumerable<TOrden>>(Mensajes.ErrorOperacion);
    }

    // trae una sola orden por su ID
    public async Task<Respuesta<TOrden>> ObtenerAsync(TOrden datos)
    {
        var respuesta = await _unidadDeTrabajo.TOrden.ObtenerEntidadAsync(x => x.OrdenId == datos.OrdenId);
        return respuesta.Data == null
            ? Error<TOrden>(Mensajes.RegistroNoEncontrado)
            : new Respuesta<TOrden> { Data = _mapper.Map<TOrden>(respuesta.Data) };
    }

    // convierte las columnas del lector SQL al resumen que recibe Angular
    private static TOrdenResumen MapearResumen(SqlDataReader lector) => new()
    {
        OrdenId = lector.GetInt32(0),
        NumeroOrden = lector.GetInt32(0).ToString("D6"),
        FechaOrden = lector.GetDateTime(1),
        Estado = lector.GetString(2),
        Total = lector.GetDecimal(3),
        Cliente = lector.GetString(4).Trim(),
        Correo = lector.GetString(5),
        CantidadProductos = lector.GetInt32(6),
        MetodoPago = lector.IsDBNull(7) ? null : lector.GetString(7),
        FacturaDisponible = lector.GetBoolean(8)
    };

    // inserta la bitacora usando la misma conexion y transaccion de la operacion principal
    private static async Task InsertarBitacoraAsync(
        SqlConnection conexion, SqlTransaction transaccion, int usuarioId, string accion,
        int ordenId, string detalle, CancellationToken cancellationToken)
    {
        var comando = CrearComando(conexion, transaccion, """
            INSERT INTO dbo.BitacoraSistema (UsuarioId, Fecha, Accion, Entidad, EntidadId, Detalle)
            VALUES (@UsuarioId, SYSDATETIME(), @Accion, N'Orden', CONVERT(nvarchar(80), @OrdenId), @Detalle);
            """);
        comando.Parameters.AddWithValue("@UsuarioId", usuarioId);
        comando.Parameters.AddWithValue("@Accion", accion);
        comando.Parameters.AddWithValue("@OrdenId", ordenId);
        comando.Parameters.AddWithValue("@Detalle", detalle);
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    // crea comandos ligados a la transaccion y deja un minuto maximo de espera
    private static SqlCommand CrearComando(SqlConnection conexion, SqlTransaction transaccion, string texto) =>
        new(texto, conexion, transaccion) { CommandTimeout = 60 };

    // define precision y dos decimales para no depender de AddWithValue con montos
    private static SqlParameter DecimalParametro(string nombre, decimal valor, byte precision = 18) =>
        new(nombre, SqlDbType.Decimal) { Precision = precision, Scale = 2, Value = valor };

    // toma la misma conexion configurada para el resto del proyecto
    private string CadenaConexion() => _configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("No existe la conexión DefaultConnection.");

    // limpia y valida los datos del checkout antes de abrir la transaccion de compra
    private static string? ValidarConfirmacion(TConfirmarCompra datos, int usuarioId)
    {
        if (usuarioId <= 0) return Mensajes.SesionInvalidaCarrito;
        datos.CorreoDestino = (datos.CorreoDestino ?? string.Empty).Trim().ToLowerInvariant();
        // Regex cambia varios espacios seguidos por uno solo
        datos.DireccionEnvio = Regex.Replace((datos.DireccionEnvio ?? string.Empty).Trim(), @"\s+", " ");
        datos.MetodoPago = (datos.MetodoPago ?? string.Empty).Trim().ToUpperInvariant();
        if (!datos.CorreoConfirmado) return "Debes confirmar que el correo de la factura es correcto.";
        if (!MailAddress.TryCreate(datos.CorreoDestino, out _) || datos.CorreoDestino.Length > 120)
            return "Ingresa un correo electrónico válido.";
        if (datos.DireccionEnvio.Length is < 10 or > 500)
            return "La dirección de entrega debe contener entre 10 y 500 caracteres.";
        if (datos.MetodoPago is not ("TARJETA" or "EFECTIVO"))
            return "Selecciona Tarjeta o Efectivo como método de pago.";
        return null;
    }

    // limpia filtros y corrige pagina y tamaño antes de armar el SQL
    private static void NormalizarFiltro(TFiltroOrdenes filtro)
    {
        filtro.Numero = filtro.Numero?.Trim();
        filtro.Cliente = filtro.Cliente?.Trim();
        filtro.Estado = filtro.Estado?.Trim().ToUpperInvariant();
        filtro.Pagina = Math.Max(1, filtro.Pagina);
        if (!new[] { 25, 50, 75, 100 }.Contains(filtro.TamanoPagina)) filtro.TamanoPagina = 25;
    }

    private static decimal Redondear(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);
    private static Respuesta<T> Error<T>(string mensaje) => new() { Success = false, Error = mensaje };

    private sealed class ClienteCompra
    {
        public string NombreCompleto { get; set; } = string.Empty;
    }

    private sealed class TFacturaItemCompra : TFacturaItem
    {
        public int StockDisponible { get; set; }
        public bool ProductoActivo { get; set; }
    }

    private sealed class CompraInvalidaException(string message) : Exception(message);
}
