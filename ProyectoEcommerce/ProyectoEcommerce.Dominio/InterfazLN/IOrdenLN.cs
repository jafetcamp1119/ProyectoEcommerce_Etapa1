using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Utilidades;

namespace ProyectoEcommerce.Dominio.InterfazLN
{
    // aqui se agrupan las operaciones viejas de ordenes y el flujo completo de compra del Cliente
    public interface IOrdenLN
    {
        // estas operaciones se mantienen por la arquitectura original del proyecto
        Task<Respuesta<TOrden>> InsertarAsync(TOrden datos);
        Task<Respuesta<TOrden>> ModificarAsync(TOrden datos);
        Task<Respuesta<bool>> EliminarAsync(TOrden datos);
        Task<Respuesta<IEnumerable<TOrden>>> BuscarAsync(TOrden datos);
        Task<Respuesta<TOrden>> ObtenerAsync(TOrden datos);
        Task<Respuesta<IEnumerable<TOrden>>> ListarAsync();
        // arma el resumen del checkout con el carrito y los datos del usuario
        Task<Respuesta<TCheckoutPreparacion>> PrepararCheckoutAsync(int usuarioId);
        // confirma stock y totales, guarda la venta y genera la factura despues del Commit
        Task<Respuesta<TCompraCompletada>> ConfirmarCompraAsync(TConfirmarCompra datos, int usuarioId,
        CancellationToken cancellationToken = default);
        // lista solamente las ordenes del usuario que viene en el JWT
        Task<Respuesta<TPagina<TOrdenResumen>>> ListarClienteAsync(TFiltroOrdenes filtro, int usuarioId);
        // deja que el Administrador vea las ordenes de todos los clientes
        Task<Respuesta<TPagina<TOrdenResumen>>> ListarAdministracionAsync(TFiltroOrdenes filtro);
        // trae el detalle si la orden es del Cliente o si quien consulta es Administrador
        Task<Respuesta<TOrdenDetalleConsulta>> ObtenerDetalleAsync(int ordenId, int usuarioId, bool administrador);
        // busca el PDF de una orden que el usuario tiene permiso de ver
        Task<Respuesta<TArchivoFactura>> ObtenerFacturaAsync(int ordenId, int usuarioId, bool administrador);
        // cancela una orden propia solamente mientras siga pendiente
        Task<Respuesta<bool>> CancelarPendienteAsync(int ordenId, int usuarioId);
    }
}
