namespace ProyectoEcommerce.Dominio.Entidades;

public partial class CompraProveedor
{
    public int CompraProveedorId { get; set; }
    public int ProveedorId { get; set; }
    public int? UsuarioId { get; set; }
    public string Numero { get; set; } = null!;
    public DateTime Fecha { get; set; }
    public DateTime? FechaConfirmacion { get; set; }
    public string Estado { get; set; } = null!;
    public decimal Total { get; set; }
    public Guid ClaveConfirmacion { get; set; }
    public virtual Proveedor Proveedor { get; set; } = null!;
    public virtual Usuario? Usuario { get; set; }
    public virtual ICollection<CompraProveedorDetalle> Detalles { get; set; } = new List<CompraProveedorDetalle>();
    public virtual ICollection<MovimientoInventario> Movimientos { get; set; } = new List<MovimientoInventario>();
}
