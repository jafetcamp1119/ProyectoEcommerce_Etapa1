using ProyectoEcommerce.Dominio.EntidadesTipadas;

namespace ProyectoEcommerce.Dominio.InterfazLN;

/// <summary>Define el envío SMTP de una factura PDF al correo confirmado por el Cliente.</summary>
public interface ICorreoFacturaLN
{
    /// <summary>Construye el mensaje de LessPrice y adjunta el archivo PDF indicado.</summary>
    Task<TResultadoCorreoFactura> EnviarAsync(
        TFacturaDatos factura,
        string rutaPdf,
        CancellationToken cancellationToken = default);
}
