using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Utilidades;

namespace ProyectoEcommerce.Dominio.InterfazLN
{
    /// <summary>Define el mantenimiento de porcentajes de impuesto.</summary>
    public interface IImpuestoLN
    {
        /// <summary>Crea un impuesto validado.</summary>
        Task<Respuesta<TImpuesto>> InsertarAsync(TImpuesto datos);
        /// <summary>Actualiza un impuesto existente.</summary>
        Task<Respuesta<TImpuesto>> ModificarAsync(TImpuesto datos);
        /// <summary>Desactiva lógicamente un impuesto.</summary>
        Task<Respuesta<bool>> EliminarAsync(TImpuesto datos);
        /// <summary>Busca impuestos por nombre.</summary>
        Task<Respuesta<IEnumerable<TImpuesto>>> BuscarAsync(TImpuesto datos);
        /// <summary>Obtiene un impuesto específico.</summary>
        Task<Respuesta<TImpuesto>> ObtenerAsync(TImpuesto datos);
        /// <summary>Lista los impuestos configurados.</summary>
        Task<Respuesta<IEnumerable<TImpuesto>>> ListarAsync();
    }
}
