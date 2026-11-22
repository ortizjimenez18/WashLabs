using WashLabs.Models.DTOs;

namespace WashLabs.ViewModels;

public sealed record EmitirFacturaViewModel(EmitirFacturaDTO Solicitud, FormularioReservaDTO Formulario,
    string Encargado, IReadOnlyList<ClienteOpcionDTO> Clientes, IReadOnlyList<ProductoDTO> Productos,
    TipoCambioVentaDTO? Tasa, string Cotizacion);
