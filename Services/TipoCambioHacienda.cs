using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using WashLabs.Models.DTOs;

namespace WashLabs.Services;

public sealed class TipoCambioHacienda(IHttpClientFactory clientes, IMemoryCache cache, ILogger<TipoCambioHacienda> logger,
    IDataProtectionProvider proteccion)
{
    public string Proteger(TipoCambioVentaDTO? tasa, int usuarioId, string documento) =>
        Protector(usuarioId, documento).Protect(JsonSerializer.Serialize(tasa), TimeSpan.FromHours(2));

    public Task<TipoCambioVentaDTO?> ParaEmitirAsync(string? cotizacion, int usuarioId, string documento, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(cotizacion))
        {
            try
            {
                return Task.FromResult(JsonSerializer.Deserialize<TipoCambioVentaDTO?>(
                    Protector(usuarioId, documento).Unprotect(cotizacion)));
            }
            catch (CryptographicException)
            {
                // Una cotización vencida o inválida se reemplaza por una consulta del servidor.
            }
        }
        return VentaAsync(ct);
    }

    private ITimeLimitedDataProtector Protector(int usuarioId, string documento) =>
        proteccion.CreateProtector("Hacienda.Venta", usuarioId.ToString(CultureInfo.InvariantCulture), documento).ToTimeLimitedDataProtector();

    public Task<TipoCambioVentaDTO?> VentaAsync(CancellationToken ct) =>
        cache.GetOrCreateAsync<TipoCambioVentaDTO?>("Hacienda:USD:venta", async entrada =>
        {
            TipoCambioVentaDTO? tasa = null;
            try
            {
                using var cliente = clientes.CreateClient(nameof(TipoCambioHacienda));
                using var respuesta = await cliente.GetAsync("indicadores/tc/dolar", ct);
                respuesta.EnsureSuccessStatusCode();
                using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync(ct));
                tasa = LeerVenta(json.RootElement);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested &&
                ex is HttpRequestException or OperationCanceledException or JsonException)
            {
                logger.LogWarning(ex, "No se pudo consultar el tipo de cambio de Hacienda.");
            }
            entrada.AbsoluteExpirationRelativeToNow = tasa is null ? TimeSpan.FromSeconds(30) : TimeSpan.FromMinutes(15);
            return tasa;
        });

    public static TipoCambioVentaDTO? LeerVenta(JsonElement respuesta)
    {
        if (respuesta.ValueKind != JsonValueKind.Object ||
            !respuesta.TryGetProperty("venta", out var venta) || venta.ValueKind != JsonValueKind.Object ||
            !venta.TryGetProperty("valor", out var valor) || valor.ValueKind != JsonValueKind.Number ||
            !valor.TryGetDecimal(out var tasa) || tasa <= 0 || tasa > 999999999999m || decimal.Round(tasa, 6) != tasa ||
            !venta.TryGetProperty("fecha", out var fecha) || fecha.ValueKind != JsonValueKind.String ||
            !DateOnly.TryParseExact(fecha.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dia))
            return null;
        return new(tasa, dia);
    }
}
