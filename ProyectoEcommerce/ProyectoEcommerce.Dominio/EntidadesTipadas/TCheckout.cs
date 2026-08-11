using System.ComponentModel.DataAnnotations;

namespace ProyectoEcommerce.Dominio.EntidadesTipadas;

/// <summary>Agrupa Cliente y carrito para presentar el checkout.</summary>
public class TCheckoutPreparacion
{
    public TCheckoutCliente Cliente { get; set; } = new();
    public TCarritoActual Carrito { get; set; } = new();
}

public class TCheckoutCliente
{
    public string NombreCompleto { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string? Direccion { get; set; }
}

/// <summary>Datos confirmados por el Cliente para crear la venta.</summary>
public class TConfirmarCompra
{
    [Required(ErrorMessage = "El correo de la factura es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido.")]
    [MaxLength(120)]
    public string CorreoDestino { get; set; } = string.Empty;

    [Required(ErrorMessage = "La dirección de entrega es obligatoria.")]
    [MinLength(10, ErrorMessage = "La dirección debe contener al menos 10 caracteres.")]
    [MaxLength(500)]
    public string DireccionEnvio { get; set; } = string.Empty;

    [Required(ErrorMessage = "Selecciona un método de pago.")]
    [MaxLength(30)]
    public string MetodoPago { get; set; } = string.Empty;

    public bool CorreoConfirmado { get; set; }
}

/// <summary>Informa el resultado final de compra, factura y correo.</summary>
public class TCompraCompletada
{
    public int OrdenId { get; set; }
    public string NumeroOrden { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string CorreoDestino { get; set; } = string.Empty;
    public bool FacturaGenerada { get; set; }
    public bool CorreoEnviado { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}

/// <summary>Modelo histórico utilizado para generar y enviar la factura.</summary>
public class TFacturaDatos
{
    public int OrdenId { get; set; }
    public string NumeroOrden { get; set; } = string.Empty;
    public string NumeroFactura { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string DireccionEnvio { get; set; } = string.Empty;
    public string MetodoPago { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal Impuestos { get; set; }
    public decimal Descuentos { get; set; }
    public decimal Total { get; set; }
    public IReadOnlyCollection<TFacturaItem> Items { get; set; } = [];
}

/// <summary>Producto histórico incluido en la factura.</summary>
public class TFacturaItem
{
    public int ProductoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal PorcentajeImpuesto { get; set; }
    public decimal PorcentajeDescuento { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Impuestos { get; set; }
    public decimal Descuento { get; set; }
    public decimal TotalLinea { get; set; }
}

public class TResultadoCorreoFactura
{
    public bool Enviado { get; set; }
    public string? Error { get; set; }
}

/// <summary>Filtros y paginación permitidos para consultar órdenes.</summary>
public class TFiltroOrdenes
{
    [MaxLength(40)] public string? Numero { get; set; }
    [MaxLength(160)] public string? Cliente { get; set; }
    [MaxLength(20)] public string? Estado { get; set; }
    public DateOnly? FechaDesde { get; set; }
    public DateOnly? FechaHasta { get; set; }
    [Range(1, int.MaxValue)] public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 25;
}

public class TOrdenResumen
{
    public int OrdenId { get; set; }
    public string NumeroOrden { get; set; } = string.Empty;
    public DateTime FechaOrden { get; set; }
    public string Estado { get; set; } = string.Empty;
    public int CantidadProductos { get; set; }
    public decimal Total { get; set; }
    public string? MetodoPago { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public bool FacturaDisponible { get; set; }
}

/// <summary>Detalle protegido de una orden y sus productos.</summary>
public class TOrdenDetalleConsulta : TOrdenResumen
{
    public string DireccionEnvio { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal Impuestos { get; set; }
    public decimal Descuentos { get; set; }
    public string? NumeroFactura { get; set; }
    public bool CorreoEnviado { get; set; }
    public IReadOnlyCollection<TOrdenProductoConsulta> Productos { get; set; } = [];
}

public class TOrdenProductoConsulta
{
    public int ProductoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal PorcentajeImpuesto { get; set; }
    public decimal PorcentajeDescuento { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Impuestos { get; set; }
    public decimal Descuentos { get; set; }
    public decimal TotalLinea { get; set; }
}

/// <summary>Ubicación autorizada y número de un PDF registrado.</summary>
public class TArchivoFactura
{
    public string RutaRelativa { get; set; } = string.Empty;
    public string NumeroFactura { get; set; } = string.Empty;
}
