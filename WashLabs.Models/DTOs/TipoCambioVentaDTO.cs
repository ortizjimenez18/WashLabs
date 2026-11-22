namespace WashLabs.Models.DTOs;

public sealed record TipoCambioVentaDTO(decimal Valor, DateOnly Fecha)
{
    public decimal Equivalente(decimal colones) => Math.Round(colones / Valor, 2, MidpointRounding.AwayFromZero);
}
