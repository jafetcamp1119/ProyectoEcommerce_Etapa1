using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Utilidades;

namespace ProyectoEcommerce.Dominio.InterfazLN
{
    // acciones disponibles para mantener los impuestos que usan los productos
    public interface IImpuestoLN
    {
        // crea un impuesto despues de revisar porcentaje y fechas
        Task<Respuesta<TImpuesto>> InsertarAsync(TImpuesto datos);
        // guarda los cambios de un impuesto existente
        Task<Respuesta<TImpuesto>> ModificarAsync(TImpuesto datos);
        // desactiva sin borrar los productos que ya lo usan
        Task<Respuesta<bool>> EliminarAsync(TImpuesto datos);
        // busca impuestos cuyo nombre contiene el texto recibido
        Task<Respuesta<IEnumerable<TImpuesto>>> BuscarAsync(TImpuesto datos);
        // trae un impuesto por su ID
        Task<Respuesta<TImpuesto>> ObtenerAsync(TImpuesto datos);
        // lista todos los impuestos para la pantalla administrativa
        Task<Respuesta<IEnumerable<TImpuesto>>> ListarAsync();
    }
}
