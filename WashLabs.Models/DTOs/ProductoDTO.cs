namespace WashLabs.Models.DTOs;

public sealed record ProductoDTO(int ProductoId, int CategoriaProductoId, string Nombre,
    string Descripcion, string Categoria, decimal Precio, string? Marca,
    string? Presentacion, string? ImagenUrl, bool EsPersonalizable, string? SKU = null, bool Activo = true);

public sealed record CategoriaProductoDTO(int CategoriaProductoId, string Nombre);
