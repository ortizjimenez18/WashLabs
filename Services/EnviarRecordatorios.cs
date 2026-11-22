using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using WashLabs.Data.Entities;
using WashLabs.Data.Repositories;
using WashLabs.Models.DTOs;

namespace WashLabs.Services;

public sealed class RecordatoriosOpciones
{
    public bool Habilitado { get; set; }
    public string Cron { get; set; } = "0 8 * * *";
    public string UrlAplicacion { get; set; } = "";
    public string ServidorSmtp { get; set; } = "smtp.gmail.com";
    public int PuertoSmtp { get; set; } = 587;
    public string Correo { get; set; } = "";
    public string ClaveSmtp { get; set; } = "";
    public string? CarpetaDemostracion { get; set; }
}

public sealed class EnviarRecordatorios(RecordatorioRepository repo, IOptions<RecordatoriosOpciones> opciones,
    ILogger<EnviarRecordatorios> logger)
{
    public async Task EjecutarAsync(CancellationToken ct)
    {
        var config = opciones.Value;
        if (!Uri.TryCreate(config.UrlAplicacion, UriKind.Absolute, out var url) || url.Scheme is not ("http" or "https") ||
            !MailAddress.TryCreate(config.Correo, out var remitente) ||
            (string.IsNullOrWhiteSpace(config.CarpetaDemostracion) && string.IsNullOrWhiteSpace(config.ClaveSmtp)))
            throw new InvalidOperationException("Configura la URL, el remitente y el medio de entrega de recordatorios.");

        var ahora = DisponibilidadReserva.Ahora;
        var fecha = DateOnly.FromDateTime(ahora).AddDays(1);
        var candidatos = await repo.CitasPorNotificarAsync(fecha, ct);
        var enviados = 0;
        foreach (var id in candidatos)
        {
            try
            {
                if (await repo.NotificarAsync(id, fecha, ahora, (cita, token) => EnviarAsync(cita, url, remitente, token), ct))
                    enviados++;
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                logger.LogWarning("Falló el recordatorio de la reserva #{CitaId} ({TipoError}).", id, ex.GetType().Name);
            }
        }
        logger.LogInformation("Recordatorios para {Fecha}: {Enviados} de {Candidatos} procesados.", fecha, enviados, candidatos.Count);
    }

    private async Task EnviarAsync(Cita cita, Uri url, MailAddress remitente, CancellationToken ct)
    {
        var config = opciones.Value;
        var detalle = $"{url.AbsoluteUri.TrimEnd('/')}/Reservas/Detalle/{cita.CitaId}";
        using var mensaje = new MailMessage(remitente, new MailAddress(cita.Cliente.Correo))
        {
            Subject = $"WashLabs · Recordatorio de reserva #{cita.CitaId}",
            Body = $"""
                Hola, {cita.Cliente.NombreCompleto}.

                Tu reserva es mañana, {cita.Fecha:dd/MM/yyyy}, de {cita.HoraInicio:HH:mm} a {cita.HoraFin:HH:mm}.
                Sucursal: {cita.Sucursal.Nombre}
                Servicio: {cita.Servicio.Nombre}

                Ingresa con tu cuenta de WashLabs para ver el detalle y confirmar la cita:
                {detalle}
                """
        };
        using var smtp = new SmtpClient(config.ServidorSmtp, config.PuertoSmtp);
        if (!string.IsNullOrWhiteSpace(config.CarpetaDemostracion))
        {
            smtp.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
            smtp.PickupDirectoryLocation = Directory.CreateDirectory(config.CarpetaDemostracion).FullName;
        }
        else
        {
            smtp.EnableSsl = true;
            smtp.Credentials = new NetworkCredential(remitente.Address, config.ClaveSmtp);
        }
        using var plazo = CancellationTokenSource.CreateLinkedTokenSource(ct);
        plazo.CancelAfter(TimeSpan.FromSeconds(30));
        await smtp.SendMailAsync(mensaje, plazo.Token);
    }
}
