using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Utilidades;

namespace ProyectoEcommerce.Dominio.InterfazLN
{
    /// <summary>Define el mantenimiento de categorías y sus consultas por familia.</summary>
    public interface ICategoriaLN
    {
        /// <summary>Crea una categoría válida.</summary>
        Task<Respuesta<TCategoria>> InsertarAsync(TCategoria datos);
        /// <summary>Actualiza una categoría existente.</summary>
        Task<Respuesta<TCategoria>> ModificarAsync(TCategoria datos);
        /// <summary>Desactiva lógicamente una categoría.</summary>
        Task<Respuesta<bool>> EliminarAsync(TCategoria datos);
        /// <summary>Busca categorías por los criterios recibidos.</summary>
        Task<Respuesta<IEnumerable<TCategoria>>> BuscarAsync(TCategoria datos);
        /// <summary>Obtiene una categoría por identificador.</summary>
        Task<Respuesta<TCategoria>> ObtenerAsync(TCategoria datos);
        /// <summary>Lista categorías para administración.</summary>
        Task<Respuesta<IEnumerable<TCategoria>>> ListarAsync();
        /// <summary>Lista categorías administrativas de una familia.</summary>
        Task<Respuesta<IEnumerable<TCategoria>>> ListarPorFamiliaAsync(int familiaId);
        /// <summary>Lista categorías activas de una familia activa para el Cliente.</summary>
        Task<Respuesta<IEnumerable<TCategoria>>> ListarClientePorFamiliaAsync(int familiaId);
    }
}
