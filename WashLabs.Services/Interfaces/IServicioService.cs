using WashLabs.Models.DTOs;
using WashLabs.Models.ViewModels;

namespace WashLabs.Services.Interfaces;

public interface IServicioService
{
    Task<IReadOnlyList<ServicioAdministracionDTO>> ListarAsync(CancellationToken ct = default);
    Task<ServicioAdministracionDTO?> ObtenerAsync(int id, CancellationToken ct = default);
    Task<ServicioFormulario?> FormularioAsync(int id, CancellationToken ct = default);
    Task<string?> GuardarAsync(ServicioFormulario f, bool crear, string? nuevaImagen, CancellationToken ct = default);
}
