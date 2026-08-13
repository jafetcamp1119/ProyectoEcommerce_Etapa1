
using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Utilidades;


namespace ProyectoEcommerce.Dominio.InterfazLN;

// Interfaz para manejar las imágenes de los productos
public interface IProductoImagenLN
{
    // Lista las imágenes que tiene un producto
    Task<Respuesta<IEnumerable<TProductoImagen>>> ListarPorProductoAsync(
        // ID del producto
        int productoId,
        // Permite consultar productos inactivos
        bool incluirProductoInactivo);

    // Obtiene la imagen principal del producto
    Task<Respuesta<TProductoImagen?>> ObtenerPrincipalAsync(
        // ID del producto
        int productoId,
        // Permite consultar productos inactivos
        bool incluirProductoInactivo);

    // Agrega una nueva imagen al producto
    Task<Respuesta<TProductoImagen>> AgregarAsync(
        // Producto al que pertenece la imagen
        int productoId,
        // Dirección donde se guardó la imagen
        string urlImagen,
        // Descripción opcional de la imagen
        string? textoAlternativo,
        // Indica si será la imagen principal
        bool esPrincipal);

    // Coloca una imagen como principal
    Task<Respuesta<bool>> EstablecerPrincipalAsync(
        // ID de la imagen
        int imagenId);

    // Elimina una imagen del producto
    Task<Respuesta<TProductoImagen?>> EliminarAsync(
        // ID de la imagen que se eliminará
        int imagenId);
}