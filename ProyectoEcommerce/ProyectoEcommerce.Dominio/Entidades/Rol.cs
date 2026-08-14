namespace ProyectoEcommerce.Dominio.Entidades;

public partial class Rol
{
    public int RolId { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    public virtual ICollection<RolMenuOpcion> RolMenuOpciones { get; set; } = new List<RolMenuOpcion>();
}
