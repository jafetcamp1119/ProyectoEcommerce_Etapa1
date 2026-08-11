namespace ProyectoEcommerce.Dominio.Entidades;

/// <summary>Representa la relación entre un rol y una opción autorizada del menú.</summary>
public class RolMenuOpcion
{
    public int RolId { get; set; }
    public int MenuOpcionId { get; set; }
    public virtual Rol Rol { get; set; } = null!;
    public virtual MenuOpcion MenuOpcion { get; set; } = null!;
}
