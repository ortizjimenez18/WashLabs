using WashLabs.Models.DTOs;
namespace WashLabs.Services.Interfaces;

public interface IOperacionService
{
    Task<IReadOnlyList<ClienteOpcionDTO>> ClientesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<VehiculoDTO>> VehiculosAsync(CancellationToken ct = default, int? clienteId = null);
    Task<RegistroVehiculoResultadoDTO> RegistrarVehiculoAsync(RegistroVehiculoDTO solicitud, CancellationToken ct = default);
    Task<IReadOnlyList<WashPointsVehiculoDTO>> WashPointsAsync(CancellationToken ct = default, int? clienteId = null);
    Task<IReadOnlyList<CategoriaAsistenteDTO>> CategoriasAsistenteAsync(CancellationToken ct = default);
    Task<PreguntaAsistenteDTO?> PreguntaAsync(int id, CancellationToken ct = default);
    Task<bool> CompletarCitaAsync(long id, int encargadoId, CancellationToken ct = default);
    Task<bool> CancelarCitaAsync(long id, int usuarioId, CancellationToken ct = default);
    Task<bool> AsignarVehiculoAsync(long citaId, int vehiculoId, int encargadoId, CancellationToken ct = default);
}
