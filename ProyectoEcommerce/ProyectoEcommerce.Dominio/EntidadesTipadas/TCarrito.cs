using System.ComponentModel.DataAnnotations;

namespace ProyectoEcommerce.Dominio.EntidadesTipadas;

/// <summary>Datos recibidos para agregar un producto al carrito.</summary>
public class TAgregarProductoCarrito
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un producto válido.")]
    public int ProductoId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor que cero.")]
    public int Cantidad { get; set; } = 1;
}

/// <summary>Resultado resumido después de agregar o acumular un producto.</summary>
public class TResultadoAgregarCarrito
{
    public int CarritoId { get; set; }
    public int ProductoId { get; set; }
    public int CantidadProducto { get; set; }
    public int CantidadTotal { get; set; }
    public bool ProductoNuevo { get; set; }
}

/// <summary>Datos necesarios para cambiar la cantidad de un detalle.</summary>
public class TActualizarCantidadCarrito
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un detalle de carrito válido.")]
    public int CarritoDetalleId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser un número entero mayor que cero.")]
    public int Cantidad { get; set; }
}

/// <summary>Representa el carrito calculado que se muestra al Cliente.</summary>
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

/// <summary>Representa una línea del carrito con stock y desglose monetario.</summary>
public class TCarritoItem
{
    public int CarritoDetalleId { get; set; }
    public int ProductoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public int StockDisponible { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal PorcentajeImpuesto { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Impuestos { get; set; }
    public decimal Descuentos { get; set; }
    public decimal TotalLinea { get; set; }
    public bool ProductoActivo { get; set; }
    public bool StockSuficiente { get; set; }
}
