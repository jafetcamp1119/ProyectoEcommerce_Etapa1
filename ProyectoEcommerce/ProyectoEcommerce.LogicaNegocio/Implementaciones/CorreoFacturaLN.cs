using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using ProyectoEcommerce.Dominio.EntidadesTipadas;
using ProyectoEcommerce.Dominio.InterfazLN;

namespace ProyectoEcommerce.LogicaNegocio.Implementaciones;

/// <summary>
/// Envía por SMTP la factura PDF generada después de una compra confirmada.
/// </summary>
public class CorreoFacturaLN : ICorreoFacturaLN
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<CorreoFacturaLN> _logger;

    public CorreoFacturaLN(IConfiguration configuration, ILogger<CorreoFacturaLN> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>Construye el correo de LessPrice, adjunta el PDF y realiza el envío configurado.</summary>
    public async Task<TResultadoCorreoFactura> EnviarAsync(
        TFacturaDatos factura,
        string rutaPdf,
        CancellationToken cancellationToken = default)
    {
        // Host identifica el servidor; Port el puerto; FromEmail y FromName forman el remitente.
        // Enabled permite desactivar el envío cuando SMTP no está preparado en el entorno.
        var seccion = _configuration.GetSection("Smtp");
        var habilitado = seccion.GetValue<bool>("Enabled");
        var servidor = seccion["Host"]?.Trim();
        var remitente = (seccion["FromEmail"] ?? seccion["SenderEmail"])?.Trim();
        if (!habilitado || string.IsNullOrWhiteSpace(servidor) || string.IsNullOrWhiteSpace(remitente))
            return new TResultadoCorreoFactura { Error = "SMTP no configurado." };

        try
        {
            var mensaje = new MimeMessage();
            var nombreRemitente = (seccion["FromName"] ?? seccion["SenderName"])?.Trim();
            mensaje.From.Add(new MailboxAddress(
                string.IsNullOrWhiteSpace(nombreRemitente) ? "LessPrice" : nombreRemitente,
                remitente));
            mensaje.To.Add(MailboxAddress.Parse(factura.Correo));
            mensaje.Subject = $"Factura de tu compra en LessPrice - Orden #{factura.NumeroOrden}";

            var cuerpo = new BodyBuilder
            {
                TextBody = $"Hola {factura.Cliente}:\n\nGracias por comprar en LessPrice.\n\nAdjuntamos la factura correspondiente a tu orden #{factura.NumeroOrden}.\n\nTotal: CRC {factura.Total:N2}\n\nGracias por tu compra.\n\nLessPrice"
            };
            cuerpo.Attachments.Add(rutaPdf, new ContentType("application", "pdf"));
            mensaje.Body = cuerpo.ToMessageBody();

            using var cliente = new SmtpClient();
            var puerto = seccion.GetValue<int?>("Port") ?? 587;
            var habilitarSsl = seccion.GetValue<bool?>("EnableSsl")
                ?? seccion.GetValue<bool?>("UseStartTls")
                ?? true;
            var seguridad = seccion.GetValue<bool>("UseSsl")
                ? SecureSocketOptions.SslOnConnect
                : habilitarSsl
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.None;

            await cliente.ConnectAsync(servidor, puerto, seguridad, cancellationToken);
            // Password llega mediante IConfiguration desde User Secrets, una variable de entorno
            // u otro proveedor seguro; no se almacena ni se muestra en el código.
            var usuario = seccion["Username"]?.Trim();
            var contrasena = seccion["Password"];
            if (!string.IsNullOrWhiteSpace(usuario))
                await cliente.AuthenticateAsync(usuario, contrasena ?? string.Empty, cancellationToken);

            await cliente.SendAsync(mensaje, cancellationToken);
            await cliente.DisconnectAsync(true, cancellationToken);
            return new TResultadoCorreoFactura { Enviado = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No fue posible enviar la factura de OrdenId {OrdenId} por SMTP.", factura.OrdenId);
            return new TResultadoCorreoFactura { Error = "No fue posible enviar la factura por correo." };
        }
    }
}
