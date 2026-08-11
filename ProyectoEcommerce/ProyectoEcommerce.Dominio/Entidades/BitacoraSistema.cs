namespace ProyectoEcommerce.Dominio.Entidades;

/// <summary>Registra acciones relevantes realizadas sobre entidades del sistema.</summary>
public class BitacoraSistema
{
    public long BitacoraId { get; set; }
    public int? UsuarioId { get; set; }
    public DateTime Fecha { get; set; }
    public string Accion { get; set; } = null!;
    public string? Entidad { get; set; }
    public string? EntidadId { get; set; }
    public string? Detalle { get; set; }
    public virtual Usuario? Usuario { get; set; }
}
