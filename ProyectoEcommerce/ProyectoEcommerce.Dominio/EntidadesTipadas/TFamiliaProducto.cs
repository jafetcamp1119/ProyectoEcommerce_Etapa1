namespace ProyectoEcommerce.Dominio.EntidadesTipadas;

// datos de familia que viajan entre controller, LN y Angular
public class TFamiliaProducto
{
    public int FamiliaId { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    // puede venir null y en ese caso Angular muestra el placeholder
    public string? UrlImagen { get; set; }
    public bool Activo { get; set; }
}
