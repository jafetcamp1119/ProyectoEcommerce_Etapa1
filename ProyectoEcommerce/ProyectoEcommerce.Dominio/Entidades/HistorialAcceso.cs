namespace ProyectoEcommerce.Dominio.Entidades;

/// <summary>Registra intentos de acceso exitosos y fallidos para trazabilidad.</summary>
public partial class HistorialAcceso
{
    public long HistorialAccesoId { get; set; }
    public int? UsuarioId { get; set; }
    public string CorreoIntentado { get; set; } = null!;
    public DateTime Fecha { get; set; }
    public bool Exitoso { get; set; }
    public virtual Usuario? Usuario { get; set; }
}
