namespace ProyectoEcommerce.Dominio.Entidades;

/// <summary>Representa un rol de seguridad asignado a usuarios y opciones de menú.</summary>
public partial class Rol
{
    public int RolId { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    public virtual ICollection<RolMenuOpcion> RolMenuOpciones { get; set; } = new List<RolMenuOpcion>();
}
