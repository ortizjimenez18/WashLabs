using WashLabs.Data.Entities;

namespace WashLabs.Data.Interfaces;

public interface IOperacionRepository
{
    Task<IReadOnlyList<Usuario>> ClientesAsync(CancellationToken ct);
    Task<IReadOnlyList<Vehiculo>> VehiculosAsync(CancellationToken ct, int? clienteId = null);
    Task<string?> RegistrarVehiculoAsync(Vehiculo vehiculo, CancellationToken ct);
    Task<ConfiguracionWashPoints?> ConfiguracionPuntosAsync(CancellationToken ct);
    Task<IReadOnlyList<CategoriaAsistente>> CategoriasAsistenteAsync(CancellationToken ct);
    Task<PreguntaAsistente?> PreguntaAsync(int id, CancellationToken ct);
    Task<bool> CompletarCitaAsync(long id, int encargadoId, CancellationToken ct);
    Task<bool> CancelarCitaAsync(long id, int usuarioId, CancellationToken ct);
    Task<bool> AsignarVehiculoAsync(long citaId, int vehiculoId, int encargadoId, CancellationToken ct);
}
