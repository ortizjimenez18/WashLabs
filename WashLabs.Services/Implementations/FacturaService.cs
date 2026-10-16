using WashLabs.Data.Interfaces;
using WashLabs.Models.DTOs;
using WashLabs.Services.Interfaces;
using WashLabs.Services.Mappers;
using System.ComponentModel.DataAnnotations;

namespace WashLabs.Services.Implementations;

public sealed class FacturaService(IFacturaRepository repository) : IFacturaService
{
    public Task<HistorialFacturacionDTO> OpcionesAsync(int usuarioId, CancellationToken ct) => repository.OpcionesAsync(usuarioId, ct);
    public Task<HistorialFacturacionDTO> ConsultarAsync(FiltroFacturacionDTO filtro, int usuarioId, CancellationToken ct)
    {
        Validator.ValidateObject(filtro, new ValidationContext(filtro), true);
        return repository.ConsultarAsync(filtro, usuarioId, ct);
    }
    public Task<bool> CambiarCantidadAsync(long id, long detalleId, decimal cantidad, int usuarioId, CancellationToken ct) =>
        repository.CambiarCantidadAsync(id, detalleId, cantidad, usuarioId, ct);
    public Task<bool> QuitarProductoAsync(long id, long detalleId, int usuarioId, CancellationToken ct) =>
        repository.QuitarProductoAsync(id, detalleId, usuarioId, ct);
    public Task<bool> AgregarProductoAsync(long id, int productoId, decimal cantidad, int usuarioId, CancellationToken ct) =>
        repository.AgregarProductoAsync(id, productoId, cantidad, usuarioId, ct);
    public async Task<IReadOnlyList<FacturaResumenDTO>> ListarAsync(CancellationToken cancellationToken, int usuarioId) =>
        (await repository.ListarAsync(cancellationToken, usuarioId)).Select(FacturaMapper.ToResumenDTO).ToArray();

    public async Task<FacturaDTO?> ObtenerAsync(long id, CancellationToken cancellationToken, int usuarioId)
    {
        if (id <= 0) return null;
        var factura = await repository.ObtenerAsync(id, cancellationToken, usuarioId);
        return factura is null ? null : FacturaMapper.ToDTO(factura) with { NombreNegocio = await repository.NombreNegocioAsync(cancellationToken) };
    }
}
