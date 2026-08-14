namespace ProyectoEcommerce.Dominio.Entidades;

public partial class ProveedorFamilia
{
    public int ProveedorFamiliaId { get; set; }
    public int ProveedorId { get; set; }
    public int FamiliaId { get; set; }
    public bool Activo { get; set; }
    public virtual Proveedor Proveedor { get; set; } = null!;
    public virtual FamiliaProducto Familia { get; set; } = null!;
}
