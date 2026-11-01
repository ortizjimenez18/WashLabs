using WashLabs.Models.DTOs;

namespace WashLabs.Mappers;

/// <summary>Imágenes locales de respaldo para los cuatro productos identificados del catálogo.</summary>
public static class ImagenProductoMapper
{
    public static string? Respaldo(ProductoDTO producto) => (producto.ProductoId, producto.SKU) switch
    {
        (12, "A4-PAULA-001") => "images/productos/washlabs-shampoo.jpg",
        (13, "A4-PAULA-002") => "images/productos/washlabs-cera.jpg",
        (14, "A4-PAULA-003") => "images/productos/washlabs-aromatizante.jpg",
        (15, "A4-PAULA-004") => "images/productos/washlabs-microfibra.jpg",
        _ => null
    };
}
