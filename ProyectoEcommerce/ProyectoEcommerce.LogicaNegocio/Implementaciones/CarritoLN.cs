using System.Data;
using Microsoft.Extensions.Logging;
using ProyectoEcommerce.Dominio.Entidades;
using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Dominio.InterfacesAD;
using ProyectoEcommerce.Dominio.InterfazLN;
using ProyectoEcommerce.Recursos;
using ProyectoEcommerce.Utilidades;

namespace ProyectoEcommerce.LogicaNegocio.Implementaciones;

// aqui se maneja el unico carrito ACTIVO de cada Cliente
// valida dueño, producto y stock y siempre vuelve a calcular precios, impuestos y descuentos
public class CarritoLN : ICarritoLN
{
    private const string EstadoActivo = "ACTIVO";
    private readonly IUnidadTrabajoEF _unidadDeTrabajo;
    private readonly ILogger<CarritoLN> _logger;
    private readonly IDescuentoLN _descuentoLN;

    public CarritoLN(IUnidadTrabajoEF unidadDeTrabajo, ILogger<CarritoLN> logger, IDescuentoLN descuentoLN)
    {
        _unidadDeTrabajo = unidadDeTrabajo;
        _logger = logger;
        _descuentoLN = descuentoLN;
    }

    // recibe producto, cantidad y el usuario del JWT
    // crea el carrito si hace falta o suma la cantidad si el producto ya estaba agregado
    public async Task<Respuesta<TResultadoAgregarCarrito>> AgregarAsync(TAgregarProductoCarrito datos, int usuarioId)
    {
        // estas validaciones simples evitan abrir una transaccion con datos que ya vienen mal
        if (usuarioId <= 0) return Error<TResultadoAgregarCarrito>(Mensajes.SesionInvalidaCarrito);
        if (datos.ProductoId <= 0 || datos.Cantidad <= 0)
            return Error<TResultadoAgregarCarrito>(Mensajes.CantidadCarritoInvalida);

        try
        {
            // Serializable evita que dos solicitudes cambien el mismo carrito o stock al mismo tiempo
            _unidadDeTrabajo.EmpezarTransaccion(IsolationLevel.Serializable);

            // busca solamente un producto activo y devuelve null si no se puede comprar
            var productoRespuesta = await _unidadDeTrabajo.TProducto.ObtenerEntidadAsync(
                x => x.ProductoId == datos.ProductoId && x.Activo);
            if (!string.IsNullOrEmpty(productoRespuesta.Error))
                return Revertir<TResultadoAgregarCarrito>(Mensajes.ErrorCarrito);

            var producto = productoRespuesta.Data;
            if (producto == null)
                return Revertir<TResultadoAgregarCarrito>(Mensajes.ProductoNoDisponibleCarrito);
            if (producto.Stock <= 0 || datos.Cantidad > producto.Stock)
                return Revertir<TResultadoAgregarCarrito>(Mensajes.StockInsuficienteCarrito);

            // Include Detalles trae las lineas actuales para saber si el producto ya esta agregado
            var carritoRespuesta = await _unidadDeTrabajo.TCarrito.ObtenerEntidadAsync(
                x => x.UsuarioId == usuarioId && x.Estado == EstadoActivo,
                ["Detalles"]);
            if (!string.IsNullOrEmpty(carritoRespuesta.Error))
                return Revertir<TResultadoAgregarCarrito>(Mensajes.ErrorCarrito);

            // crea el carrito solamente cuando el Cliente todavia no tiene uno abierto
            var carrito = carritoRespuesta.Data;
            if (carrito == null)
            {
                carrito = new Carrito
                {
                    UsuarioId = usuarioId,
                    FechaCreacion = DateTime.UtcNow,
                    Estado = EstadoActivo
                };
                var creacion = await _unidadDeTrabajo.TCarrito.InsertarAsync(carrito);
                if (creacion.Data == null || !string.IsNullOrEmpty(creacion.Error))
                    return Revertir<TResultadoAgregarCarrito>(Mensajes.ErrorCarrito);
            }

            // FirstOrDefault busca la linea de ese producto y devuelve null si es la primera vez
            // un producto mantiene una sola fila, agregarlo otra vez solo acumula la cantidad
            var detalle = carrito.Detalles.FirstOrDefault(x => x.ProductoId == producto.ProductoId);
            var productoNuevo = detalle == null;
            var cantidadProducto = (detalle?.Cantidad ?? 0) + datos.Cantidad;
            if (cantidadProducto > producto.Stock)
                return Revertir<TResultadoAgregarCarrito>(Mensajes.StockInsuficienteCarrito);

            int cantidadTotal;
            if (detalle == null)
            {
                // Sum junta las cantidades anteriores y agrega la nueva
                cantidadTotal = carrito.Detalles.Sum(x => x.Cantidad) + datos.Cantidad;
                detalle = new CarritoDetalle
                {
                    CarritoId = carrito.CarritoId,
                    ProductoId = producto.ProductoId,
                    Cantidad = datos.Cantidad,
                    PrecioUnitario = producto.PrecioVenta
                };
                var insercion = await _unidadDeTrabajo.TCarritoDetalle.InsertarAsync(detalle);
                if (insercion.Data == null || !string.IsNullOrEmpty(insercion.Error))
                    return Revertir<TResultadoAgregarCarrito>(Mensajes.ErrorCarrito);
            }
            else
            {
                detalle.Cantidad = cantidadProducto;
                detalle.PrecioUnitario = producto.PrecioVenta;
                cantidadTotal = carrito.Detalles.Sum(x => x.Cantidad);
                var actualizacion = await _unidadDeTrabajo.TCarritoDetalle.ModificarAsync(detalle);
                if (actualizacion.Data == null || !string.IsNullOrEmpty(actualizacion.Error))
                    return Revertir<TResultadoAgregarCarrito>(Mensajes.ErrorCarrito);
            }

            // solo despues de pasar todas las revisiones hace Commit de la transaccion
            _unidadDeTrabajo.CompletarTran();
            await RegistrarBitacora(usuarioId, carrito.CarritoId, producto.ProductoId, datos.Cantidad);

            return new Respuesta<TResultadoAgregarCarrito>
            {
                Data = new TResultadoAgregarCarrito
                {
                    CarritoId = carrito.CarritoId,
                    ProductoId = producto.ProductoId,
                    CantidadProducto = cantidadProducto,
                    CantidadTotal = cantidadTotal,
                    ProductoNuevo = productoNuevo
                }
            };
        }
        catch (Exception ex)
        {
            _unidadDeTrabajo.Rollback();
            _logger.LogError(ex, "Error al agregar ProductoId {ProductoId} al carrito del usuario autenticado.", datos.ProductoId);
            return Error<TResultadoAgregarCarrito>(Mensajes.ErrorCarrito);
        }
    }

    // trae el carrito del usuario y vuelve a calcularlo con precio, stock y descuentos actuales
    // devuelve un carrito vacio si el Cliente todavia no ha agregado nada
    public async Task<Respuesta<TCarritoActual>> ObtenerActualAsync(int usuarioId)
    {
        if (usuarioId <= 0) return Error<TCarritoActual>(Mensajes.SesionInvalidaCarrito);
        try
        {
            // este Include trae cada detalle con su producto e impuesto en la misma consulta
            var respuesta = await _unidadDeTrabajo.TCarrito.ObtenerEntidadAsync(
                x => x.UsuarioId == usuarioId && x.Estado == EstadoActivo,
                ["Detalles.Producto.Impuesto"]);
            if (!string.IsNullOrEmpty(respuesta.Error)) return Error<TCarritoActual>(Mensajes.ErrorCarrito);
            if (respuesta.Data == null)
                return new Respuesta<TCarritoActual> { Data = new TCarritoActual() };

            var carrito = respuesta.Data;
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
            // Select agarra solamente los IDs y la LN de descuentos resuelve el mejor para cada uno
            var descuentosRespuesta = await _descuentoLN.ObtenerMejoresDescuentosAsync(
                carrito.Detalles.Select(x => x.ProductoId));
            if (!string.IsNullOrEmpty(descuentosRespuesta.Error) || descuentosRespuesta.Data == null)
                return Error<TCarritoActual>(Mensajes.ErrorCarrito);
            var descuentos = descuentosRespuesta.Data;
            // recorre las lineas del carrito ya acomodadas por nombre y arma lo que recibira Angular
            var items = carrito.Detalles
                .OrderBy(x => x.Producto.Nombre)
                .Select(x =>
                {
                    var precio = x.Producto.PrecioVenta;
                    // TryGetValue trae el descuento calculado sin recorrer todo el diccionario
                    var descuento = descuentos.TryGetValue(x.ProductoId, out var resuelto)
                        ? resuelto
                        : ResolucionDescuentos.Calcular(x.ProductoId, precio, []);
                    var impuesto = x.Producto.Impuesto;
                    // el impuesto solo aplica si esta activo y la fecha actual cae dentro de su vigencia
                    var porcentaje = impuesto.Activo && impuesto.FechaInicio <= hoy &&
                        (!impuesto.FechaFin.HasValue || impuesto.FechaFin.Value >= hoy)
                        ? impuesto.Porcentaje
                        : 0m;
                    // PrecioVenta ya incluye impuesto, Calcular solo lo separa para mostrar el desglose
                    var desglose = CalculoPrecioIncluido.Calcular(precio, x.Cantidad, porcentaje, descuento.Porcentaje);
                    return new TCarritoItem
                    {
                        CarritoDetalleId = x.CarritoDetalleId,
                        ProductoId = x.ProductoId,
                        Nombre = x.Producto.Nombre,
                        Cantidad = x.Cantidad,
                        StockDisponible = x.Producto.Stock,
                        PrecioOriginal = precio,
                        PrecioUnitario = descuento.PrecioFinal,
                        PorcentajeImpuesto = porcentaje,
                        PorcentajeDescuento = descuento.Porcentaje,
                        TipoDescuento = descuento.TipoDescuento,
                        NombreDescuento = descuento.Nombre,
                        Subtotal = desglose.Subtotal,
                        Impuestos = desglose.Impuestos,
                        Descuentos = desglose.Descuento,
                        TotalLinea = desglose.Total,
                        ProductoActivo = x.Producto.Activo,
                        StockSuficiente = x.Producto.Activo && x.Producto.Stock >= x.Cantidad
                    };
                })
                .ToList();

            // suma cada columna de las lineas para formar los totales generales del carrito
            var subtotalCarrito = items.Sum(x => x.Subtotal);
            var impuestosCarrito = items.Sum(x => x.Impuestos);
            var descuentosCarrito = items.Sum(x => x.Descuentos);

            return new Respuesta<TCarritoActual>
            {
                Data = new TCarritoActual
                {
                    CarritoId = carrito.CarritoId,
                    Estado = carrito.Estado,
                    CantidadTotal = items.Sum(x => x.Cantidad),
                    Subtotal = subtotalCarrito,
                    Impuestos = impuestosCarrito,
                    Descuentos = descuentosCarrito,
                    Total = items.Sum(x => x.TotalLinea),
                    Items = items
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el carrito del usuario autenticado.");
            return Error<TCarritoActual>(Mensajes.ErrorCarrito);
        }
    }

    // cambia la cantidad de una linea despues de revisar dueño, carrito activo, producto y stock
    public async Task<Respuesta<TCarritoActual>> ActualizarCantidadAsync(TActualizarCantidadCarrito datos, int usuarioId)
    {
        if (usuarioId <= 0) return Error<TCarritoActual>(Mensajes.SesionInvalidaCarrito);
        if (datos.CarritoDetalleId <= 0 || datos.Cantidad <= 0)
            return Error<TCarritoActual>(Mensajes.CantidadCarritoInvalida);

        try
        {
            _unidadDeTrabajo.EmpezarTransaccion(IsolationLevel.Serializable);
            // la condicion incluye UsuarioId para que nunca se pueda modificar un carrito ajeno
            var detalleRespuesta = await _unidadDeTrabajo.TCarritoDetalle.ObtenerEntidadAsync(
                x => x.CarritoDetalleId == datos.CarritoDetalleId &&
                     x.Carrito.UsuarioId == usuarioId && x.Carrito.Estado == EstadoActivo,
                ["Producto"]);
            if (!string.IsNullOrEmpty(detalleRespuesta.Error))
                return Revertir<TCarritoActual>(Mensajes.ErrorCarrito);

            var detalle = detalleRespuesta.Data;
            if (detalle == null)
                return Revertir<TCarritoActual>(Mensajes.DetalleCarritoNoEncontrado);
            if (!detalle.Producto.Activo)
                return Revertir<TCarritoActual>(Mensajes.ProductoNoDisponibleCarrito);
            if (datos.Cantidad > detalle.Producto.Stock)
                return Revertir<TCarritoActual>(Mensajes.StockInsuficienteCarrito);

            detalle.Cantidad = datos.Cantidad;
            detalle.PrecioUnitario = detalle.Producto.PrecioVenta;
            var actualizacion = await _unidadDeTrabajo.TCarritoDetalle.ModificarAsync(detalle);
            if (actualizacion.Data == null || !string.IsNullOrEmpty(actualizacion.Error))
                return Revertir<TCarritoActual>(Mensajes.ErrorCarrito);

            // confirma el cambio y despues devuelve el carrito completo ya recalculado
            _unidadDeTrabajo.CompletarTran();
            await RegistrarBitacora(usuarioId, detalle.CarritoId, detalle.ProductoId, datos.Cantidad, "ACTUALIZAR_CARRITO");
            return await ObtenerActualAsync(usuarioId);
        }
        catch (Exception ex)
        {
            _unidadDeTrabajo.Rollback();
            _logger.LogError(ex, "Error al actualizar CarritoDetalleId {CarritoDetalleId}.", datos.CarritoDetalleId);
            return Error<TCarritoActual>(Mensajes.ErrorCarrito);
        }
    }

    // recibe el ID de la linea y la borra fisicamente solo si pertenece al carrito activo del usuario
    public async Task<Respuesta<TCarritoActual>> EliminarDetalleAsync(int carritoDetalleId, int usuarioId)
    {
        if (usuarioId <= 0) return Error<TCarritoActual>(Mensajes.SesionInvalidaCarrito);
        if (carritoDetalleId <= 0) return Error<TCarritoActual>(Mensajes.DetalleCarritoNoEncontrado);

        try
        {
            _unidadDeTrabajo.EmpezarTransaccion(IsolationLevel.Serializable);
            var detalleRespuesta = await _unidadDeTrabajo.TCarritoDetalle.ObtenerEntidadAsync(
                x => x.CarritoDetalleId == carritoDetalleId &&
                     x.Carrito.UsuarioId == usuarioId && x.Carrito.Estado == EstadoActivo);
            if (!string.IsNullOrEmpty(detalleRespuesta.Error))
                return Revertir<TCarritoActual>(Mensajes.ErrorCarrito);

            var detalle = detalleRespuesta.Data;
            if (detalle == null)
                return Revertir<TCarritoActual>(Mensajes.DetalleCarritoNoEncontrado);

            // guarda estos IDs antes de borrar porque se necesitan para la bitacora
            var carritoId = detalle.CarritoId;
            var productoId = detalle.ProductoId;
            var eliminacion = await _unidadDeTrabajo.TCarritoDetalle.EliminarAsync(detalle);
            if (!eliminacion.Data || !string.IsNullOrEmpty(eliminacion.Error))
                return Revertir<TCarritoActual>(Mensajes.ErrorCarrito);

            _unidadDeTrabajo.CompletarTran();
            await RegistrarBitacora(usuarioId, carritoId, productoId, 0, "ELIMINAR_CARRITO");
            return await ObtenerActualAsync(usuarioId);
        }
        catch (Exception ex)
        {
            _unidadDeTrabajo.Rollback();
            _logger.LogError(ex, "Error al eliminar CarritoDetalleId {CarritoDetalleId}.", carritoDetalleId);
            return Error<TCarritoActual>(Mensajes.ErrorCarrito);
        }
    }

    // atajo usado cuando una validacion falla dentro de la transaccion
    // hace Rollback y devuelve el mensaje en una sola llamada
    private Respuesta<T> Revertir<T>(string mensaje)
    {
        _unidadDeTrabajo.Rollback();
        return Error<T>(mensaje);
    }

    // deja una nota de cada cambio del carrito sin hacer fallar la compra si solo falla la bitacora
    private async Task RegistrarBitacora(int usuarioId, int carritoId, int productoId, int cantidad, string accion = "AGREGAR_CARRITO")
    {
        var respuesta = await _unidadDeTrabajo.TBitacoraSistema.InsertarAsync(new BitacoraSistema
        {
            UsuarioId = usuarioId,
            Fecha = DateTime.UtcNow,
            Accion = accion,
            Entidad = "Carrito",
            EntidadId = carritoId.ToString(),
            Detalle = $"ProductoId: {productoId}; Cantidad agregada: {cantidad}"
        });
        if (!string.IsNullOrEmpty(respuesta.Error))
            _logger.LogWarning("No fue posible registrar la bitácora del carrito: {Error}", respuesta.Error);
    }

    private static Respuesta<T> Error<T>(string mensaje) => new() { Success = false, Error = mensaje };

}
