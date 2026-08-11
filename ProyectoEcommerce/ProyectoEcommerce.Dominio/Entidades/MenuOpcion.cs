namespace ProyectoEcommerce.Dominio.Entidades;

/// <summary>Representa una opción de navegación que puede asignarse a un rol.</summary>
public class MenuOpcion
{
    public int MenuOpcionId { get; set; }
    public string Nombre { get; set; } = null!;
    public string Ruta { get; set; } = null!;
    public string? Icono { get; set; }
    public int Orden { get; set; }
    public bool Activo { get; set; }
    public virtual ICollection<RolMenuOpcion> RolMenuOpciones { get; set; } = new List<RolMenuOpcion>();
}
