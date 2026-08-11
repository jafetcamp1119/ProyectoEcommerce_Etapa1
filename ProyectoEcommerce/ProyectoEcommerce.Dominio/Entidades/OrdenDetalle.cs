namespace ProyectoEcommerce.Dominio.Entidades;

/// <summary>Conserva producto, cantidades, precios e impuestos históricos de una orden.</summary>
public partial class OrdenDetalle
{
    public int OrdenDetalleId { get; set; }
    public int OrdenId { get; set; }
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal PorcentajeImpuesto { get; set; }
    public decimal PorcentajeDescuento { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TotalLinea { get; set; }
    public virtual Orden Orden { get; set; } = null!;
    public virtual Producto Producto { get; set; } = null!;
}
