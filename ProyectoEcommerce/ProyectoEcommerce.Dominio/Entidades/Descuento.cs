namespace ProyectoEcommerce.Dominio.Entidades;

/// <summary>Representa un descuento porcentual dirigido a una familia, categoría o producto.</summary>
public partial class Descuento
{
    public int DescuentoId { get; set; }
    public string Nombre { get; set; } = null!;
    public string TipoDescuento { get; set; } = null!;
    public int? ProductoId { get; set; }
    public int? CategoriaId { get; set; }
    public int? FamiliaId { get; set; }
    public decimal Porcentaje { get; set; }
    public decimal? MontoFijo { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public bool Activo { get; set; }
    public virtual Producto? Producto { get; set; }
    public virtual Categoria? Categoria { get; set; }
    public virtual FamiliaProducto? Familia { get; set; }
}
