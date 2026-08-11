namespace ProyectoEcommerce.Dominio.Entidades;

/// <summary>Representa una imagen asociada a un producto y su prioridad visual.</summary>
public partial class ProductoImagen
{
    public int ImagenId { get; set; }
    public int ProductoId { get; set; }
    public string UrlImagen { get; set; } = null!;
    public string? TextoAlternativo { get; set; }
    public bool EsPrincipal { get; set; }
    public int Orden { get; set; }
    public bool Activo { get; set; }
    public virtual Producto Producto { get; set; } = null!;
}
