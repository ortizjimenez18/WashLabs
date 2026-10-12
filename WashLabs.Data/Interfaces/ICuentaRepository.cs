using WashLabs.Data.Entities;
namespace WashLabs.Data.Interfaces;
public interface ICuentaRepository
{
    Task<Usuario?> PorCorreoAsync(string correo, CancellationToken ct);
    Task<Usuario?> PorIdAsync(int id, CancellationToken ct);
    Task<string?> RegistrarAsync(Usuario usuario, CancellationToken ct);
    Task GuardarClaveAsync(int id, string hash, CancellationToken ct);
}
