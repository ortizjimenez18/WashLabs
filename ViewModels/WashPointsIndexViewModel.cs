using WashLabs.Models.DTOs;
namespace WashLabs.ViewModels;
public sealed record WashPointsIndexViewModel(IReadOnlyList<ClienteOpcionDTO> Clientes, int? ClienteId,
    IReadOnlyList<WashPointsVehiculoDTO> Puntos);
