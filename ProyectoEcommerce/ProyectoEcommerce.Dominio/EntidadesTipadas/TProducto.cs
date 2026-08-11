using System.ComponentModel.DataAnnotations;

namespace ProyectoEcommerce.Dominio.EntidadesTipadas;

/// <summary>Modelo administrativo completo de un producto.</summary>
public class TProducto
{
    public int ProductoId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una categoría válida.")]
    public int CategoriaId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un impuesto válido.")]
    public int ImpuestoId { get; set; }
    [Required(ErrorMessage = "El código es obligatorio.")]
    [MaxLength(50, ErrorMessage = "El código no puede superar 50 caracteres.")]
    public string Codigo { get; set; } = string.Empty;
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(120, ErrorMessage = "El nombre no puede superar 120 caracteres.")]
    public string Nombre { get; set; } = string.Empty;
    [MaxLength(500, ErrorMessage = "La descripción no puede superar 500 caracteres.")]
    public string? Descripcion { get; set; }
    [Range(typeof(decimal), "0", "9999999999999999.99", ParseLimitsInInvariantCulture = true, ErrorMessage = "El precio de venta no puede ser negativo.")]
    public decimal PrecioVenta { get; set; }
    [Range(typeof(decimal), "0", "9999999999999999.99", ParseLimitsInInvariantCulture = true, ErrorMessage = "El costo no puede ser negativo.")]
    public decimal Costo { get; set; }
    [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo.")]
    public int Stock { get; set; }
    [Range(0, int.MaxValue, ErrorMessage = "El stock mínimo no puede ser negativo.")]
    public int StockMinimo { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public int FamiliaId { get; set; }
    public string FamiliaNombre { get; set; } = string.Empty;
    public string CategoriaNombre { get; set; } = string.Empty;
    public string ImpuestoNombre { get; set; } = string.Empty;
    public decimal ImpuestoPorcentaje { get; set; }
    public bool Disponible { get; set; }
    public string EstadoStock { get; set; } = string.Empty;
    public TProductoImagen? ImagenPrincipal { get; set; }
    public TDescuentoAplicado Descuento { get; set; } = new();
}
