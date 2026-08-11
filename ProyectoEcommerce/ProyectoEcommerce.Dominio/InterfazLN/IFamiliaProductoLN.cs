using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Utilidades;

namespace ProyectoEcommerce.Dominio.InterfazLN
{
    /// <summary>Define el mantenimiento y las consultas de familias de producto.</summary>
    public interface IFamiliaProductoLN
    {
        /// <summary>Crea una familia después de validar duplicados.</summary>
        Task<Respuesta<TFamiliaProducto>> InsertarAsync(TFamiliaProducto datos);
        /// <summary>Actualiza una familia existente.</summary>
        Task<Respuesta<TFamiliaProducto>> ModificarAsync(TFamiliaProducto datos);
        /// <summary>Desactiva lógicamente una familia.</summary>
        Task<Respuesta<bool>> EliminarAsync(TFamiliaProducto datos);
        /// <summary>Busca familias por nombre.</summary>
        Task<Respuesta<IEnumerable<TFamiliaProducto>>> BuscarAsync(TFamiliaProducto datos);
        /// <summary>Obtiene una familia específica.</summary>
        Task<Respuesta<TFamiliaProducto>> ObtenerAsync(TFamiliaProducto datos);
        /// <summary>Lista familias para administración.</summary>
        Task<Respuesta<IEnumerable<TFamiliaProducto>>> ListarAsync();
        /// <summary>Lista familias activas para la navegación del Cliente.</summary>
        Task<Respuesta<IEnumerable<TFamiliaProducto>>> ListarClienteAsync();
    }
}
