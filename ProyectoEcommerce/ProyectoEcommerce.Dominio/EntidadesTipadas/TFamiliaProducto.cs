namespace ProyectoEcommerce.Dominio.EntidadesTipadas;

/// <summary>Modelo utilizado para consultar y mantener familias.</summary>
public class TFamiliaProducto
{
    public int FamiliaId { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public string? UrlImagen { get; set; }
    public bool Activo { get; set; }
}
