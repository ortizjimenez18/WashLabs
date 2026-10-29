using System.ComponentModel.DataAnnotations;

namespace WashLabs.Models.DTOs;

public sealed class FiltroFacturacionDTO : IValidatableObject
{
    [RegularExpression("^(FACTURA|PROFORMA)$", ErrorMessage = "Selecciona un tipo de documento válido.")]
    public string? TipoDocumento { get; set; }
    public byte? EstadoFacturaId { get; set; }
    [DataType(DataType.Date)]
    public DateOnly? FechaInicial { get; set; }
    [DataType(DataType.Date)]
    public DateOnly? FechaFinal { get; set; }
    [StringLength(120, ErrorMessage = "La búsqueda de cliente admite hasta 120 caracteres.")]
    public string? Cliente { get; set; }
    [Range(1, long.MaxValue, ErrorMessage = "El número de documento debe ser positivo.")]
    public long? NumeroDocumento { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una sucursal válida.")]
    public int? SucursalId { get; set; }
    [RegularExpression("^(recientes|antiguas|mayor|menor)$", ErrorMessage = "Selecciona un orden válido.")]
    public string Orden { get; set; } = "recientes";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FechaInicial.HasValue && FechaFinal.HasValue && FechaInicial > FechaFinal)
            yield return new ValidationResult("La fecha inicial no puede ser posterior a la final.", [nameof(FechaInicial), nameof(FechaFinal)]);
    }
}

public sealed record EstadoFacturaOpcionDTO(byte EstadoFacturaId, string Nombre);
public sealed record ResumenFacturacionDTO(int Facturas, int ProformasPendientes, decimal TotalFacturado);
public sealed record HistorialFacturacionDTO(IReadOnlyList<FacturaResumenDTO> Documentos,
    ResumenFacturacionDTO Resumen, IReadOnlyList<EstadoFacturaOpcionDTO> Estados,
    IReadOnlyList<SucursalReservaDTO> Sucursales);
