using WashLabs.Data.Entities;
using WashLabs.Models.DTOs;
using WashLabs.Models.ViewModels;

namespace WashLabs.Services.Mappers;

public static class ServicioMapper
{
    public static ServicioAdministracionDTO ToDTO(Servicio s) => new(s.ServicioId, s.Nombre, s.Descripcion, s.TarifaBase,
        s.DuracionMinutos, s.TipoLavado, s.NivelDetalle, s.Activo, s.ImagenUrl);
    public static ServicioFormulario ToFormulario(Servicio s) => new() {
        ServicioId = s.ServicioId, Nombre = s.Nombre, Descripcion = s.Descripcion, TarifaBase = s.TarifaBase,
        DuracionMinutos = s.DuracionMinutos, TipoLavado = s.TipoLavado, NivelDetalle = s.NivelDetalle,
        Activo = s.Activo, ImagenUrl = s.ImagenUrl };
    public static Servicio ToEntity(ServicioFormulario f, string? imagen) => new() {
        ServicioId = f.ServicioId, Nombre = f.Nombre.Trim(), Descripcion = f.Descripcion.Trim(),
        TarifaBase = f.TarifaBase, DuracionMinutos = f.DuracionMinutos, TipoLavado = f.TipoLavado.Trim(),
        NivelDetalle = f.NivelDetalle, Activo = f.Activo, ImagenUrl = imagen };
}
