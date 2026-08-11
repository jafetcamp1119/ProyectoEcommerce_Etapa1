namespace ProyectoEcommerce.Dominio.Entidades;

/// <summary>Representa el nivel superior que agrupa categorías del catálogo.</summary>
public partial class FamiliaProducto
{
    public int FamiliaId { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
    public virtual ICollection<Categoria> Categorias { get; set; } = new List<Categoria>();
    public virtual ICollection<Descuento> Descuentos { get; set; } = new List<Descuento>();
}
