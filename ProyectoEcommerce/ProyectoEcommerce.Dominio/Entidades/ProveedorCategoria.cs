namespace ProyectoEcommerce.Dominio.Entidades;

public partial class ProveedorCategoria
{
    public int ProveedorCategoriaId { get; set; }
    public int ProveedorId { get; set; }
    public int CategoriaId { get; set; }
    public bool Activo { get; set; }
    public virtual Proveedor Proveedor { get; set; } = null!;
    public virtual Categoria Categoria { get; set; } = null!;
    public virtual ICollection<ProductoProveedorCatalogo> Productos { get; set; } = new List<ProductoProveedorCatalogo>();
}
