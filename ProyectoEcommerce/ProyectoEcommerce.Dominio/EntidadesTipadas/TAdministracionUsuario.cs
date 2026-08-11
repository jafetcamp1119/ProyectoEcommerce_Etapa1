using System.ComponentModel.DataAnnotations;

namespace ProyectoEcommerce.Dominio.EntidadesTipadas;

/// <summary>Filtros de búsqueda y paginación para administrar usuarios.</summary>
public class TFiltroUsuarios
{
    public string? Texto { get; set; }
    public int? RolId { get; set; }
    public bool? Activo { get; set; }
    [Range(1, int.MaxValue)] public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 25;
}

/// <summary>Respuesta paginada reutilizada por catálogos y administración.</summary>
public class TPagina<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
    public int Total { get; set; }
}

public class TCambioRolUsuario
{
    [Range(1, int.MaxValue)] public int UsuarioId { get; set; }
    [Range(1, int.MaxValue)] public int RolId { get; set; }
}

public class TCambioEstadoUsuario
{
    [Range(1, int.MaxValue)] public int UsuarioId { get; set; }
    public bool Activo { get; set; }
}

/// <summary>Resultado interno de autenticación, incluido un posible bloqueo temporal.</summary>
public class TEstadoAutenticacion
{
    public TUsuario? Usuario { get; set; }
    public bool Bloqueado { get; set; }
    public DateTime? BloqueadoHasta { get; set; }
    public int? SegundosRestantes { get; set; }
}
