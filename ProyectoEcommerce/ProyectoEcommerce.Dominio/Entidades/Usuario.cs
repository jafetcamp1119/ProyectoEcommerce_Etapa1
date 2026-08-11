namespace ProyectoEcommerce.Dominio.Entidades;

/// <summary>Representa una cuenta, su rol, credenciales protegidas y estado de acceso.</summary>
public partial class Usuario
{
    public int UsuarioId { get; set; }
    public int RolId { get; set; }
    public string Nombre { get; set; } = null!;
    public string Apellidos { get; set; } = null!;
    public string Correo { get; set; } = null!;
    public string Telefono { get; set; } = null!;
    public string? PasswordHash { get; set; }
    public string? Direccion { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaRegistro { get; set; }
    public int IntentosFallidos { get; set; }
    public DateTime? BloqueadoHasta { get; set; }
    public DateTime? UltimoIntentoFallido { get; set; }
    public virtual Rol Rol { get; set; } = null!;
    public virtual ICollection<HistorialAcceso> HistorialAccesos { get; set; } = new List<HistorialAcceso>();
    public virtual ICollection<Orden> Ordenes { get; set; } = new List<Orden>();
    public virtual ICollection<Carrito> Carritos { get; set; } = new List<Carrito>();
}
