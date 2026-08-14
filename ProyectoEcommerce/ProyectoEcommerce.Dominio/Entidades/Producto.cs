namespace ProyectoEcommerce.Dominio.Entidades;

/// <summary>Representa un producto, su precio final, inventario y relaciones de catálogo.</summary>
public partial class Producto
{
    public int ProductoId { get; set; }
    public int CategoriaId { get; set; }
    public int ImpuestoId { get; set; }
    public string Codigo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public decimal PrecioVenta { get; set; }
    public decimal Costo { get; set; }
    public int Stock { get; set; }
    public int StockMinimo { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public virtual Categoria Categoria { get; set; } = null!;
    public virtual Impuesto Impuesto { get; set; } = null!;
    public virtual ICollection<ProductoImagen> Imagenes { get; set; } = new List<ProductoImagen>();
    public virtual ICollection<OrdenDetalle> OrdenDetalles { get; set; } = new List<OrdenDetalle>();
    public virtual ICollection<CarritoDetalle> CarritoDetalles { get; set; } = new List<CarritoDetalle>();
    public virtual ICollection<Descuento> Descuentos { get; set; } = new List<Descuento>();
    public virtual ICollection<ProductoProveedor> Proveedores { get; set; } = new List<ProductoProveedor>();
    public virtual ICollection<ProductoProveedorCatalogo> OfertasProveedor { get; set; } = new List<ProductoProveedorCatalogo>();
    public virtual ICollection<CompraProveedorDetalle> DetallesCompraProveedor { get; set; } = new List<CompraProveedorDetalle>();
    public virtual ICollection<MovimientoInventario> MovimientosInventario { get; set; } = new List<MovimientoInventario>();
}
