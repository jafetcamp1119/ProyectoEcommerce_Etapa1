using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Utilidades;

namespace ProyectoEcommerce.Dominio.InterfazLN;

/// <summary>
/// Define las reglas disponibles para administrar el carrito activo de un Cliente.
/// </summary>
public interface ICarritoLN
{
    /// <summary>Agrega una cantidad de producto al carrito perteneciente al usuario.</summary>
    Task<Respuesta<TResultadoAgregarCarrito>> AgregarAsync(TAgregarProductoCarrito datos, int usuarioId);
    /// <summary>Obtiene o representa el carrito actual del usuario.</summary>
    Task<Respuesta<TCarritoActual>> ObtenerActualAsync(int usuarioId);
    /// <summary>Modifica una cantidad después de comprobar propiedad y stock.</summary>
    Task<Respuesta<TCarritoActual>> ActualizarCantidadAsync(TActualizarCantidadCarrito datos, int usuarioId);
    /// <summary>Retira un detalle que pertenece al carrito del usuario.</summary>
    Task<Respuesta<TCarritoActual>> EliminarDetalleAsync(int carritoDetalleId, int usuarioId);
}
