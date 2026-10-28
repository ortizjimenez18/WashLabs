SET NOCOUNT ON;
SET XACT_ABORT ON;
IF DB_NAME() <> N'WashLabsDB' THROW 51000, 'Ejecute en WashLabsDB.', 1;
DECLARE @respaldo nvarchar(4000);
SELECT TOP (1) @respaldo=m.physical_device_name
FROM msdb.dbo.backupset b JOIN msdb.dbo.backupmediafamily m ON m.media_set_id=b.media_set_id
WHERE b.database_name=DB_NAME() AND b.type='D' AND b.has_backup_checksums=1
AND b.is_copy_only=1 AND b.backup_finish_date>=DATEADD(hour,-24,SYSDATETIME())
ORDER BY b.backup_finish_date DESC;
IF @respaldo IS NULL THROW 51001, 'Se requiere respaldo FULL COPY_ONLY con CHECKSUM de las últimas 24 horas.', 1;
RESTORE VERIFYONLY FROM DISK=@respaldo WITH CHECKSUM;
IF OBJECT_ID('dbo.Servicios') IS NULL OR OBJECT_ID('dbo.Productos') IS NULL
OR OBJECT_ID('dbo.CategoriasProducto') IS NULL THROW 51002, 'Falta el esquema del Avance 3.', 1;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.Servicios','ImagenUrl') IS NULL
    ALTER TABLE dbo.Servicios ADD ImagenUrl nvarchar(500) NULL;
COMMIT;
-- Productos ya dispone de ImagenUrl, Marca, Presentacion, SKU y categoría con FK.
-- Validación de solo lectura de las columnas y restricciones reutilizadas.
SELECT OBJECT_NAME(object_id) Tabla, name Columna, TYPE_NAME(user_type_id) Tipo, max_length, is_nullable
FROM sys.columns WHERE object_id IN (OBJECT_ID('dbo.Servicios'),OBJECT_ID('dbo.Productos'));
SELECT name, definition FROM sys.check_constraints
WHERE parent_object_id IN (OBJECT_ID('dbo.Servicios'),OBJECT_ID('dbo.Productos'));
