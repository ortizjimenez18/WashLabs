using WashLabs.Data.Entities;
using WashLabs.Models.DTOs;

namespace WashLabs.Services.Mappers;

public static class ProductoMapper
{
    public static ProductoDTO ToDTO(Producto producto) => new(producto.ProductoId,
        producto.CategoriaProductoId, producto.Nombre, producto.Descripcion,
        producto.CategoriaProducto.Nombre, producto.Precio, producto.Marca,
        producto.Presentacion, producto.ImagenUrl, producto.EsPersonalizable, producto.SKU, producto.Activo);

    public static CategoriaProductoDTO ToDTO(CategoriaProducto categoria) =>
        new(categoria.CategoriaProductoId, categoria.Nombre);
}
