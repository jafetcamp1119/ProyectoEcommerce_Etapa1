namespace ProyectoEcommerce.Dominio.Entidades;

public class RolMenuOpcion
{
    public int RolId { get; set; }
    public int MenuOpcionId { get; set; }
    public virtual Rol Rol { get; set; } = null!;
    public virtual MenuOpcion MenuOpcion { get; set; } = null!;
}
