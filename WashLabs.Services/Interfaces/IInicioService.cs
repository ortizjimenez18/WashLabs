using WashLabs.Models.DTOs;

namespace WashLabs.Services.Interfaces;

public interface IInicioService
{
    Task<InicioDTO> ObtenerAsync(CancellationToken cancellationToken = default);
}
