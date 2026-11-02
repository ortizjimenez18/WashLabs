# Imágenes del catálogo

Los recursos utilizados están en `wwwroot/images/productos/`.

| Producto | Archivo |
|---|---|
| Shampoo | washlabs-shampoo.jpg |
| Aromatizante | washlabs-aromatizante.jpg |
| Cera | washlabs-cera.jpg |
| Microfibra | washlabs-microfibra.jpg |

`Mappers/ImagenProductoMapper.cs` resuelve las asociaciones existentes y conserva las rutas personalizadas. El formulario permite cargar JPG, PNG o WEBP de hasta 2 MB y conservar la imagen anterior al editar. La ausencia de una imagen se comunica mediante el recurso compartido de imagen no disponible.
