using ProyectoEcommerce.Dominio.EntidadesTipadas;

namespace ProyectoEcommerce.Dominio.InterfazLN;

// contrato para mandar por SMTP la factura que se genero despues de la compra
public interface ICorreoFacturaLN
{
    // recibe los datos de la factura y la ruta del PDF, luego devuelve si el correo salio o fallo
    Task<TResultadoCorreoFactura> EnviarAsync(
        TFacturaDatos factura,
        string rutaPdf,
        CancellationToken cancellationToken = default);
}
