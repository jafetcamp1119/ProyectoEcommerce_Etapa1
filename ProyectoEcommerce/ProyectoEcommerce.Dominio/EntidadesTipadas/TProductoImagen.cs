namespace ProyectoEcommerce.Dominio.EntidadesTipadas;

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
