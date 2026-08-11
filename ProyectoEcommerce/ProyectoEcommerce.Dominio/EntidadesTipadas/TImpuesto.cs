namespace ProyectoEcommerce.Dominio.EntidadesTipadas;

/// <summary>Modelo utilizado para consultar y mantener impuestos.</summary>
public class TImpuesto
{
    public int ImpuestoId { get; set; }
    public string Nombre { get; set; } = null!;
    public decimal Porcentaje { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly? FechaFin { get; set; }
    public bool Activo { get; set; }
}
