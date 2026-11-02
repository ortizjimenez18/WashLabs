-- Ejecutar en la base de WashLabs después de copiar wwwroot/images/servicios.
-- Solo completa imágenes vacías de los registros identificados por ID y nombre exactos.
SET XACT_ABORT ON;
IF DB_NAME() <> N'WashLabsDB' THROW 51000, 'Ejecute en WashLabsDB.', 1;
BEGIN TRANSACTION;
DECLARE @imagenes TABLE (ServicioId int PRIMARY KEY, Nombre nvarchar(120), ImagenUrl nvarchar(500));
INSERT INTO @imagenes VALUES
(1,N'Wash Express',N'/images/servicios/wash-express.jpg'),
(2,N'Wash Complete',N'/images/servicios/wash-complete.jpg'),
(3,N'Interior Deep Clean',N'/images/servicios/interior-deep-clean.jpg'),
(4,N'Baño Descontaminante',N'/images/servicios/bano-descontaminante.jpg'),
(5,N'Nano Cerámico',N'/images/servicios/nano-ceramico.jpg'),
(6,N'Cerámico Premium',N'/images/servicios/ceramico-premium.jpg'),
(15,N'A4 Paula - Lavado exterior',N'/images/servicios/lavado-exterior.jpg'),
(16,N'A4 Paula - Limpieza interior',N'/images/servicios/limpieza-interior.jpg'),
(17,N'A4 Paula - Lavado completo',N'/images/servicios/lavado-completo.jpg'),
(18,N'A4 Paula - Protección de pintura',N'/images/servicios/proteccion-pintura.jpg');
UPDATE s SET ImagenUrl=i.ImagenUrl
OUTPUT inserted.ServicioId,inserted.Nombre,inserted.ImagenUrl
FROM dbo.Servicios s JOIN @imagenes i ON s.ServicioId=i.ServicioId AND s.Nombre=i.Nombre
WHERE NULLIF(LTRIM(RTRIM(s.ImagenUrl)),N'') IS NULL;
COMMIT;
