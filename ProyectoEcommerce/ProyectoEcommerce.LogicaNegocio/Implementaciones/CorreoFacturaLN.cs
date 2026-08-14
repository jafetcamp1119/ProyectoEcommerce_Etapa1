using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Dominio.InterfazLN;

namespace ProyectoEcommerce.LogicaNegocio.Implementaciones;

// manda por SMTP el PDF que se genero despues de confirmar una compra
public class CorreoFacturaLN : ICorreoFacturaLN
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<CorreoFacturaLN> _logger;

    public CorreoFacturaLN(IConfiguration configuration, ILogger<CorreoFacturaLN> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    // recibe los datos de factura y la ruta del PDF
    // arma el mensaje, conecta al servidor configurado y devuelve si el envio salio bien
    public async Task<TResultadoCorreoFactura> EnviarAsync(
        TFacturaDatos factura,
        string rutaPdf,
        CancellationToken cancellationToken = default)
    {
        var asunto = $"Factura de tu compra en LessPrice - Orden #{factura.NumeroOrden}";
        var texto = $"Hola {factura.Cliente}:\n\nGracias por comprar en LessPrice.\n\n" +
            $"Adjuntamos la factura correspondiente a tu orden #{factura.NumeroOrden}.\n\n" +
            $"Total: CRC {factura.Total:N2}\n\nGracias por tu compra.\n\nLessPrice";
        return await EnviarMensajeAsync(factura.Correo, asunto, texto, rutaPdf, cancellationToken);
    }

    public async Task<TResultadoCorreoFactura> EnviarCompraProveedorAsync(
        TDocumentoCompraProveedor documento,
        string rutaPdf,
        bool esProforma,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(documento.CorreoProveedor))
            return new TResultadoCorreoFactura { Error = "El proveedor no tiene un correo registrado." };

        var tipo = esProforma ? "Proforma" : "Comprobante";
        var asunto = $"{tipo} de compra LessPrice - {documento.Numero}";
        var texto = $"Hola {documento.Proveedor}:\n\n" +
            $"Adjuntamos la {tipo.ToLowerInvariant()} de compra {documento.Numero}.\n\n" +
            $"Total: CRC {documento.Total:N2}\n\nLessPrice";
        return await EnviarMensajeAsync(
            documento.CorreoProveedor,
            asunto,
            texto,
            rutaPdf,
            cancellationToken);
    }

    // toda salida usa la misma sección SMTP y las mismas credenciales ya configuradas
    private async Task<TResultadoCorreoFactura> EnviarMensajeAsync(
        string destino,
        string asunto,
        string texto,
        string rutaPdf,
        CancellationToken cancellationToken)
    {
        var seccion = _configuration.GetSection("Smtp");
        var servidor = seccion["Host"]?.Trim();
        var remitente = (seccion["FromEmail"] ?? seccion["SenderEmail"])?.Trim();
        if (!seccion.GetValue<bool>("Enabled") || string.IsNullOrWhiteSpace(servidor) ||
            string.IsNullOrWhiteSpace(remitente))
            return new TResultadoCorreoFactura { Error = "SMTP no configurado." };

        try
        {
            var mensaje = new MimeMessage();
            var nombreRemitente = (seccion["FromName"] ?? seccion["SenderName"])?.Trim();
            mensaje.From.Add(new MailboxAddress(
                string.IsNullOrWhiteSpace(nombreRemitente) ? "LessPrice" : nombreRemitente,
                remitente));
            mensaje.To.Add(MailboxAddress.Parse(destino));
            mensaje.Subject = asunto;
            var cuerpo = new BodyBuilder { TextBody = texto };
            cuerpo.Attachments.Add(rutaPdf, new ContentType("application", "pdf"));
            mensaje.Body = cuerpo.ToMessageBody();

            using var cliente = new SmtpClient();
            var puerto = seccion.GetValue<int?>("Port") ?? 587;
            var habilitarSsl = seccion.GetValue<bool?>("EnableSsl")
                ?? seccion.GetValue<bool?>("UseStartTls")
                ?? true;
            var seguridad = seccion.GetValue<bool>("UseSsl")
                ? SecureSocketOptions.SslOnConnect
                : habilitarSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
            await cliente.ConnectAsync(servidor, puerto, seguridad, cancellationToken);

            var usuario = seccion["Username"]?.Trim();
            if (!string.IsNullOrWhiteSpace(usuario))
                await cliente.AuthenticateAsync(usuario, seccion["Password"] ?? string.Empty, cancellationToken);

            await cliente.SendAsync(mensaje, cancellationToken);
            await cliente.DisconnectAsync(true, cancellationToken);
            return new TResultadoCorreoFactura { Enviado = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No fue posible enviar un documento PDF de LessPrice por SMTP.");
            return new TResultadoCorreoFactura { Error = "No fue posible enviar el documento por correo." };
        }
    }
}
