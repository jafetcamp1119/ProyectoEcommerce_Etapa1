using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Utilidades;

namespace ProyectoEcommerce.Dominio.InterfazLN
{
    /// <summary>
    /// Define operaciones administrativas de órdenes y el flujo completo de venta del Cliente.
    /// </summary>
    public interface IOrdenLN
    {
        Task<Respuesta<TOrden>> InsertarAsync(TOrden datos);
        Task<Respuesta<TOrden>> ModificarAsync(TOrden datos);
        Task<Respuesta<bool>> EliminarAsync(TOrden datos);
        Task<Respuesta<IEnumerable<TOrden>>> BuscarAsync(TOrden datos);
        Task<Respuesta<TOrden>> ObtenerAsync(TOrden datos);
        Task<Respuesta<IEnumerable<TOrden>>> ListarAsync();
        /// <summary>Prepara checkout usando el carrito y los datos del usuario autenticado.</summary>
        Task<Respuesta<TCheckoutPreparacion>> PrepararCheckoutAsync(int usuarioId);
        /// <summary>Confirma la venta, registra sus detalles y genera la factura posterior al commit.</summary>
        Task<Respuesta<TCompraCompletada>> ConfirmarCompraAsync(TConfirmarCompra datos, int usuarioId, CancellationToken cancellationToken = default);
        /// <summary>Lista órdenes limitadas al propietario identificado por JWT.</summary>
        Task<Respuesta<TPagina<TOrdenResumen>>> ListarClienteAsync(TFiltroOrdenes filtro, int usuarioId);
        /// <summary>Lista órdenes de todos los usuarios para el Administrador.</summary>
        Task<Respuesta<TPagina<TOrdenResumen>>> ListarAdministracionAsync(TFiltroOrdenes filtro);
        /// <summary>Obtiene el detalle validando propiedad o rol administrativo.</summary>
        Task<Respuesta<TOrdenDetalleConsulta>> ObtenerDetalleAsync(int ordenId, int usuarioId, bool administrador);
        /// <summary>Localiza el documento de factura vinculado con una orden autorizada.</summary>
        Task<Respuesta<TArchivoFactura>> ObtenerFacturaAsync(int ordenId, int usuarioId, bool administrador);
        /// <summary>Cancela una orden pendiente del Cliente cuando la regla de estado lo permite.</summary>
        Task<Respuesta<bool>> CancelarPendienteAsync(int ordenId, int usuarioId);
    }
}
