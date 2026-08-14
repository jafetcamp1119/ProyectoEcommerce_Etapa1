using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Utilidades;

namespace ProyectoEcommerce.Dominio.InterfazLN
{
    // acciones para mostrar el catalogo y para mantener productos desde administracion
    public interface IProductoLN
    {
        // filtra productos activos y devuelve solo la pagina pedida por el Cliente
        Task<Respuesta<TPagina<TProductoCatalogo>>> ListarCatalogoAsync(TFiltroProductos filtro);
        // lista productos activos e inactivos con los filtros administrativos
        Task<Respuesta<TPagina<TProducto>>> ListarAdministracionAsync(TFiltroProductos filtro);
        // trae un producto activo con los datos que se muestran al Cliente
        Task<Respuesta<TProductoCatalogo>> ObtenerCatalogoAsync(int productoId);
        // trae los datos completos que necesita el formulario de edicion
        Task<Respuesta<TProducto>> ObtenerAdministracionAsync(int productoId);
        // junta familias, categorias e impuestos para llenar los select del formulario
        Task<Respuesta<TCatalogosProducto>> ListarCatalogosAsync();
        // crea el producto y apunta en bitacora cual Administrador lo hizo
        Task<Respuesta<TProducto>> InsertarAsync(TProducto datos, int administradorId);
        // valida y guarda los cambios del producto
        Task<Respuesta<TProducto>> ModificarAsync(TProducto datos, int administradorId);
        // activa o desactiva sin borrar el registro ni sus imagenes
        Task<Respuesta<TProducto>> CambiarEstadoAsync(int productoId, bool activo, int administradorId);
    }
}
