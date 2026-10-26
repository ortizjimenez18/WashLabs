using WashLabs.Models.DTOs;
using WashLabs.Models.ViewModels;

namespace WashLabs.Services.Interfaces;

public interface ISucursalAdministracionService
{
    Task<IReadOnlyList<SucursalAdministracionDTO>> ListarAsync(CancellationToken ct = default);
    Task<SucursalAdministracionDTO?> ObtenerAsync(int id, CancellationToken ct = default);
    Task<SucursalFormulario?> FormularioAsync(int? id, CancellationToken ct = default);
    Task CargarEncargadosAsync(SucursalFormulario form, CancellationToken ct = default);
    Task<string?> GuardarAsync(SucursalFormulario form, bool crear, CancellationToken ct = default);
}
