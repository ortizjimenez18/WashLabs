using WashLabs.Data.Entities;
using WashLabs.Models.DTOs;

namespace WashLabs.Data.Interfaces;

public interface IReservaRepository
{
    Task<Usuario?> ObtenerEncargadoAsync(int encargadoId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Cita>> ListarAsync(int encargadoId, CancellationToken cancellationToken);
    Task<Cita?> ObtenerAsync(long id, int encargadoId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Cita>> ListarClienteAsync(int clienteId, CancellationToken ct);
    Task<Cita?> ObtenerClienteAsync(long id, int clienteId, CancellationToken ct);
    Task<FormularioReservaDTO?> FormularioAsync(int encargadoId, CancellationToken ct, int? sucursalId = null);
    Task<AgendaReservaDTO?> AgendaAsync(int encargadoId, int servicioId, DateOnly fecha, CancellationToken ct, int? sucursalId = null);
    Task<ResultadoCrearReservaDTO> CrearAsync(CrearReservaDTO solicitud, int encargadoId, int creadorId, CancellationToken ct);
}
