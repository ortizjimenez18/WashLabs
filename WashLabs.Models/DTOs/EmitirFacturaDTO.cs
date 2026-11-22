using System.ComponentModel.DataAnnotations;

namespace WashLabs.Models.DTOs;

public sealed class EmitirFacturaDTO
{
    public Guid SolicitudId { get; set; } = Guid.NewGuid();
    [Range(1, int.MaxValue)] public int SucursalId { get; set; }
    [Range(1, int.MaxValue)] public int ClienteId { get; set; }
    public List<LineaNuevaFacturaDTO> Lineas { get; set; } = [];
}

public sealed class LineaNuevaFacturaDTO
{
    public string? Articulo { get; set; }
    public int Cantidad { get; set; } = 1;
}
