using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Utilidades;

namespace ProyectoEcommerce.Dominio.InterfazLN;

/// <summary>Centraliza mantenimiento, vigencia y resolución económica de descuentos.</summary>
public interface IDescuentoLN
{
    Task<Respuesta<IEnumerable<TDescuento>>> ListarAsync();
    Task<Respuesta<TDescuento>> ObtenerAsync(int descuentoId);
    Task<Respuesta<TCatalogosDescuento>> ListarCatalogosAsync();
    Task<Respuesta<TDescuento>> InsertarAsync(TDescuento datos, int administradorId);
    Task<Respuesta<TDescuento>> ModificarAsync(TDescuento datos, int administradorId);
    Task<Respuesta<TDescuento>> CambiarEstadoAsync(int descuentoId, bool activo, int administradorId);
    Task<Respuesta<TDescuentoAplicado>> ObtenerMejorDescuentoAsync(int productoId);
    Task<Respuesta<IReadOnlyDictionary<int, TDescuentoAplicado>>> ObtenerMejoresDescuentosAsync(IEnumerable<int> productoIds);
}
