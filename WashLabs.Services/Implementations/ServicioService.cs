using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using WashLabs.Data.Interfaces;
using WashLabs.Models.DTOs;
using WashLabs.Models.ViewModels;
using WashLabs.Services.Interfaces;
using WashLabs.Services.Mappers;

namespace WashLabs.Services.Implementations;

public sealed class ServicioService(IServicioRepository repository) : IServicioService
{
    public async Task<IReadOnlyList<ServicioAdministracionDTO>> ListarAsync(CancellationToken ct = default) =>
        (await repository.ListarAsync(ct)).Select(ServicioMapper.ToDTO).ToArray();
    public async Task<ServicioAdministracionDTO?> ObtenerAsync(int id, CancellationToken ct = default) =>
        await repository.ObtenerAsync(id, ct) is { } s ? ServicioMapper.ToDTO(s) : null;
    public async Task<ServicioFormulario?> FormularioAsync(int id, CancellationToken ct = default) =>
        await repository.ObtenerAsync(id, ct) is { } s ? ServicioMapper.ToFormulario(s) : null;
    public async Task<string?> GuardarAsync(ServicioFormulario f, bool crear, string? nuevaImagen, CancellationToken ct = default)
    {
        var errores = new List<ValidationResult>();
        if (!Validator.TryValidateObject(f, new ValidationContext(f), errores, true)) return errores[0].ErrorMessage;
        if (decimal.Round(f.TarifaBase, 2) != f.TarifaBase) return "La tarifa admite hasta dos decimales.";
        if (await repository.NombreExisteAsync(f.Nombre.Trim(), crear ? 0 : f.ServicioId, ct)) return "Ya existe un servicio con ese nombre.";
        var actual = crear ? null : await repository.ObtenerAsync(f.ServicioId, ct);
        if (!crear && actual is null) return "El servicio ya no existe.";
        try
        {
            return await repository.GuardarAsync(ServicioMapper.ToEntity(f, nuevaImagen ?? actual?.ImagenUrl), crear, ct)
                ? null : "El servicio ya no existe.";
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && sql.Number is 2601 or 2627)
        { return "Ya existe un servicio con ese nombre."; }
        catch (DbUpdateException) { return "No se pudo guardar el servicio. Revise los valores e intente nuevamente."; }
    }
}
