namespace ProyectoEcommerce.Dominio.EntidadesTipadas;

// datos de categoria que viajan por la API sin mandar toda la entidad de EF
public class TCategoria
{
    public int CategoriaId { get; set; }
    public int FamiliaId { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    // la imagen es opcional y por eso acepta null
    public string? UrlImagen { get; set; }
    public bool Activo { get; set; }
}
