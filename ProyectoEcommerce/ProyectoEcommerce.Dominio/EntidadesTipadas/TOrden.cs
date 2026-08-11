namespace ProyectoEcommerce.Dominio.EntidadesTipadas;

/// <summary>Modelo administrativo básico de una orden.</summary>
public class TOrden
{
    public int OrdenId { get; set; }
    public int UsuarioId { get; set; }
    public DateTime FechaOrden { get; set; }
    public string Estado { get; set; } = null!;
    public string TipoOrden { get; set; } = "VENTA";
    public string? DireccionEnvio { get; set; }
    public string Moneda { get; set; } = null!;
    public decimal? Total { get; set; }
}
