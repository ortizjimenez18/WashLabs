using WashLabs.Models.DTOs;
namespace WashLabs.Services.Interfaces;
public interface ICuentaService
{
    Task<UsuarioSesionDTO?> IniciarAsync(string correo, string clave, CancellationToken ct = default);
    Task<UsuarioSesionDTO?> SesionAsync(int id, CancellationToken ct = default);
    Task<RegistroCuentaResultadoDTO> RegistrarAsync(RegistroCuentaDTO solicitud, CancellationToken ct = default);
    Task<bool> ConfigurarClaveLocalAsync(string correo, string clave, CancellationToken ct = default);
}
