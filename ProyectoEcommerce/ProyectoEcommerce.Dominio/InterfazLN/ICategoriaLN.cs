using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Utilidades;

namespace ProyectoEcommerce.Dominio.InterfazLN
{
    // acciones disponibles para guardar categorias y buscarlas dentro de una familia
    public interface ICategoriaLN
    {
        // crea una categoria despues de revisar su nombre y su familia
        Task<Respuesta<TCategoria>> InsertarAsync(TCategoria datos);
        // guarda cambios de una categoria, incluida su UrlImagen opcional
        Task<Respuesta<TCategoria>> ModificarAsync(TCategoria datos);
        // desactiva sin borrar productos ni relaciones
        Task<Respuesta<bool>> EliminarAsync(TCategoria datos);
        // busca categorias usando el nombre recibido
        Task<Respuesta<IEnumerable<TCategoria>>> BuscarAsync(TCategoria datos);
        // trae una categoria especifica por su ID
        Task<Respuesta<TCategoria>> ObtenerAsync(TCategoria datos);
        // lista activas e inactivas para administracion
        Task<Respuesta<IEnumerable<TCategoria>>> ListarAsync();
        // trae todas las categorias que pertenecen a la familia indicada
        Task<Respuesta<IEnumerable<TCategoria>>> ListarPorFamiliaAsync(int familiaId);
        // para el Cliente solo deja salir categorias activas de una familia activa
        Task<Respuesta<IEnumerable<TCategoria>>> ListarClientePorFamiliaAsync(int familiaId);
    }
}
