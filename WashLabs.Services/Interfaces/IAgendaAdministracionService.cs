using WashLabs.Models.DTOs;
using WashLabs.Models.ViewModels;

namespace WashLabs.Services.Interfaces;

public interface IAgendaAdministracionService
{
    Task<AgendaListado> ListarAsync(int? sucursalId, CancellationToken ct=default);
    Task CargarSucursalesAsync(AgendaFormulario form, CancellationToken ct=default);
    Task<HorarioAdministracionDTO?> HorarioAsync(int id, CancellationToken ct=default);
    Task<BloqueoAdministracionDTO?> BloqueoAsync(long id, CancellationToken ct=default);
    Task<ResultadoAgendaDTO> GuardarAsync(AgendaFormulario form, int actor, CancellationToken ct=default);
}
