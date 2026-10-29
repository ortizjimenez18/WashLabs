using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using WashLabs.Models.DTOs;
namespace WashLabs.ViewModels;
public sealed class RegistroVehiculoViewModel
{
    public RegistroVehiculoDTO Vehiculo { get; set; } = new();
    [ValidateNever] public IReadOnlyList<ClienteOpcionDTO> Clientes { get; set; } = [];
}
