using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Utilidades;

namespace ProyectoEcommerce.Dominio.InterfazLN;

// acciones que puede hacer un Cliente sobre su propio carrito abierto
public interface ICarritoLN
{
    // agrega una cantidad despues de revisar producto, stock y dueño del carrito
    Task<Respuesta<TResultadoAgregarCarrito>> AgregarAsync(TAgregarProductoCarrito datos, int usuarioId);
    // trae el carrito abierto o devuelve uno vacio si todavia no existe
    Task<Respuesta<TCarritoActual>> ObtenerActualAsync(int usuarioId);
    // cambia la cantidad cuidando que el detalle sea del usuario y alcance el stock
    Task<Respuesta<TCarritoActual>> ActualizarCantidadAsync(TActualizarCantidadCarrito datos, int usuarioId);
    // quita un producto solamente del carrito que pertenece al usuario autenticado
    Task<Respuesta<TCarritoActual>> EliminarDetalleAsync(int carritoDetalleId, int usuarioId);
}
