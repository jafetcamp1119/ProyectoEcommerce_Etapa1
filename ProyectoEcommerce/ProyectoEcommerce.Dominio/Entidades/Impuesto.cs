namespace ProyectoEcommerce.Dominio.Entidades;

/// <summary>Representa un porcentaje de impuesto y su período de vigencia.</summary>
public partial class Impuesto
{
    public int ImpuestoId { get; set; }
    public string Nombre { get; set; } = null!;
    public decimal Porcentaje { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly? FechaFin { get; set; }
    public bool Activo { get; set; }
    public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();
}
