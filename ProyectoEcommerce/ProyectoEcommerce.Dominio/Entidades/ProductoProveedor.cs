namespace ProyectoEcommerce.Dominio.Entidades;

public partial class ProductoProveedor
{
    public int ProductoId { get; set; }
    public int ProveedorId { get; set; }
    public decimal PrecioCompra { get; set; }
    public bool Activo { get; set; }
    public virtual Producto Producto { get; set; } = null!;
    public virtual Proveedor Proveedor { get; set; } = null!;
}
