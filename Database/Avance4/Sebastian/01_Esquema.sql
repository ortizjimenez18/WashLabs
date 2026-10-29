/* SQL Server 2019. No elimina datos ni cambia columnas/relaciones.
   Requiere un FULL COPY_ONLY con CHECKSUM y RESTORE VERIFYONLY antes de ejecutar.
   La ruta de respaldo debe ser accesible para la cuenta del servicio SQL Server.
   No versionar respaldos. Ejecutar primero scripts/Avance4-Sebastian.ps1 -Accion Respaldo.
   Ajustes: permitir Encargado sin sucursal; permitir duración <8h SOLO en fecha puntual.
   Se mantienen los horarios recurrentes, la numeración lunes=1/domingo=7 y sus datos.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF DB_NAME() <> N'WashLabsDB' THROW 51000, 'Ejecute en WashLabsDB.', 1;
DECLARE @backup nvarchar(4000);
SELECT TOP (1) @backup=m.physical_device_name
FROM msdb.dbo.backupset b JOIN msdb.dbo.backupmediafamily m ON m.media_set_id=b.media_set_id
WHERE b.database_name=DB_NAME() AND b.type='D' AND b.has_backup_checksums=1
 AND b.backup_finish_date>=DATEADD(hour,-24,SYSDATETIME()) AND b.is_copy_only=1
ORDER BY b.backup_finish_date DESC;
IF @backup IS NULL THROW 51001, 'Falta respaldo FULL COPY_ONLY con CHECKSUM de las últimas 24 horas.', 1;
RESTORE VERIFYONLY FROM DISK=@backup WITH CHECKSUM;
IF OBJECT_ID('dbo.Sucursales') IS NULL OR OBJECT_ID('dbo.Usuarios') IS NULL
 OR OBJECT_ID('dbo.HorariosSucursal') IS NULL OR OBJECT_ID('dbo.BloqueosAgenda') IS NULL
 THROW 51002, 'No existe la estructura esperada del Avance 3.', 1;
BEGIN TRANSACTION;
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name='CK_Usuarios_EncargadoSucursal')
 ALTER TABLE dbo.Usuarios DROP CONSTRAINT CK_Usuarios_EncargadoSucursal;
-- La FK existente continúa garantizando que toda sucursal asignada exista.
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name='CK_HorariosSucursal_Horas')
 ALTER TABLE dbo.HorariosSucursal DROP CONSTRAINT CK_HorariosSucursal_Horas;
ALTER TABLE dbo.HorariosSucursal WITH CHECK ADD CONSTRAINT CK_HorariosSucursal_Horas
 CHECK (HoraInicio < HoraFin AND
   ((FechaVigenciaHasta IS NOT NULL AND FechaVigenciaHasta=FechaVigenciaDesde) OR DATEDIFF(minute,HoraInicio,HoraFin)>=480));
COMMIT;
SELECT name, definition, is_disabled, is_not_trusted FROM sys.check_constraints
WHERE parent_object_id IN (OBJECT_ID('dbo.HorariosSucursal'),OBJECT_ID('dbo.Usuarios'));
