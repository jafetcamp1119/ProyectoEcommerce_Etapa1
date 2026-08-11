namespace ProyectoEcommerce.Dominio.Entidades;

/// <summary>Representa el carrito abierto o convertido perteneciente a un usuario.</summary>
public partial class Carrito
{
    public int CarritoId { get; set; }
    public int UsuarioId { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string Estado { get; set; } = null!;
    public virtual Usuario Usuario { get; set; } = null!;
    public virtual ICollection<CarritoDetalle> Detalles { get; set; } = new List<CarritoDetalle>();
}
