using Cronos;
using Microsoft.Extensions.Options;

namespace WashLabs.Services;

public sealed class ProgramadorRecordatorios(IServiceScopeFactory scopes, IOptions<RecordatoriosOpciones> opciones,
    ILogger<ProgramadorRecordatorios> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var config = opciones.Value;
        if (!config.Habilitado) return;

        var cron = CronExpression.Parse(config.Cron);
        var zona = TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
        while (!ct.IsCancellationRequested)
        {
            var ahora = DateTimeOffset.UtcNow;
            var siguiente = cron.GetNextOccurrence(ahora, zona);
            if (siguiente is null) return;
            logger.LogInformation("Siguiente ejecución de recordatorios: {Fecha}", TimeZoneInfo.ConvertTime(siguiente.Value, zona));
            await Task.Delay(siguiente.Value - ahora, ct);
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<EnviarRecordatorios>().EjecutarAsync(ct);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                logger.LogError(ex, "Falló el ciclo de recordatorios.");
            }
        }
    }
}
