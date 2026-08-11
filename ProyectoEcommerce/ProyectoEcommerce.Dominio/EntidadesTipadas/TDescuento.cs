using System.ComponentModel.DataAnnotations;

namespace ProyectoEcommerce.Dominio.EntidadesTipadas;

/// <summary>Datos administrativos de un descuento porcentual.</summary>
public class TDescuento
{
    public int DescuentoId { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(150, ErrorMessage = "El nombre no puede superar 150 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tipo de descuento es obligatorio.")]
    [MaxLength(20)]
    public string TipoDescuento { get; set; } = string.Empty;

    public int? ProductoId { get; set; }
    public int? CategoriaId { get; set; }
    public int? FamiliaId { get; set; }

    [Range(typeof(decimal), "0.01", "100", ParseLimitsInInvariantCulture = true,
        ErrorMessage = "El porcentaje debe ser mayor que 0 y menor o igual que 100.")]
    public decimal Porcentaje { get; set; }

    [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
    public DateTime FechaInicio { get; set; }

    [Required(ErrorMessage = "La fecha de fin es obligatoria.")]
    public DateTime FechaFin { get; set; }

    public bool Activo { get; set; } = true;
    public string Destino { get; set; } = string.Empty;
    public string VigenciaActual { get; set; } = string.Empty;
}

/// <summary>Precio efectivo ya resuelto por la lógica de negocio.</summary>
public class TDescuentoAplicado
{
    public int ProductoId { get; set; }
    public bool TieneDescuento { get; set; }
    public int? DescuentoId { get; set; }
    public string? TipoDescuento { get; set; }
    public string? Nombre { get; set; }
    public decimal Porcentaje { get; set; }
    public decimal PrecioOriginal { get; set; }
    public decimal MontoDescuento { get; set; }
    public decimal PrecioFinal { get; set; }
}

/// <summary>Listas activas utilizadas por el formulario administrativo.</summary>
public class TCatalogosDescuento
{
    public IEnumerable<TFamiliaProducto> Familias { get; set; } = [];
    public IEnumerable<TCategoria> Categorias { get; set; } = [];
    public IEnumerable<TProductoSelector> Productos { get; set; } = [];
}

public class TProductoSelector
{
    public int ProductoId { get; set; }
    public int CategoriaId { get; set; }
    public int FamiliaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string CategoriaNombre { get; set; } = string.Empty;
    public string FamiliaNombre { get; set; } = string.Empty;
}

public class TCambioEstadoDescuento
{
    public bool Activo { get; set; }
}

/// <summary>Candidato mínimo usado por el selector central y por sus pruebas.</summary>
public class TDescuentoCandidato
{
    public int DescuentoId { get; set; }
    public string TipoDescuento { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal Porcentaje { get; set; }
}
