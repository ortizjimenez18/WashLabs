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
DECLARE @respaldo nvarchar(4000);
SELECT TOP (1) @respaldo=m.physical_device_name
FROM msdb.dbo.backupset b JOIN msdb.dbo.backupmediafamily m ON m.media_set_id=b.media_set_id
WHERE b.database_name=DB_NAME() AND b.type='D' AND b.has_backup_checksums=1
AND b.is_copy_only=1 AND b.backup_finish_date>=DATEADD(hour,-24,SYSDATETIME())
ORDER BY b.backup_finish_date DESC;
IF @respaldo IS NULL THROW 51001, 'Se requiere respaldo FULL COPY_ONLY con CHECKSUM de las últimas 24 horas.', 1;
RESTORE VERIFYONLY FROM DISK=@respaldo WITH CHECKSUM;
IF COL_LENGTH('dbo.Servicios','ImagenUrl') IS NULL THROW 51002, 'Ejecute primero 01_Esquema.sql.', 1;
IF (SELECT COUNT(*) FROM dbo.CategoriasProducto WHERE Nombre IN (N'Shampoo',N'Ceras y selladores',N'Aromatizantes',N'Microfibras y accesorios'))<>4
    THROW 51003, 'Faltan categorías predeterminadas. No se crearán categorías nuevas.', 1;
BEGIN TRANSACTION;
INSERT INTO dbo.Servicios (Nombre,Descripcion,TarifaBase,DuracionMinutos,TipoLavado,NivelDetalle,Activo,ImagenUrl)
SELECT s.Nombre,s.Descripcion,s.Tarifa,s.Minutos,s.Tipo,s.Nivel,1,NULL
FROM (VALUES
    (N'A4 Paula - Lavado exterior',N'Servicio de prueba de lavado exterior.',4500.00,30,'EXTERIOR','BASICO'),
    (N'A4 Paula - Limpieza interior',N'Servicio de prueba de limpieza interior.',6500.00,60,'INTERIOR','INTERMEDIO'),
    (N'A4 Paula - Lavado completo',N'Servicio de prueba de lavado completo.',9500.00,90,'COMPLETO','PROFUNDO'),
    (N'A4 Paula - Protección de pintura',N'Servicio de prueba de protección de pintura.',15000.00,120,'PROTECCION','PREMIUM')
) s(Nombre,Descripcion,Tarifa,Minutos,Tipo,Nivel)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Servicios actual WITH (UPDLOCK,HOLDLOCK) WHERE actual.Nombre=s.Nombre);
INSERT INTO dbo.Productos (CategoriaProductoId,Nombre,Descripcion,Precio,Marca,Presentacion,SKU,ImagenUrl,EsPersonalizable,Activo)
SELECT c.CategoriaProductoId,p.Nombre,p.Descripcion,p.Precio,N'WashLabs',p.Presentacion,p.SKU,NULL,0,1
FROM (VALUES
    ('A4-PAULA-001',N'A4 Paula - Shampoo',N'Producto de prueba para mantenimiento de shampoo.',3500.00,N'500 ml',N'Shampoo'),
    ('A4-PAULA-002',N'A4 Paula - Cera',N'Producto de prueba para mantenimiento de cera.',6500.00,N'250 g',N'Ceras y selladores'),
    ('A4-PAULA-003',N'A4 Paula - Aromatizante',N'Producto de prueba para mantenimiento de aromatizante.',2000.00,N'100 ml',N'Aromatizantes'),
    ('A4-PAULA-004',N'A4 Paula - Microfibra',N'Producto de prueba para mantenimiento de microfibras.',2500.00,N'Paquete de 3',N'Microfibras y accesorios')
) p(SKU,Nombre,Descripcion,Precio,Presentacion,Categoria)
JOIN dbo.CategoriasProducto c ON c.Nombre=p.Categoria
WHERE NOT EXISTS (SELECT 1 FROM dbo.Productos actual WITH (UPDLOCK,HOLDLOCK) WHERE actual.SKU=p.SKU);
COMMIT;
SELECT COUNT(*) ServiciosPrueba FROM dbo.Servicios WHERE Nombre LIKE N'A4 Paula - %';
SELECT COUNT(*) ProductosPrueba FROM dbo.Productos WHERE SKU LIKE 'A4-PAULA-%';
