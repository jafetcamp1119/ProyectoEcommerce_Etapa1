using System.ComponentModel.DataAnnotations;

namespace ProyectoEcommerce.Dominio.EntidadesTipadas;

public class TFiltroProductos
{
    [MaxLength(120)] public string? Texto { get; set; }
    public int? FamiliaId { get; set; }
    public int? CategoriaId { get; set; }
    [Range(typeof(decimal), "0", "9999999999999999.99", ParseLimitsInInvariantCulture = true)] public decimal? PrecioMinimo { get; set; }
    [Range(typeof(decimal), "0", "9999999999999999.99", ParseLimitsInInvariantCulture = true)] public decimal? PrecioMaximo { get; set; }
    [MaxLength(20)] public string? Disponibilidad { get; set; }
    public bool? Activo { get; set; }
    [Range(1, int.MaxValue)] public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 25;
    [MaxLength(30)] public string? Orden { get; set; } = "nombre_asc";
}

public class TProductoCatalogo
{
    public int ProductoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal PrecioVenta { get; set; }
    public int FamiliaId { get; set; }
    public string FamiliaNombre { get; set; } = string.Empty;
    public int CategoriaId { get; set; }
    public string CategoriaNombre { get; set; } = string.Empty;
    public int ImpuestoId { get; set; }
    public string ImpuestoNombre { get; set; } = string.Empty;
    public decimal ImpuestoPorcentaje { get; set; }
    public bool Disponible { get; set; }
    public string EstadoStock { get; set; } = string.Empty;
    public TProductoImagen? ImagenPrincipal { get; set; }
    public TDescuentoAplicado Descuento { get; set; } = new();
}

public class TCatalogosProducto
{
    public IEnumerable<TFamiliaProducto> Familias { get; set; } = [];
    public IEnumerable<TCategoria> Categorias { get; set; } = [];
    public IEnumerable<TImpuesto> Impuestos { get; set; } = [];
}

public class TCambioEstadoProducto
{
    public bool Activo { get; set; }
}
