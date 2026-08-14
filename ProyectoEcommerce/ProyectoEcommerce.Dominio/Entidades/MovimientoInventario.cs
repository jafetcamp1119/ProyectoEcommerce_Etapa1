namespace ProyectoEcommerce.Dominio.Entidades;

public partial class MovimientoInventario
{
    public long MovimientoInventarioId { get; set; }
    public int ProductoId { get; set; }
    public string Tipo { get; set; } = null!;
    public int Cantidad { get; set; }
    public string? Motivo { get; set; }
    public DateTime Fecha { get; set; }
    public int? UsuarioId { get; set; }
    public int? CompraProveedorId { get; set; }
    public int? StockAnterior { get; set; }
    public int? StockNuevo { get; set; }
    public virtual Producto Producto { get; set; } = null!;
    public virtual Usuario? Usuario { get; set; }
    public virtual CompraProveedor? CompraProveedor { get; set; }
}
