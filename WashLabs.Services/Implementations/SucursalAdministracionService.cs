using System.ComponentModel.DataAnnotations;
using WashLabs.Data.Interfaces;
using WashLabs.Models.DTOs;
using WashLabs.Models.ViewModels;
using WashLabs.Services.Interfaces;
using WashLabs.Services.Mappers;

namespace WashLabs.Services.Implementations;

public sealed class SucursalAdministracionService(ISucursalAdministracionRepository repo) : ISucursalAdministracionService
{
    public async Task<IReadOnlyList<SucursalAdministracionDTO>> ListarAsync(CancellationToken ct = default) =>
        (await repo.ListarAsync(ct)).Select(SucursalAdministracionMapper.ToDTO).ToList();
    public async Task<SucursalAdministracionDTO?> ObtenerAsync(int id, CancellationToken ct = default) =>
        await repo.ObtenerAsync(id, ct) is { } s ? SucursalAdministracionMapper.ToDTO(s) : null;
    public async Task<SucursalFormulario?> FormularioAsync(int? id, CancellationToken ct = default)
    {
        var s = id.HasValue ? await ObtenerAsync(id.Value, ct) : null;
        if (id.HasValue && s is null) return null;
        var f = s is null ? new SucursalFormulario() : SucursalAdministracionMapper.ToFormulario(s);
        await CargarEncargadosAsync(f, ct); return f;
    }
    public async Task CargarEncargadosAsync(SucursalFormulario f, CancellationToken ct = default) =>
        f.Disponibles = (await repo.EncargadosAsync(f.SucursalId == 0 ? null : f.SucursalId, ct)).Select(SucursalAdministracionMapper.ToDTO).ToList();
    public async Task<string?> GuardarAsync(SucursalFormulario f, bool crear, CancellationToken ct = default)
    {
        var errores = new List<ValidationResult>();
        if (!Validator.TryValidateObject(f, new ValidationContext(f), errores, true)) return string.Join(" ", errores.Select(e => e.ErrorMessage));
        if (f.EncargadoIds is null || f.EncargadoIds.Distinct().Count() != f.EncargadoIds.Count) return "La selección de encargados no es válida.";
        return await repo.GuardarAsync(SucursalAdministracionMapper.ToEntity(f), f.EncargadoIds, crear, seleccion =>
        {
            if (seleccion.Count != f.EncargadoIds.Count || seleccion.Any(u => u.Rol != "Encargado" ||
                (!u.Activo && (crear || u.SucursalId != f.SucursalId)) ||
                u.SucursalId is not null && (crear || u.SucursalId != f.SucursalId)))
                return "Uno de los encargados no existe, no está activo o ya pertenece a otra sucursal. Actualice la selección.";
            return null;
        }, ct);
    }
}
