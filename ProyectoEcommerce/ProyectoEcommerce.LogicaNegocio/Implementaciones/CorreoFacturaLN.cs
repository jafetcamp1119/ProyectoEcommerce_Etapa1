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
        // Host es el servidor, Port el puerto y FromEmail y FromName forman el remitente
        // Enabled deja apagar correos cuando SMTP no esta preparado en el entorno
        var seccion = _configuration.GetSection("Smtp");
        var habilitado = seccion.GetValue<bool>("Enabled");
        var servidor = seccion["Host"]?.Trim();
        var remitente = (seccion["FromEmail"] ?? seccion["SenderEmail"])?.Trim();
        if (!habilitado || string.IsNullOrWhiteSpace(servidor) || string.IsNullOrWhiteSpace(remitente))
            return new TResultadoCorreoFactura { Error = "SMTP no configurado." };

        try
        {
            // MimeMessage guarda remitente, destino, asunto y contenido del correo
            var mensaje = new MimeMessage();
            var nombreRemitente = (seccion["FromName"] ?? seccion["SenderName"])?.Trim();
            mensaje.From.Add(new MailboxAddress(
                string.IsNullOrWhiteSpace(nombreRemitente) ? "LessPrice" : nombreRemitente,
                remitente));
            mensaje.To.Add(MailboxAddress.Parse(factura.Correo));
            mensaje.Subject = $"Factura de tu compra en LessPrice - Orden #{factura.NumeroOrden}";

            // BodyBuilder arma el texto y permite adjuntar el PDF desde su ruta
            var cuerpo = new BodyBuilder
            {
                TextBody =
                $"Hola {factura.Cliente}:\n\nGracias por comprar en LessPrice.\n\nAdjuntamos la factura correspondiente a tu orden #{
                factura.NumeroOrden}.\n\nTotal: CRC {factura.Total:N2}\n\nGracias por tu compra.\n\nLessPrice"
            };
            cuerpo.Attachments.Add(rutaPdf, new ContentType("application", "pdf"));
            mensaje.Body = cuerpo.ToMessageBody();

            using var cliente = new SmtpClient();
            var puerto = seccion.GetValue<int?>("Port") ?? 587;
            var habilitarSsl = seccion.GetValue<bool?>("EnableSsl")
                ?? seccion.GetValue<bool?>("UseStartTls")
                ?? true;
            // escoge SSL directo, StartTls o sin cifrado segun la configuracion
            var seguridad = seccion.GetValue<bool>("UseSsl")
                ? SecureSocketOptions.SslOnConnect
                : habilitarSsl
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.None;

            // abre la conexion SMTP usando la seguridad escogida
            await cliente.ConnectAsync(servidor, puerto, seguridad, cancellationToken);

            // Password llega desde User Secrets, variable de entorno u otro proveedor configurado
            // no se guarda ni se muestra dentro del codigo
            var usuario = seccion["Username"]?.Trim();
            var contrasena = seccion["Password"];
            if (!string.IsNullOrWhiteSpace(usuario))
                await cliente.AuthenticateAsync(usuario, contrasena ?? string.Empty, cancellationToken);

            // manda el mensaje completo y cierra la conexion de forma ordenada
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
