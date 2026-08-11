using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Utilidades;

namespace ProyectoEcommerce.Dominio.InterfazLN
{
    /// <summary>Define consultas de catálogo y mantenimiento administrativo de productos.</summary>
    public interface IProductoLN
    {
        /// <summary>Lista productos activos según texto, familia, categoría, precio y disponibilidad.</summary>
        Task<Respuesta<TPagina<TProductoCatalogo>>> ListarCatalogoAsync(TFiltroProductos filtro);
        /// <summary>Lista productos para administración, incluidos sus estados.</summary>
        Task<Respuesta<TPagina<TProducto>>> ListarAdministracionAsync(TFiltroProductos filtro);
        /// <summary>Obtiene el detalle público de un producto disponible.</summary>
        Task<Respuesta<TProductoCatalogo>> ObtenerCatalogoAsync(int productoId);
        /// <summary>Obtiene los datos completos de un producto para editarlo.</summary>
        Task<Respuesta<TProducto>> ObtenerAdministracionAsync(int productoId);
        /// <summary>Obtiene familias, categorías e impuestos necesarios para los formularios.</summary>
        Task<Respuesta<TCatalogosProducto>> ListarCatalogosAsync();
        /// <summary>Crea un producto y registra la acción del Administrador.</summary>
        Task<Respuesta<TProducto>> InsertarAsync(TProducto datos, int administradorId);
        /// <summary>Actualiza un producto y registra la acción del Administrador.</summary>
        Task<Respuesta<TProducto>> ModificarAsync(TProducto datos, int administradorId);
        /// <summary>Cambia el estado lógico del producto sin eliminarlo físicamente.</summary>
        Task<Respuesta<TProducto>> CambiarEstadoAsync(int productoId, bool activo, int administradorId);
    }
}
