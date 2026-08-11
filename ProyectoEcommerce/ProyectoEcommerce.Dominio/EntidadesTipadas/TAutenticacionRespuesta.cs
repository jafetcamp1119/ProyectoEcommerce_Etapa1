namespace ProyectoEcommerce.Dominio.EntidadesTipadas;

/// <summary>Respuesta que entrega JWT, vencimiento y usuario autenticado.</summary>
public class TAutenticacionRespuesta
{
    public string Token { get; set; } = null!;
    public DateTime Expira { get; set; }
    public TUsuario Usuario { get; set; } = null!;
}
