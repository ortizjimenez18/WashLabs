using WashLabs.Data.Interfaces;
using WashLabs.Models.DTOs;
using WashLabs.Services.Interfaces;
using WashLabs.Services.Mappers;

namespace WashLabs.Services.Implementations;

public sealed class InicioService(IInicioRepository repository, IProductoService productos) : IInicioService
{
    public async Task<InicioDTO> ObtenerAsync(CancellationToken cancellationToken = default)
    {
        // El DbContext scoped se comparte: las consultas se esperan secuencialmente.
        var negocio = await repository.ObtenerNegocioAsync(cancellationToken);
        var servicios = await repository.ListarServiciosAsync(cancellationToken);
        var destacados = await productos.ListarAsync(null, null, cancellationToken);
        var sucursales = await repository.ListarSucursalesAsync(cancellationToken);
        return new(negocio is null ? null : InicioMapper.ToDTO(negocio),
            servicios.Select(InicioMapper.ToDTO).ToArray(),
            destacados.OrderByDescending(p => p.EsPersonalizable).ThenBy(p => p.Nombre).Take(3).ToArray(),
            sucursales.Select(InicioMapper.ToDTO).ToArray());
    }
}
