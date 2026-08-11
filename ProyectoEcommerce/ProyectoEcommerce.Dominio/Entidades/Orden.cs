namespace ProyectoEcommerce.Dominio.Entidades;

/// <summary>Representa una orden de venta o compra con su estado y total histórico.</summary>
public partial class Orden
{
    public int OrdenId { get; set; }
    public int UsuarioId { get; set; }
    public DateTime FechaOrden { get; set; }
    public string Estado { get; set; } = null!;
    public string TipoOrden { get; set; } = null!;
    public string? DireccionEnvio { get; set; }
    public string Moneda { get; set; } = null!;
    public decimal? Total { get; set; }
    public virtual Usuario Usuario { get; set; } = null!;
    public virtual ICollection<OrdenDetalle> OrdenDetalles { get; set; } = new List<OrdenDetalle>();
}
