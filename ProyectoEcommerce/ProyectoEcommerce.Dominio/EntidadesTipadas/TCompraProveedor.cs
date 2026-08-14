using System.ComponentModel.DataAnnotations;

namespace ProyectoEcommerce.Dominio.EntidadesTipadas;

public class TSolicitudCompraProveedor
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un proveedor válido.")]
    public int ProveedorId { get; set; }
    public Guid ClaveConfirmacion { get; set; }

    [MinLength(1, ErrorMessage = "Debe agregar al menos un producto.")]
    public List<TSolicitudCompraProveedorItem> Productos { get; set; } = [];
}

public class TSolicitudCompraProveedorItem
{
    [Range(1, int.MaxValue)]
    public int ProductoProveedorCatalogoId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor que cero.")]
    public int Cantidad { get; set; }
}

public class TDocumentoCompraProveedor
{
    public int? CompraProveedorId { get; set; }
    public string Numero { get; set; } = "PROFORMA";
    public DateTime Fecha { get; set; }
    public int ProveedorId { get; set; }
    public string Proveedor { get; set; } = string.Empty;
    public string? CorreoProveedor { get; set; }
    public string? TelefonoProveedor { get; set; }
    public string? DireccionProveedor { get; set; }
    public string Estado { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public IReadOnlyCollection<TDocumentoCompraProveedorItem> Productos { get; set; } = [];
}

public class TDocumentoCompraProveedorItem
{
    public int ProductoProveedorCatalogoId { get; set; }
    public int ProductoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

public class TCompraProveedorConfirmada
{
    public int CompraProveedorId { get; set; }
    public string Numero { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public bool PdfDisponible { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}

public class TFiltroComprasProveedor
{
    public int? ProveedorId { get; set; }
    [MaxLength(20)] public string? Estado { get; set; }
    public DateOnly? FechaDesde { get; set; }
    public DateOnly? FechaHasta { get; set; }
    [Range(1, int.MaxValue)] public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 25;
}

public class TCompraProveedorResumen
{
    public int CompraProveedorId { get; set; }
    public string Numero { get; set; } = string.Empty;
    public int ProveedorId { get; set; }
    public string Proveedor { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = string.Empty;
    public bool PdfDisponible { get; set; }
}

public class TCompraProveedorDetalleConsulta : TCompraProveedorResumen
{
    public string? CorreoProveedor { get; set; }
    public string? TelefonoProveedor { get; set; }
    public string? DireccionProveedor { get; set; }
    public IReadOnlyCollection<TDocumentoCompraProveedorItem> Productos { get; set; } = [];
}

public class TArchivoCompraProveedor
{
    public string RutaRelativa { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
}
