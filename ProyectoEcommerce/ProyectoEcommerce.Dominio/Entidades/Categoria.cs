namespace ProyectoEcommerce.Dominio.Entidades;

// entidad Database First de dbo.Categorias y su relacion con FamiliaProducto
public partial class Categoria
{
    public int CategoriaId { get; set; }
    public int FamiliaId { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    // columna opcional usada por las tarjetas de categorias
    public string? UrlImagen { get; set; }
    public bool Activo { get; set; }
    public virtual FamiliaProducto Familia { get; set; } = null!;
    public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();
    public virtual ICollection<Descuento> Descuentos { get; set; } = new List<Descuento>();
}
