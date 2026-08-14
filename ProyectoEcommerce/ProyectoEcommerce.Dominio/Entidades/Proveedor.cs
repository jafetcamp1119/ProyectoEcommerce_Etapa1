namespace ProyectoEcommerce.Dominio.Entidades;

// entidad Database First de los datos de contacto y estado de un proveedor
public partial class Proveedor
{
    public int ProveedorId { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Correo { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public string? UrlImagen { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaRegistro { get; set; }
    public virtual ICollection<ProveedorFamilia> Familias { get; set; } = new List<ProveedorFamilia>();
    public virtual ICollection<ProveedorCategoria> Categorias { get; set; } = new List<ProveedorCategoria>();
    public virtual ICollection<ProductoProveedor> Productos { get; set; } = new List<ProductoProveedor>();
    public virtual ICollection<CompraProveedor> Compras { get; set; } = new List<CompraProveedor>();
}
