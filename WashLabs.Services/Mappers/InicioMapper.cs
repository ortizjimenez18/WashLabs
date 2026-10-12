using WashLabs.Data.Entities;
using WashLabs.Models.DTOs;

namespace WashLabs.Services.Mappers;

public static class InicioMapper
{
    public static NegocioDTO ToDTO(ConfiguracionSistema negocio) => new(negocio.NombreNegocio, negocio.DescripcionNegocio);
    public static ServicioDTO ToDTO(Servicio servicio) => new(servicio.ServicioId, servicio.Nombre,
        servicio.Descripcion, servicio.TarifaBase, servicio.DuracionMinutos);
    public static SucursalDTO ToDTO(Sucursal sucursal) => new(sucursal.SucursalId, sucursal.Nombre,
        sucursal.Descripcion, sucursal.Direccion, sucursal.Telefono, sucursal.Correo);
}
