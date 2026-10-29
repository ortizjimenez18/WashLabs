namespace WashLabs.Models.DTOs;

public sealed record ServicioDTO(int ServicioId, string Nombre, string Descripcion,
    decimal TarifaBase, int DuracionMinutos);
public sealed record SucursalDTO(int SucursalId, string Nombre, string Descripcion,
    string Direccion, string Telefono, string Correo);
public sealed record NegocioDTO(string Nombre, string Descripcion);
public sealed record InicioDTO(NegocioDTO? Negocio, IReadOnlyList<ServicioDTO> Servicios,
    IReadOnlyList<ProductoDTO> Productos, IReadOnlyList<SucursalDTO> Sucursales);
