namespace ProyectoEcommerce.Dominio.EntidadesTipadas;

/// <summary>Modelo de una imagen asociada a un producto.</summary>
public class TProductoImagen
{
    public int ImagenId { get; set; }
    public int ProductoId { get; set; }
    public string UrlImagen { get; set; } = null!;
    public string? TextoAlternativo { get; set; }
    public bool EsPrincipal { get; set; }
    public int Orden { get; set; }
    public bool Activo { get; set; }
}
