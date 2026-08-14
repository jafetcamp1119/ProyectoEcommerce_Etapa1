namespace ProyectoEcommerce.Dominio.Entidades;

public partial class CompraProveedorDetalle
{
    public int CompraProveedorDetalleId { get; set; }
    public int CompraProveedorId { get; set; }
    public int ProductoId { get; set; }
    public int? ProductoProveedorCatalogoId { get; set; }
    public string NombreProducto { get; set; } = null!;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
    public virtual CompraProveedor CompraProveedor { get; set; } = null!;
    public virtual Producto Producto { get; set; } = null!;
    public virtual ProductoProveedorCatalogo? Oferta { get; set; }
}
