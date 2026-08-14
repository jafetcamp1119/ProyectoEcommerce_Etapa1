using System.ComponentModel.DataAnnotations;

namespace ProyectoEcommerce.Dominio.EntidadesTipadas;

public class TProveedor
{
    public int ProveedorId { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido.")]
    [MaxLength(150)]
    public string? Correo { get; set; }

    [MaxLength(30)]
    public string? Telefono { get; set; }

    [MaxLength(250)]
    public string? Direccion { get; set; }

    [MaxLength(500)]
    public string? UrlImagen { get; set; }

    public bool Activo { get; set; }
    public DateTime FechaRegistro { get; set; }
}

public class TCambioEstadoProveedor
{
    public bool Activo { get; set; }
}

public class TFamiliaOfertaProveedor
{
    public int ProveedorId { get; set; }
    public int FamiliaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? UrlImagen { get; set; }
    public bool Incorporada { get; set; }
}

public class TCategoriaOfertaProveedor
{
    public int ProveedorId { get; set; }
    public int FamiliaId { get; set; }
    public int CategoriaId { get; set; }
    public string FamiliaNombre { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? UrlImagen { get; set; }
    public bool Incorporada { get; set; }
}

public class TProductoOfertaProveedor
{
    public int ProductoProveedorCatalogoId { get; set; }
    public int ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;
    public int FamiliaId { get; set; }
    public string FamiliaNombre { get; set; } = string.Empty;
    public int CategoriaId { get; set; }
    public string CategoriaNombre { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal PrecioCompra { get; set; }
    public int ImpuestoId { get; set; }
    public string ImpuestoNombre { get; set; } = string.Empty;
    public decimal ImpuestoPorcentaje { get; set; }
    public int? ProductoId { get; set; }
    public string? Codigo { get; set; }
    public int Stock { get; set; }
    public bool Activo { get; set; }
    public bool Incorporado => ProductoId.HasValue;
}

public class TIncorporarProductoProveedor
{
    [MaxLength(500)]
    public string? Descripcion { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "El stock mínimo no puede ser negativo.")]
    public int StockMinimo { get; set; } = 5;
}

public class TProductoIncorporadoProveedor
{
    public int ProductoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal PrecioCompra { get; set; }
    public decimal PrecioVenta { get; set; }
    public int Stock { get; set; }
}

public class TCategoriaNuevaProveedor
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una familia válida.")]
    public int FamiliaId { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(80)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? Descripcion { get; set; }
}

public class TCategoriaProveedorResultado
{
    public int CategoriaId { get; set; }
    public int FamiliaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool YaExistia { get; set; }
}

public class TProductoExistenteProveedor
{
    public int ProductoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public int ImpuestoId { get; set; }
    public string ImpuestoNombre { get; set; } = string.Empty;
    public bool Activo { get; set; }
}

public class TAgregarProductoExistenteProveedor
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un producto válido.")]
    public int ProductoId { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99",
        ParseLimitsInInvariantCulture = true,
        ErrorMessage = "El precio de compra debe ser mayor que cero.")]
    public decimal PrecioCompra { get; set; }
}

public class TCrearProductoProveedor
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(120)]
    public string Nombre { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999999999999999.99",
        ParseLimitsInInvariantCulture = true,
        ErrorMessage = "El precio de compra debe ser mayor que cero.")]
    public decimal PrecioCompra { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un impuesto válido.")]
    public int ImpuestoId { get; set; }

    public bool Activo { get; set; } = true;
}

public class TModificarProductoProveedor
{
    [Range(typeof(decimal), "0.01", "9999999999999999.99",
        ParseLimitsInInvariantCulture = true,
        ErrorMessage = "El precio de compra debe ser mayor que cero.")]
    public decimal PrecioCompra { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un impuesto válido.")]
    public int ImpuestoId { get; set; }

    public bool Activo { get; set; }
}
