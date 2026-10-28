namespace WashLabs.Services;

public interface IImagenService
{
    Task<string?> GuardarAsync(IFormFile? archivo, CancellationToken ct);
    void EliminarNueva(string? ruta);
}
