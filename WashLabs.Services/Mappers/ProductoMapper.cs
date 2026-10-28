using WashLabs.Data.Entities;
using WashLabs.Models.DTOs;
using WashLabs.Models.ViewModels;

namespace WashLabs.Services.Mappers;

public static class ProductoMapper
{
    public static ProductoFormulario ToFormulario(Producto p) => new() {
        ProductoId = p.ProductoId, Nombre = p.Nombre, Descripcion = p.Descripcion, CategoriaProductoId = p.CategoriaProductoId,
        Precio = p.Precio, Marca = p.Marca, Presentacion = p.Presentacion, SKU = p.SKU, ImagenUrl = p.ImagenUrl, Activo = p.Activo };
    public static Producto ToEntity(ProductoFormulario f, string? imagen) => new() {
        ProductoId = f.ProductoId, Nombre = f.Nombre.Trim(), Descripcion = f.Descripcion.Trim(), CategoriaProductoId = f.CategoriaProductoId,
        Precio = f.Precio, Marca = string.IsNullOrWhiteSpace(f.Marca) ? null : f.Marca.Trim(),
        Presentacion = string.IsNullOrWhiteSpace(f.Presentacion) ? null : f.Presentacion.Trim(),
        SKU = string.IsNullOrWhiteSpace(f.SKU) ? null : f.SKU.Trim(), ImagenUrl = imagen, Activo = f.Activo };
    public static ProductoDTO ToDTO(Producto producto) => new(producto.ProductoId,
        producto.CategoriaProductoId, producto.Nombre, producto.Descripcion,
        producto.CategoriaProducto.Nombre, producto.Precio, producto.Marca,
        producto.Presentacion, producto.ImagenUrl, producto.EsPersonalizable, producto.SKU, producto.Activo);

    public static CategoriaProductoDTO ToDTO(CategoriaProducto categoria) =>
        new(categoria.CategoriaProductoId, categoria.Nombre);
}
