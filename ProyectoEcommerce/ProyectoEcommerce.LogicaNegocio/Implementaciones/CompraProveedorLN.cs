using System.Data;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Dominio.InterfazLN;
using ProyectoEcommerce.Utilidades;

namespace ProyectoEcommerce.LogicaNegocio.Implementaciones;

// confirma compras separadas del carrito del Cliente y registra sus entradas de inventario
public class CompraProveedorLN : ICompraProveedorLN
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<CompraProveedorLN> _logger;
    private readonly IFacturaLN _facturaLN;
    private readonly ICorreoFacturaLN _correoFacturaLN;
    private readonly IWebHostEnvironment _entorno;

    public CompraProveedorLN(
        IConfiguration configuration,
        ILogger<CompraProveedorLN> logger,
        IFacturaLN facturaLN,
        ICorreoFacturaLN correoFacturaLN,
        IWebHostEnvironment entorno)
    {
        _configuration = configuration;
        _logger = logger;
        _facturaLN = facturaLN;
        _correoFacturaLN = correoFacturaLN;
        _entorno = entorno;
    }

    // recalcula precios desde las ofertas; no crea compra ni cambia stock
    public async Task<Respuesta<TDocumentoCompraProveedor>> PrepararProformaAsync(
        TSolicitudCompraProveedor datos)
    {
        var validacion = ValidarSolicitud(datos, false);
        if (validacion != null) return Error<TDocumentoCompraProveedor>(validacion);

        try
        {
            var documento = await ConstruirDesdeSolicitudAsync(datos);
            return documento == null
                ? Error<TDocumentoCompraProveedor>(
                    "Uno de los productos no pertenece al proveedor o aún no fue incorporado.")
                : new Respuesta<TDocumentoCompraProveedor> { Data = documento };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al preparar la proforma del ProveedorId {ProveedorId}.", datos.ProveedorId);
            return Error<TDocumentoCompraProveedor>("No fue posible generar la proforma.");
        }
    }

    public async Task<Respuesta<bool>> EnviarProformaAsync(
        TSolicitudCompraProveedor datos,
        CancellationToken cancellationToken = default)
    {
        var preparacion = await PrepararProformaAsync(datos);
        if (preparacion.Data == null || !string.IsNullOrEmpty(preparacion.Error))
            return Error<bool>(preparacion.Error);

        var carpeta = Path.Combine(RaizWeb(), "documentos", "proformas");
        var ruta = Path.Combine(carpeta, $"Proforma-{Guid.NewGuid():N}.pdf");
        try
        {
            Directory.CreateDirectory(carpeta);
            await File.WriteAllBytesAsync(
                ruta,
                _facturaLN.GenerarCompraProveedor(preparacion.Data, true),
                cancellationToken);
            var correo = await _correoFacturaLN.EnviarCompraProveedorAsync(
                preparacion.Data,
                ruta,
                true,
                cancellationToken);
            return correo.Enviado
                ? new Respuesta<bool> { Data = true }
                : Error<bool>(correo.Error ?? "No fue posible enviar la proforma.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar o enviar una proforma de proveedor.");
            return Error<bool>("No fue posible enviar la proforma.");
        }
        finally
        {
            if (File.Exists(ruta)) File.Delete(ruta);
        }
    }

    public async Task<Respuesta<TCompraProveedorConfirmada>> ConfirmarAsync(
        TSolicitudCompraProveedor datos,
        int administradorId,
        CancellationToken cancellationToken = default)
    {
        var validacion = ValidarSolicitud(datos, true);
        if (validacion != null || administradorId <= 0)
            return Error<TCompraProveedorConfirmada>(validacion ?? "La sesión del administrador no es válida.");

        TCompraProveedorConfirmada compra;
        try
        {
            await using var conexion = new SqlConnection(CadenaConexion());
            await conexion.OpenAsync(cancellationToken);
            var comando = conexion.CreateCommand();
            comando.CommandType = CommandType.StoredProcedure;
            comando.CommandText = "dbo.sp_ConfirmarCompraProveedor";
            comando.CommandTimeout = 60;
            comando.Parameters.AddWithValue("@ClaveConfirmacion", datos.ClaveConfirmacion);
            comando.Parameters.AddWithValue("@ProveedorId", datos.ProveedorId);
            comando.Parameters.AddWithValue("@UsuarioId", administradorId);
            comando.Parameters.Add("@DetalleJson", SqlDbType.NVarChar, -1).Value =
                JsonSerializer.Serialize(datos.Productos, OpcionesJson);
            await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
            if (!await lector.ReadAsync(cancellationToken))
                return Error<TCompraProveedorConfirmada>("No fue posible confirmar la compra.");

            compra = new TCompraProveedorConfirmada
            {
                CompraProveedorId = lector.GetInt32(0),
                Numero = lector.GetString(1),
                Total = lector.GetDecimal(2),
                Estado = lector.GetString(3),
                Fecha = lector.GetDateTime(4),
                Mensaje = "Compra confirmada e inventario actualizado correctamente."
            };
        }
        catch (SqlException ex) when (ex.Number is >= 51101 and <= 51106)
        {
            return Error<TCompraProveedorConfirmada>(MensajeSql(ex));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transaccional al confirmar compra a ProveedorId {ProveedorId}.", datos.ProveedorId);
            return Error<TCompraProveedorConfirmada>(
                "No fue posible confirmar la compra. El inventario no quedó actualizado parcialmente.");
        }

        // igual que en ventas, PDF y correo quedan fuera de la transacción de inventario
        try
        {
            var detalle = await CargarDocumentoConfirmadoAsync(compra.CompraProveedorId, cancellationToken);
            if (detalle == null) throw new InvalidOperationException("No se pudo reconstruir el comprobante.");

            var nombreArchivo = $"Compra-{compra.Numero}.pdf";
            var rutaRelativa = $"documentos/compras/{nombreArchivo}";
            var rutaAbsoluta = Path.Combine(RaizWeb(), "documentos", "compras", nombreArchivo);
            Directory.CreateDirectory(Path.GetDirectoryName(rutaAbsoluta)!);
            await File.WriteAllBytesAsync(
                rutaAbsoluta,
                _facturaLN.GenerarCompraProveedor(detalle, false),
                cancellationToken);
            await RegistrarDocumentoAsync(
                compra.CompraProveedorId,
                compra.Numero,
                rutaRelativa,
                detalle.CorreoProveedor,
                cancellationToken);
            compra.PdfDisponible = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "La compra {Numero} se confirmó, pero no se pudo generar su PDF.", compra.Numero);
            compra.Mensaje = "La compra quedó confirmada, pero no fue posible generar el PDF.";
        }

        return new Respuesta<TCompraProveedorConfirmada> { Data = compra };
    }

    public async Task<Respuesta<TPagina<TCompraProveedorResumen>>> ListarAsync(
        TFiltroComprasProveedor filtro)
    {
        NormalizarFiltro(filtro);
        try
        {
            await using var conexion = new SqlConnection(CadenaConexion());
            await conexion.OpenAsync();
            var condiciones = new List<string> { "1 = 1" };
            if (filtro.ProveedorId.HasValue) condiciones.Add("compra.ProveedorId = @ProveedorId");
            if (!string.IsNullOrEmpty(filtro.Estado)) condiciones.Add("compra.Estado = @Estado");
            if (filtro.FechaDesde.HasValue) condiciones.Add("compra.Fecha >= @FechaDesde");
            if (filtro.FechaHasta.HasValue) condiciones.Add("compra.Fecha < DATEADD(day, 1, @FechaHasta)");
            var donde = string.Join(" AND ", condiciones);

            var comando = conexion.CreateCommand();
            comando.CommandText = $"""
                SELECT COUNT(*)
                FROM dbo.ComprasProveedor compra
                WHERE {donde};

                SELECT compra.CompraProveedorId, compra.Numero, compra.ProveedorId,
                       proveedor.Nombre, compra.Fecha, compra.Total, compra.Estado,
                       CONVERT(bit, CASE WHEN documento.DocumentoId IS NULL THEN 0 ELSE 1 END)
                FROM dbo.ComprasProveedor compra
                INNER JOIN dbo.Proveedores proveedor ON proveedor.ProveedorId = compra.ProveedorId
                OUTER APPLY
                (
                    SELECT TOP (1) DocumentoId
                    FROM dbo.Documentos
                    WHERE CompraProveedorId = compra.CompraProveedorId AND Tipo = N'COMPRA_PROVEEDOR'
                    ORDER BY DocumentoId DESC
                ) documento
                WHERE {donde}
                ORDER BY compra.Fecha DESC, compra.CompraProveedorId DESC
                OFFSET @Omitir ROWS FETCH NEXT @Tomar ROWS ONLY;
                """;
            AgregarFiltros(comando, filtro);
            comando.Parameters.AddWithValue("@Omitir", (filtro.Pagina - 1) * filtro.TamanoPagina);
            comando.Parameters.AddWithValue("@Tomar", filtro.TamanoPagina);
            var items = new List<TCompraProveedorResumen>();
            var total = 0;
            await using var lector = await comando.ExecuteReaderAsync();
            if (await lector.ReadAsync()) total = lector.GetInt32(0);
            await lector.NextResultAsync();
            while (await lector.ReadAsync()) items.Add(MapearResumen(lector));
            return new Respuesta<TPagina<TCompraProveedorResumen>>
            {
                Data = new TPagina<TCompraProveedorResumen>
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
            _logger.LogError(ex, "Error al listar compras a proveedores.");
            return Error<TPagina<TCompraProveedorResumen>>("No fue posible cargar el historial de compras.");
        }
    }

    public async Task<Respuesta<TCompraProveedorDetalleConsulta>> ObtenerDetalleAsync(int compraProveedorId)
    {
        if (compraProveedorId <= 0)
            return Error<TCompraProveedorDetalleConsulta>("La compra no existe.");
        try
        {
            await using var conexion = new SqlConnection(CadenaConexion());
            await conexion.OpenAsync();
            var comando = conexion.CreateCommand();
            comando.CommandText = """
                SELECT compra.CompraProveedorId, compra.Numero, compra.ProveedorId,
                       proveedor.Nombre, compra.Fecha, compra.Total, compra.Estado,
                       proveedor.Correo, proveedor.Telefono, proveedor.Direccion,
                       CONVERT(bit, CASE WHEN documento.DocumentoId IS NULL THEN 0 ELSE 1 END)
                FROM dbo.ComprasProveedor compra
                INNER JOIN dbo.Proveedores proveedor ON proveedor.ProveedorId = compra.ProveedorId
                OUTER APPLY
                (
                    SELECT TOP (1) DocumentoId FROM dbo.Documentos
                    WHERE CompraProveedorId = compra.CompraProveedorId AND Tipo = N'COMPRA_PROVEEDOR'
                    ORDER BY DocumentoId DESC
                ) documento
                WHERE compra.CompraProveedorId = @CompraProveedorId;

                SELECT COALESCE(detalle.ProductoProveedorCatalogoId, 0), detalle.ProductoId,
                       detalle.NombreProducto, detalle.Cantidad, detalle.PrecioUnitario, detalle.Subtotal
                FROM dbo.CompraProveedorDetalle detalle
                WHERE detalle.CompraProveedorId = @CompraProveedorId
                ORDER BY detalle.CompraProveedorDetalleId;
                """;
            comando.Parameters.AddWithValue("@CompraProveedorId", compraProveedorId);
            await using var lector = await comando.ExecuteReaderAsync();
            if (!await lector.ReadAsync())
                return Error<TCompraProveedorDetalleConsulta>("La compra no existe.");

            var resultado = new TCompraProveedorDetalleConsulta
            {
                CompraProveedorId = lector.GetInt32(0),
                Numero = lector.GetString(1),
                ProveedorId = lector.GetInt32(2),
                Proveedor = lector.GetString(3),
                Fecha = lector.GetDateTime(4),
                Total = lector.GetDecimal(5),
                Estado = lector.GetString(6),
                CorreoProveedor = lector.IsDBNull(7) ? null : lector.GetString(7),
                TelefonoProveedor = lector.IsDBNull(8) ? null : lector.GetString(8),
                DireccionProveedor = lector.IsDBNull(9) ? null : lector.GetString(9),
                PdfDisponible = lector.GetBoolean(10)
            };
            await lector.NextResultAsync();
            var productos = new List<TDocumentoCompraProveedorItem>();
            while (await lector.ReadAsync())
            {
                productos.Add(new TDocumentoCompraProveedorItem
                {
                    ProductoProveedorCatalogoId = lector.GetInt32(0),
                    ProductoId = lector.GetInt32(1),
                    Nombre = lector.GetString(2),
                    Cantidad = lector.GetInt32(3),
                    PrecioUnitario = lector.GetDecimal(4),
                    Subtotal = lector.GetDecimal(5)
                });
            }
            resultado.Productos = productos;
            return new Respuesta<TCompraProveedorDetalleConsulta> { Data = resultado };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener la compra {CompraProveedorId}.", compraProveedorId);
            return Error<TCompraProveedorDetalleConsulta>("No fue posible cargar el detalle de la compra.");
        }
    }

    public async Task<Respuesta<TArchivoCompraProveedor>> ObtenerPdfAsync(int compraProveedorId)
    {
        if (compraProveedorId <= 0) return Error<TArchivoCompraProveedor>("El PDF no existe.");
        try
        {
            await using var conexion = new SqlConnection(CadenaConexion());
            await conexion.OpenAsync();
            var comando = conexion.CreateCommand();
            comando.CommandText = """
                SELECT TOP (1) Ruta, Numero
                FROM dbo.Documentos
                WHERE CompraProveedorId = @CompraProveedorId AND Tipo = N'COMPRA_PROVEEDOR'
                ORDER BY DocumentoId DESC;
                """;
            comando.Parameters.AddWithValue("@CompraProveedorId", compraProveedorId);
            await using var lector = await comando.ExecuteReaderAsync();
            return !await lector.ReadAsync()
                ? Error<TArchivoCompraProveedor>("El PDF no existe.")
                : new Respuesta<TArchivoCompraProveedor>
                {
                    Data = new TArchivoCompraProveedor
                    {
                        RutaRelativa = lector.GetString(0),
                        Numero = lector.GetString(1)
                    }
                };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al localizar el PDF de CompraProveedorId {CompraProveedorId}.", compraProveedorId);
            return Error<TArchivoCompraProveedor>("No fue posible obtener el PDF.");
        }
    }

    private async Task<TDocumentoCompraProveedor?> ConstruirDesdeSolicitudAsync(
        TSolicitudCompraProveedor datos)
    {
        await using var conexion = new SqlConnection(CadenaConexion());
        await conexion.OpenAsync();
        var comando = conexion.CreateCommand();
        comando.CommandText = """
            SELECT proveedor.Nombre, proveedor.Correo, proveedor.Telefono, proveedor.Direccion
            FROM dbo.Proveedores proveedor
            WHERE proveedor.ProveedorId = @ProveedorId AND proveedor.Activo = 1;

            SELECT oferta.ProductoProveedorCatalogoId, oferta.ProductoId, oferta.Nombre,
                   detalle.Cantidad, oferta.PrecioCompra,
                   ROUND(oferta.PrecioCompra * detalle.Cantidad, 2)
            FROM OPENJSON(@DetalleJson)
            WITH
            (
                ProductoProveedorCatalogoId INT '$.productoProveedorCatalogoId',
                Cantidad INT '$.cantidad'
            ) detalle
            INNER JOIN dbo.ProductosProveedorCatalogo oferta
                ON oferta.ProductoProveedorCatalogoId = detalle.ProductoProveedorCatalogoId
            INNER JOIN dbo.ProveedorCategorias relacion
                ON relacion.ProveedorCategoriaId = oferta.ProveedorCategoriaId
            INNER JOIN dbo.Productos producto
                ON producto.ProductoId = oferta.ProductoId
            INNER JOIN dbo.ProductoProveedor productoProveedor
                ON productoProveedor.ProductoId = oferta.ProductoId
               AND productoProveedor.ProveedorId = relacion.ProveedorId
            WHERE relacion.ProveedorId = @ProveedorId
              AND relacion.Activo = 1
              AND oferta.Activo = 1
              AND oferta.ProductoId IS NOT NULL
              AND producto.Activo = 1
              AND productoProveedor.Activo = 1;
            """;
        comando.Parameters.AddWithValue("@ProveedorId", datos.ProveedorId);
        comando.Parameters.Add("@DetalleJson", SqlDbType.NVarChar, -1).Value =
            JsonSerializer.Serialize(datos.Productos, OpcionesJson);
        await using var lector = await comando.ExecuteReaderAsync();
        if (!await lector.ReadAsync()) return null;

        var documento = new TDocumentoCompraProveedor
        {
            Fecha = DateTime.Now,
            ProveedorId = datos.ProveedorId,
            Proveedor = lector.GetString(0),
            CorreoProveedor = lector.IsDBNull(1) ? null : lector.GetString(1),
            TelefonoProveedor = lector.IsDBNull(2) ? null : lector.GetString(2),
            DireccionProveedor = lector.IsDBNull(3) ? null : lector.GetString(3),
            Estado = "PROFORMA"
        };
        await lector.NextResultAsync();
        var productos = new List<TDocumentoCompraProveedorItem>();
        while (await lector.ReadAsync())
        {
            productos.Add(new TDocumentoCompraProveedorItem
            {
                ProductoProveedorCatalogoId = lector.GetInt32(0),
                ProductoId = lector.GetInt32(1),
                Nombre = lector.GetString(2),
                Cantidad = lector.GetInt32(3),
                PrecioUnitario = lector.GetDecimal(4),
                Subtotal = lector.GetDecimal(5)
            });
        }
        if (productos.Count != datos.Productos.Count) return null;
        documento.Productos = productos;
        documento.Total = productos.Sum(x => x.Subtotal);
        return documento;
    }

    private async Task<TDocumentoCompraProveedor?> CargarDocumentoConfirmadoAsync(
        int compraProveedorId,
        CancellationToken cancellationToken)
    {
        var respuesta = await ObtenerDetalleAsync(compraProveedorId);
        if (respuesta.Data == null) return null;
        return new TDocumentoCompraProveedor
        {
            CompraProveedorId = respuesta.Data.CompraProveedorId,
            Numero = respuesta.Data.Numero,
            Fecha = respuesta.Data.Fecha,
            ProveedorId = respuesta.Data.ProveedorId,
            Proveedor = respuesta.Data.Proveedor,
            CorreoProveedor = respuesta.Data.CorreoProveedor,
            TelefonoProveedor = respuesta.Data.TelefonoProveedor,
            DireccionProveedor = respuesta.Data.DireccionProveedor,
            Estado = respuesta.Data.Estado,
            Total = respuesta.Data.Total,
            Productos = respuesta.Data.Productos
        };
    }

    private async Task RegistrarDocumentoAsync(
        int compraProveedorId,
        string numero,
        string ruta,
        string? correo,
        CancellationToken cancellationToken)
    {
        await using var conexion = new SqlConnection(CadenaConexion());
        await conexion.OpenAsync(cancellationToken);
        var comando = conexion.CreateCommand();
        comando.CommandText = """
            IF NOT EXISTS
            (
                SELECT 1 FROM dbo.Documentos
                WHERE CompraProveedorId = @CompraProveedorId AND Tipo = N'COMPRA_PROVEEDOR'
            )
                INSERT dbo.Documentos
                (
                    OrdenId, CompraProveedorId, Tipo, Numero, Ruta,
                    CorreoDestino, EnviadoCorreo, FechaCreacion
                )
                VALUES
                (
                    NULL, @CompraProveedorId, N'COMPRA_PROVEEDOR', @Numero, @Ruta,
                    @Correo, 0, SYSDATETIME()
                );
            """;
        comando.Parameters.AddWithValue("@CompraProveedorId", compraProveedorId);
        comando.Parameters.AddWithValue("@Numero", numero);
        comando.Parameters.AddWithValue("@Ruta", ruta);
        comando.Parameters.Add("@Correo", SqlDbType.NVarChar, 120).Value = (object?)correo ?? DBNull.Value;
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string? ValidarSolicitud(TSolicitudCompraProveedor datos, bool requiereClave)
    {
        if (datos.ProveedorId <= 0) return "Selecciona un proveedor válido.";
        if (requiereClave && datos.ClaveConfirmacion == Guid.Empty)
            return "La clave de confirmación no es válida.";
        if (datos.Productos.Count == 0) return "Debe agregar al menos un producto.";
        if (datos.Productos.Any(x => x.ProductoProveedorCatalogoId <= 0 || x.Cantidad <= 0))
            return "La cantidad debe ser un número entero mayor que cero.";
        if (datos.Productos.Select(x => x.ProductoProveedorCatalogoId).Distinct().Count() !=
            datos.Productos.Count)
            return "La compra contiene productos repetidos.";
        return null;
    }

    private static void NormalizarFiltro(TFiltroComprasProveedor filtro)
    {
        filtro.Estado = filtro.Estado?.Trim().ToUpperInvariant();
        filtro.Pagina = Math.Max(1, filtro.Pagina);
        if (!new[] { 25, 50, 75, 100 }.Contains(filtro.TamanoPagina))
            filtro.TamanoPagina = 25;
    }

    private static void AgregarFiltros(SqlCommand comando, TFiltroComprasProveedor filtro)
    {
        if (filtro.ProveedorId.HasValue)
            comando.Parameters.AddWithValue("@ProveedorId", filtro.ProveedorId.Value);
        if (!string.IsNullOrEmpty(filtro.Estado))
            comando.Parameters.AddWithValue("@Estado", filtro.Estado);
        if (filtro.FechaDesde.HasValue)
            comando.Parameters.AddWithValue("@FechaDesde", filtro.FechaDesde.Value.ToDateTime(TimeOnly.MinValue));
        if (filtro.FechaHasta.HasValue)
            comando.Parameters.AddWithValue("@FechaHasta", filtro.FechaHasta.Value.ToDateTime(TimeOnly.MinValue));
    }

    private static TCompraProveedorResumen MapearResumen(SqlDataReader lector) => new()
    {
        CompraProveedorId = lector.GetInt32(0),
        Numero = lector.GetString(1),
        ProveedorId = lector.GetInt32(2),
        Proveedor = lector.GetString(3),
        Fecha = lector.GetDateTime(4),
        Total = lector.GetDecimal(5),
        Estado = lector.GetString(6),
        PdfDisponible = lector.GetBoolean(7)
    };

    private string RaizWeb() => _entorno.WebRootPath ?? Path.Combine(_entorno.ContentRootPath, "wwwroot");

    private string CadenaConexion() => _configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("No existe la conexión DefaultConnection.");

    private static string MensajeSql(SqlException ex) =>
        ex.Message.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries)[0];

    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);
    private static Respuesta<T> Error<T>(string mensaje) => new() { Success = false, Error = mensaje };
}
