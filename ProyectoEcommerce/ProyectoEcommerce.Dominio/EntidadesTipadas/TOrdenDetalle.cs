namespace ProyectoEcommerce.Dominio.EntidadesTipadas;

public class TOrdenDetalle
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
}
