using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Utilidades;

namespace ProyectoEcommerce.Dominio.InterfazLN;

/// <summary>Define las consultas de imágenes asociadas a productos.</summary>
public interface IProductoImagenLN
{
    /// <summary>Lista las imágenes del producto respetando su visibilidad.</summary>
    Task<Respuesta<IEnumerable<TProductoImagen>>> ListarPorProductoAsync(int productoId, bool incluirProductoInactivo);
    /// <summary>Obtiene la imagen marcada como principal.</summary>
    Task<Respuesta<TProductoImagen?>> ObtenerPrincipalAsync(int productoId, bool incluirProductoInactivo);
}
