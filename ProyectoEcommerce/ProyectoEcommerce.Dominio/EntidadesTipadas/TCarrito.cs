using System.ComponentModel.DataAnnotations;

namespace ProyectoEcommerce.Dominio.EntidadesTipadas;

public class TAgregarProductoCarrito
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un producto válido.")]
    public int ProductoId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor que cero.")]
    public int Cantidad { get; set; } = 1;
}

public class TResultadoAgregarCarrito
{
    public int CarritoId { get; set; }
    public int ProductoId { get; set; }
    public int CantidadProducto { get; set; }
    public int CantidadTotal { get; set; }
    public bool ProductoNuevo { get; set; }
}

public class TActualizarCantidadCarrito
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un detalle de carrito válido.")]
    public int CarritoDetalleId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser un número entero mayor que cero.")]
    public int Cantidad { get; set; }
}

public class TCarritoActual
{
    public int CarritoId { get; set; }
    public string Estado { get; set; } = "ACTIVO";
    public int CantidadTotal { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Impuestos { get; set; }
    public decimal Descuentos { get; set; }
    public decimal Total { get; set; }
    public IEnumerable<TCarritoItem> Items { get; set; } = [];
}

public class TCarritoItem
{
    public int CarritoDetalleId { get; set; }
    public int ProductoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public int StockDisponible { get; set; }
    public decimal PrecioOriginal { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal PorcentajeImpuesto { get; set; }
    public decimal PorcentajeDescuento { get; set; }
    public string? TipoDescuento { get; set; }
    public string? NombreDescuento { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Impuestos { get; set; }
    public decimal Descuentos { get; set; }
    public decimal TotalLinea { get; set; }
    public bool ProductoActivo { get; set; }
    public bool StockSuficiente { get; set; }
}
