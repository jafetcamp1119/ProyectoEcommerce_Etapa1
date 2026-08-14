namespace ProyectoEcommerce.Dominio.Entidades;

// entidad Database First que corresponde a dbo.FamiliasProducto
public partial class FamiliaProducto
{
    public int FamiliaId { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    // columna opcional de hasta 500 caracteres configurada en el contexto
    public string? UrlImagen { get; set; }
    public bool Activo { get; set; }
    public virtual ICollection<Categoria> Categorias { get; set; } = new List<Categoria>();
    public virtual ICollection<Descuento> Descuentos { get; set; } = new List<Descuento>();
    public virtual ICollection<ProveedorFamilia> Proveedores { get; set; } = new List<ProveedorFamilia>();
}
