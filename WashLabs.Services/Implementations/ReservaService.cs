using WashLabs.Data.Interfaces;
using WashLabs.Models.DTOs;
using WashLabs.Services.Interfaces;
using WashLabs.Services.Mappers;
using System.ComponentModel.DataAnnotations;

namespace WashLabs.Services.Implementations;

public sealed class ReservaService(IReservaRepository repository) : IReservaService
{
    public async Task<ReservasHistorialDTO> ListarClienteAsync(int clienteId, CancellationToken ct = default) =>
        new("", "", (await repository.ListarClienteAsync(clienteId, ct)).Select(ReservaMapper.ToResumenDTO).ToArray(), true);
    public async Task<ReservaDTO?> ObtenerClienteAsync(long id, int clienteId, CancellationToken ct = default)
    {
        var cita = await repository.ObtenerClienteAsync(id, clienteId, ct);
        return cita is null ? null : ReservaMapper.ToDTO(cita);
    }
    public Task<FormularioReservaDTO?> FormularioAsync(int encargadoId, CancellationToken ct = default) => repository.FormularioAsync(encargadoId, ct);

    public async Task<IReadOnlyList<TimeOnly>> HorariosAsync(int encargadoId, int servicioId, DateOnly fecha, CancellationToken ct = default) =>
        DisponibilidadReserva.Calcular(await repository.AgendaAsync(encargadoId, servicioId, fecha, ct), fecha);

    public async Task<ResultadoCrearReservaDTO> CrearAsync(CrearReservaDTO solicitud, int encargadoId, CancellationToken ct = default, int? creadorId = null)
    {
        var errores = new List<ValidationResult>();
        if (!Validator.TryValidateObject(solicitud, new ValidationContext(solicitud), errores, true))
            return new(false, null, errores[0].ErrorMessage);
        foreach (var respuesta in solicitud.Respuestas)
            if (!Validator.TryValidateObject(respuesta, new ValidationContext(respuesta), errores, true))
                return new(false, null, errores[^1].ErrorMessage);
        if (solicitud.Fecha!.Value.ToDateTime(solicitud.HoraInicio!.Value) <= DisponibilidadReserva.Ahora)
            return new(false, null, "Selecciona una fecha y hora futuras.");
        return await repository.CrearAsync(solicitud, encargadoId, creadorId ?? encargadoId, ct);
    }
    public async Task<ReservasHistorialDTO?> ListarAsync(int encargadoId, CancellationToken cancellationToken = default)
    {
        var encargado = await repository.ObtenerEncargadoAsync(encargadoId, cancellationToken);
        if (encargado?.Sucursal is null) return null;
        var reservas = await repository.ListarAsync(encargadoId, cancellationToken);
        return new(encargado.NombreCompleto, encargado.Sucursal.Nombre,
            reservas.Select(ReservaMapper.ToResumenDTO).ToArray());
    }

    public async Task<ReservaDTO?> ObtenerAsync(long id, int encargadoId, CancellationToken cancellationToken = default)
    {
        if (id <= 0 || encargadoId <= 0) return null;
        var reserva = await repository.ObtenerAsync(id, encargadoId, cancellationToken);
        return reserva is null ? null : ReservaMapper.ToDTO(reserva);
    }
}
