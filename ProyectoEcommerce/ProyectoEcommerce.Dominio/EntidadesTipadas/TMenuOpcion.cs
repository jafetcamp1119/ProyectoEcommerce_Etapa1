namespace ProyectoEcommerce.Dominio.EntidadesTipadas;

/// <summary>Opción de navegación autorizada para la sesión del usuario.</summary>
public class TMenuOpcion
{
    public int MenuOpcionId { get; set; }
    public string Nombre { get; set; } = null!;
    public string Ruta { get; set; } = null!;
    public string? Icono { get; set; }
    public int Orden { get; set; }
}
