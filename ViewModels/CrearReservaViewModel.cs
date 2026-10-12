using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using WashLabs.Models.DTOs;
namespace WashLabs.ViewModels;

public sealed class CrearReservaViewModel
{
    public CrearReservaDTO Reserva { get; set; } = new();
    [ValidateNever] public FormularioReservaDTO? Formulario { get; set; }
    [ValidateNever] public IReadOnlyList<ClienteOpcionDTO> Clientes { get; set; } = [];
    [ValidateNever] public IReadOnlyList<VehiculoDTO> Vehiculos { get; set; } = [];
    [ValidateNever] public IReadOnlyList<TimeOnly> Horarios { get; set; } = [];
}
