namespace WashLabs.Models.DTOs;

public sealed record ServicioAdministracionDTO(int ServicioId, string Nombre, string Descripcion, decimal TarifaBase,
    int DuracionMinutos, string TipoLavado, string NivelDetalle, bool Activo, string? ImagenUrl);
