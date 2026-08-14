namespace ProyectoEcommerce.Dominio.EntidadesTipadas;

public class TUsuario
{
    public int UsuarioId { get; set; }
    public int RolId { get; set; }
    public string RolNombre { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string Apellidos { get; set; } = null!;
    public string Correo { get; set; } = null!;
    public string Telefono { get; set; } = null!;
    public string? Direccion { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaRegistro { get; set; }
    public IEnumerable<TMenuOpcion> MenuOpciones { get; set; } = [];
}
