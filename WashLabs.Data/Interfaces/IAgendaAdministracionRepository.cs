using WashLabs.Data.Entities;
using WashLabs.Models.DTOs;
using WashLabs.Models.ViewModels;

namespace WashLabs.Data.Interfaces;

public interface IAgendaAdministracionRepository
{
    Task<IReadOnlyList<Sucursal>> SucursalesAsync(CancellationToken ct);
    Task<IReadOnlyList<HorarioSucursal>> HorariosAsync(int sucursalId, CancellationToken ct);
    Task<IReadOnlyList<BloqueoAgenda>> BloqueosAsync(int sucursalId, CancellationToken ct);
    Task<HorarioSucursal?> HorarioAsync(int id, CancellationToken ct);
    Task<BloqueoAgenda?> BloqueoAsync(long id, CancellationToken ct);
    Task<ResultadoAgendaDTO> GuardarAsync(AgendaFormulario form, int actor,
        Func<ContextoAgendaDTO, string?> validar, CancellationToken ct);
}
