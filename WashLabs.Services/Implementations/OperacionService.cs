using System.ComponentModel.DataAnnotations;
using WashLabs.Data.Entities;
using WashLabs.Data.Interfaces;
using WashLabs.Models.DTOs;
using WashLabs.Services.Interfaces;
namespace WashLabs.Services.Implementations;

public sealed class OperacionService(IOperacionRepository repository) : IOperacionService
{
    private static VehiculoDTO Map(Vehiculo v) => new(v.VehiculoId, v.ClienteId, v.Cliente.NombreCompleto,
        v.Matricula, v.Marca, v.Modelo, v.Anio, v.Color, v.CategoriaTamano);
    public async Task<IReadOnlyList<ClienteOpcionDTO>> ClientesAsync(CancellationToken ct = default) =>
        (await repository.ClientesAsync(ct)).Select(u => new ClienteOpcionDTO(u.UsuarioId, u.NombreCompleto)).ToArray();
    public async Task<IReadOnlyList<VehiculoDTO>> VehiculosAsync(CancellationToken ct = default, int? clienteId = null) =>
        (await repository.VehiculosAsync(ct, clienteId)).Select(Map).ToArray();
    public async Task<RegistroVehiculoResultadoDTO> RegistrarVehiculoAsync(RegistroVehiculoDTO solicitud, CancellationToken ct = default)
    {
        var errores = new List<ValidationResult>();
        if (!Validator.TryValidateObject(solicitud, new ValidationContext(solicitud), errores, true))
            return new(false, errores[0].ErrorMessage);
        var matricula = solicitud.Matricula.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(solicitud.Marca) || string.IsNullOrWhiteSpace(solicitud.Modelo))
            return new(false, "Indica la marca y el modelo.");
        var error = await repository.RegistrarVehiculoAsync(new Vehiculo { ClienteId = solicitud.ClienteId,
            Matricula = matricula, Marca = solicitud.Marca.Trim(), Modelo = solicitud.Modelo.Trim(),
            Anio = solicitud.Anio, Color = solicitud.Color?.Trim(), CategoriaTamano = solicitud.CategoriaTamano, Activo = true }, ct);
        return new(error is null, error);
    }
    public async Task<IReadOnlyList<WashPointsVehiculoDTO>> WashPointsAsync(CancellationToken ct = default, int? clienteId = null)
    {
        var config = await repository.ConfiguracionPuntosAsync(ct)
            ?? throw new InvalidOperationException("No hay configuración activa de WashPoints.");
        var vehiculos = await repository.VehiculosAsync(ct, clienteId);
        return vehiculos.Select(v => new WashPointsVehiculoDTO(Map(v), v.TarjetasWashPoints?.LavadosAcumulados ?? 0,
            config.MetaLavados, config.DescripcionRecompensa, v.TarjetasWashPoints?.RecompensaDisponible ?? false,
            (v.TarjetasWashPoints?.MovimientosWashPoints ?? []).Where(m => m.TipoMovimiento == "ACUMULA" && m.CitaId.HasValue)
                .OrderByDescending(m => m.Fecha).ThenByDescending(m => m.MovimientoWashPointsId)
                .Select(m => new MovimientoLavadoDTO(m.CitaId!.Value, m.Fecha, m.Descripcion, m.Cantidad)).ToArray())).ToArray();
    }
    public async Task<IReadOnlyList<CategoriaAsistenteDTO>> CategoriasAsistenteAsync(CancellationToken ct = default) =>
        (await repository.CategoriasAsistenteAsync(ct)).Select(c => new CategoriaAsistenteDTO(c.CategoriaAsistenteId,
            c.Nombre, c.Descripcion, c.PreguntasAsistente.OrderBy(p => p.Orden).ThenBy(p => p.PreguntaAsistenteId)
                .Select(p => (int?)p.PreguntaAsistenteId).FirstOrDefault())).ToArray();
    public async Task<PreguntaAsistenteDTO?> PreguntaAsync(int id, CancellationToken ct = default)
    {
        var p = id > 0 ? await repository.PreguntaAsync(id, ct) : null;
        return p is null ? null : new(p.PreguntaAsistenteId, p.CategoriaAsistente.Nombre, p.TextoPregunta,
            p.OpcionesAsistentePreguntaAsistente.OrderBy(o => o.Orden).ThenBy(o => o.OpcionAsistenteId)
                .Select(o => new OpcionAsistenteDTO(o.OpcionAsistenteId, o.TextoOpcion, o.RespuestaFinal, o.SiguientePreguntaId)).ToArray());
    }
    public Task<bool> CompletarCitaAsync(long id, int encargadoId, CancellationToken ct = default) =>
        id > 0 && encargadoId > 0 ? repository.CompletarCitaAsync(id, encargadoId, ct) : Task.FromResult(false);

    public Task<bool> CancelarCitaAsync(long id, int usuarioId, CancellationToken ct = default) =>
        id > 0 && usuarioId > 0 ? repository.CancelarCitaAsync(id, usuarioId, ct) : Task.FromResult(false);

    public Task<bool> AsignarVehiculoAsync(long citaId, int vehiculoId, int encargadoId, CancellationToken ct = default) =>
        citaId > 0 && vehiculoId > 0 && encargadoId > 0
            ? repository.AsignarVehiculoAsync(citaId, vehiculoId, encargadoId, ct) : Task.FromResult(false);
}
