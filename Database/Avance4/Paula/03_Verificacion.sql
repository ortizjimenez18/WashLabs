SET NOCOUNT ON;
IF DB_NAME() <> N'WashLabsDB' THROW 51000, 'Ejecute en WashLabsDB.', 1;
SELECT COUNT(*) TotalServicios, SUM(CASE WHEN Activo=1 THEN 1 ELSE 0 END) ServiciosActivos FROM dbo.Servicios;
SELECT COUNT(*) TotalProductos, SUM(CASE WHEN Activo=1 THEN 1 ELSE 0 END) ProductosActivos FROM dbo.Productos;
SELECT c.CategoriaProductoId,c.Nombre,COUNT(p.ProductoId) Productos
FROM dbo.CategoriasProducto c LEFT JOIN dbo.Productos p ON p.CategoriaProductoId=c.CategoriaProductoId
GROUP BY c.CategoriaProductoId,c.Nombre ORDER BY c.Nombre;
SELECT s.ServicioId,s.Nombre,s.TarifaBase,s.DuracionMinutos,s.TipoLavado,s.NivelDetalle,s.ImagenUrl,
CASE WHEN s.ImagenUrl IS NULL THEN N'Sin imagen: utiliza placeholder'
WHEN s.ImagenUrl LIKE '/images/%' AND s.ImagenUrl NOT LIKE '%..%' AND s.ImagenUrl NOT LIKE '%\%' THEN N'Referencia local'
ELSE N'Revisar referencia' END EstadoImagen
FROM dbo.Servicios s ORDER BY s.Nombre;
SELECT p.ProductoId,p.Nombre,c.Nombre Categoria,p.Precio,p.Marca,p.Presentacion,p.SKU,p.ImagenUrl,p.EsPersonalizable,
CASE WHEN p.ImagenUrl IS NULL THEN N'Sin imagen: utiliza placeholder'
WHEN (p.ImagenUrl LIKE '/images/%' OR p.ImagenUrl LIKE '~/images/%') AND p.ImagenUrl NOT LIKE '%..%' AND p.ImagenUrl NOT LIKE '%\%' THEN N'Referencia local'
ELSE N'Revisar referencia' END EstadoImagen
FROM dbo.Productos p JOIN dbo.CategoriasProducto c ON c.CategoriaProductoId=p.CategoriaProductoId ORDER BY p.Nombre;
SELECT p.ProductoId,p.Nombre,p.EsPersonalizable,COUNT(c.CriterioProductoId) Criterios
FROM dbo.Productos p LEFT JOIN dbo.CriteriosProducto c ON c.ProductoId=p.ProductoId
WHERE p.EsPersonalizable=1 GROUP BY p.ProductoId,p.Nombre,p.EsPersonalizable;
IF EXISTS (SELECT 1 FROM dbo.Productos p LEFT JOIN dbo.CategoriasProducto c ON c.CategoriaProductoId=p.CategoriaProductoId WHERE c.CategoriaProductoId IS NULL)
    THROW 51001, 'Hay productos sin categoría válida.', 1;
IF EXISTS (SELECT SKU FROM dbo.Productos WHERE SKU IS NOT NULL GROUP BY SKU HAVING COUNT(*)>1)
    THROW 51002, 'Hay SKU duplicados.', 1;
IF EXISTS (SELECT 1 FROM dbo.Servicios WHERE DuracionMinutos<=0 OR TarifaBase<0 OR LEN(LTRIM(RTRIM(Nombre)))=0 OR LEN(LTRIM(RTRIM(Descripcion)))=0)
    THROW 51003, 'Hay servicios con datos inválidos.', 1;
IF EXISTS (SELECT 1 FROM dbo.Productos WHERE Precio<0 OR LEN(LTRIM(RTRIM(Nombre)))=0 OR LEN(LTRIM(RTRIM(Descripcion)))=0)
    THROW 51004, 'Hay productos con datos inválidos.', 1;
SELECT N'Verificación completada sin modificar datos.' Resultado;
