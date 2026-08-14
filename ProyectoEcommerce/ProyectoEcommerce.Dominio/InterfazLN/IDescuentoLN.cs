using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Utilidades;

namespace ProyectoEcommerce.Dominio.InterfazLN;

// acciones para mantener descuentos y escoger cual aplica a cada producto
public interface IDescuentoLN
{
    // lista todos los descuentos para la pantalla administrativa
    Task<Respuesta<IEnumerable<TDescuento>>> ListarAsync();
    // trae un descuento por su ID
    Task<Respuesta<TDescuento>> ObtenerAsync(int descuentoId);
    // junta productos, categorias y familias usados por el formulario
    Task<Respuesta<TCatalogosDescuento>> ListarCatalogosAsync();
    // crea un descuento y registra al Administrador que lo guardo
    Task<Respuesta<TDescuento>> InsertarAsync(TDescuento datos, int administradorId);
    // valida y guarda los cambios de un descuento
    Task<Respuesta<TDescuento>> ModificarAsync(TDescuento datos, int administradorId);
    // activa o desactiva el descuento sin borrarlo
    Task<Respuesta<TDescuento>> CambiarEstadoAsync(int descuentoId, bool activo, int administradorId);
    // calcula el mejor descuento vigente para un solo producto
    Task<Respuesta<TDescuentoAplicado>> ObtenerMejorDescuentoAsync(int productoId);
    // hace el mismo calculo para varios productos de una sola vez
    Task<Respuesta<IReadOnlyDictionary<int, TDescuentoAplicado>>> ObtenerMejoresDescuentosAsync(IEnumerable<int> productoIds);
}
