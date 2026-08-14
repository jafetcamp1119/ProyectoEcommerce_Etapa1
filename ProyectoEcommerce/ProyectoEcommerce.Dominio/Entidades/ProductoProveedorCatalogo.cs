namespace ProyectoEcommerce.Dominio.Entidades;

// una oferta existe antes de que el administrador decida incorporarla a dbo.Productos
public partial class ProductoProveedorCatalogo
{
    public int ProductoProveedorCatalogoId { get; set; }
    public int ProveedorCategoriaId { get; set; }
    public int ImpuestoId { get; set; }
    public string Nombre { get; set; } = null!;
    public decimal PrecioCompra { get; set; }
    public int? ProductoId { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaActualizacion { get; set; }
    public virtual ProveedorCategoria ProveedorCategoria { get; set; } = null!;
    public virtual Impuesto Impuesto { get; set; } = null!;
    public virtual Producto? Producto { get; set; }
    public virtual ICollection<CompraProveedorDetalle> DetallesCompra { get; set; } = new List<CompraProveedorDetalle>();
}
