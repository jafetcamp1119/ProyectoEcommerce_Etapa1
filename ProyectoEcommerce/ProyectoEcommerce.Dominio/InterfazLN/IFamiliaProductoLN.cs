using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Utilidades;

namespace ProyectoEcommerce.Dominio.InterfazLN
{
    // acciones que usa la API para mantener familias y mostrar el primer nivel del catalogo
    public interface IFamiliaProductoLN
    {
        // crea una familia si el nombre todavia no esta usado
        Task<Respuesta<TFamiliaProducto>> InsertarAsync(TFamiliaProducto datos);
        // guarda cambios de la familia, incluida su UrlImagen opcional
        Task<Respuesta<TFamiliaProducto>> ModificarAsync(TFamiliaProducto datos);
        // desactiva la familia sin borrar sus categorias
        Task<Respuesta<bool>> EliminarAsync(TFamiliaProducto datos);
        // busca coincidencias por nombre
        Task<Respuesta<IEnumerable<TFamiliaProducto>>> BuscarAsync(TFamiliaProducto datos);
        // trae una familia especifica por su ID
        Task<Respuesta<TFamiliaProducto>> ObtenerAsync(TFamiliaProducto datos);
        // lista activas e inactivas para administracion
        Task<Respuesta<IEnumerable<TFamiliaProducto>>> ListarAsync();
        // manda al Cliente solamente las familias que puede navegar
        Task<Respuesta<IEnumerable<TFamiliaProducto>>> ListarClienteAsync();
    }
}
