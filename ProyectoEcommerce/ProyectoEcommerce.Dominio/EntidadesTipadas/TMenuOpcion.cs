namespace ProyectoEcommerce.Dominio.EntidadesTipadas;

public class TMenuOpcion
{
    public int MenuOpcionId { get; set; }
    public string Nombre { get; set; } = null!;
    public string Ruta { get; set; } = null!;
    public string? Icono { get; set; }
    public int Orden { get; set; }
}
