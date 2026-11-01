-- Opcional: la vista compartida ya resuelve estos archivos cuando falta una imagen válida.
-- Solo persiste ImagenUrl; no reemplaza rutas personalizadas ni cambia otros datos.
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
IF DB_NAME() <> N'WashLabsDB' THROW 51000, 'Ejecute en WashLabsDB.', 1;

DECLARE @imagenes TABLE (ProductoId int PRIMARY KEY, SKU nvarchar(50), Nombre nvarchar(120), ImagenUrl nvarchar(500));
INSERT INTO @imagenes VALUES
    (12, N'A4-PAULA-001', N'Shampoo', N'/images/productos/washlabs-shampoo.jpg'),
    (13, N'A4-PAULA-002', N'Cera', N'/images/productos/washlabs-cera.jpg'),
    (14, N'A4-PAULA-003', N'Aromatizante', N'/images/productos/washlabs-aromatizante.jpg'),
    (15, N'A4-PAULA-004', N'Microfibra', N'/images/productos/washlabs-microfibra.jpg');

BEGIN TRY
    BEGIN TRANSACTION;
    IF (SELECT COUNT(*) FROM dbo.Productos p WITH (UPDLOCK, HOLDLOCK)
        JOIN @imagenes i ON p.ProductoId = i.ProductoId AND p.SKU = i.SKU
        WHERE p.Nombre = i.Nombre OR p.Nombre = N'A4 Paula - ' + i.Nombre) <> 4
        THROW 51001, 'Los cuatro identificadores, SKU y nombres deben coincidir. No se modificó ningún producto.', 1;

    UPDATE p SET ImagenUrl = i.ImagenUrl
    OUTPUT inserted.ProductoId, deleted.ImagenUrl AS ImagenAnterior, inserted.ImagenUrl AS ImagenNueva
    FROM dbo.Productos p JOIN @imagenes i ON p.ProductoId = i.ProductoId AND p.SKU = i.SKU
    WHERE NULLIF(LTRIM(RTRIM(p.ImagenUrl)), N'') IS NULL
        OR p.ImagenUrl IN (N'/images/productos/sin-imagen.svg', N'~/images/productos/sin-imagen.svg');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
