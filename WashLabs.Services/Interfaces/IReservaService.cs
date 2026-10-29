using WashLabs.Models.DTOs;

namespace WashLabs.Services.Interfaces;

public interface IReservaService
{
    Task<ReservasHistorialDTO?> ListarAsync(int encargadoId, CancellationToken cancellationToken = default);
    Task<ReservaDTO?> ObtenerAsync(long id, int encargadoId, CancellationToken cancellationToken = default);
    Task<ReservasHistorialDTO> ListarClienteAsync(int clienteId, CancellationToken ct = default);
    Task<ReservaDTO?> ObtenerClienteAsync(long id, int clienteId, CancellationToken ct = default);
    Task<FormularioReservaDTO?> FormularioAsync(int encargadoId, CancellationToken ct = default, int? sucursalId = null);
    Task<IReadOnlyList<TimeOnly>> HorariosAsync(int encargadoId, int servicioId, DateOnly fecha, CancellationToken ct = default, int? sucursalId = null);
    Task<ResultadoCrearReservaDTO> CrearAsync(CrearReservaDTO solicitud, int encargadoId, CancellationToken ct = default, int? creadorId = null);
}
