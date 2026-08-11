namespace ProyectoEcommerce.Dominio.EntidadesTipadas;

/// <summary>Modelo utilizado para consultar y mantener categorías.</summary>
public class TCategoria
{
    public int CategoriaId { get; set; }
    public int FamiliaId { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
}
