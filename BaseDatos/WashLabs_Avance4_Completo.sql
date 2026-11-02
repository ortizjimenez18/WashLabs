SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
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

GO
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

GO
SET XACT_ABORT ON;
SET NOCOUNT ON;
IF DB_NAME() <> N'WashLabsDB' THROW 51000, 'Ejecute en WashLabsDB.', 1;
BEGIN TRANSACTION;
IF (SELECT COUNT(*) FROM dbo.Sucursales) < 4 AND NOT EXISTS (SELECT 1 FROM dbo.Sucursales WHERE Nombre=N'WashLabs San Rafael')
 INSERT dbo.Sucursales(Nombre,Descripcion,Telefono,Direccion,Correo,Activa)
 VALUES(N'WashLabs San Rafael',N'Centro de lavado y cuidado automotriz.',N'2222-4444',N'San Rafael de Alajuela, 200 metros al norte de la iglesia.',N'sanrafael@washlabs.cr',1);
DECLARE @sucursal int=(SELECT TOP(1) SucursalId FROM dbo.Sucursales WHERE Activa=1 ORDER BY SucursalId DESC);
DECLARE @actor int=(SELECT TOP(1) UsuarioId FROM dbo.Usuarios u JOIN dbo.Roles r ON r.RolId=u.RolId WHERE u.Activo=1 AND r.Nombre=N'Administrador');
IF @actor IS NULL THROW 51003, 'Se requiere un administrador activo existente.', 1;
DECLARE @fecha date=DATEADD(day,1,CONVERT(date,SYSUTCDATETIME() AT TIME ZONE 'UTC' AT TIME ZONE 'Central America Standard Time'));
DECLARE @intentos int=0;
WHILE (SELECT COUNT(*) FROM dbo.BloqueosAgenda)<4 AND @intentos<730
BEGIN
 IF NOT EXISTS(SELECT 1 FROM dbo.HorariosSucursal h WHERE h.SucursalId=@sucursal AND h.Activo=1
 AND h.DiaSemana=((DATEDIFF(day,CONVERT(date,'19000101',112),@fecha)%7)+1)
 AND h.FechaVigenciaDesde<=@fecha AND (h.FechaVigenciaHasta IS NULL OR h.FechaVigenciaHasta>=@fecha)
 AND h.HoraInicio<'07:00' AND h.HoraFin>'06:00')
 AND NOT EXISTS(SELECT 1 FROM dbo.BloqueosAgenda b WHERE b.SucursalId=@sucursal AND b.Fecha=@fecha AND (b.DiaCompleto=1 OR (b.HoraInicio<'07:00' AND b.HoraFin>'06:00')))
 AND NOT EXISTS(SELECT 1 FROM dbo.Citas c WHERE c.SucursalId=@sucursal AND c.Fecha=@fecha AND c.HoraInicio<'07:00' AND c.HoraFin>'06:00')
 INSERT dbo.BloqueosAgenda(SucursalId,Fecha,DiaCompleto,HoraInicio,HoraFin,Motivo,CreadoPorUsuarioId)
 VALUES(@sucursal,@fecha,0,'06:00','07:00',N'Mantenimiento preventivo de equipos',@actor);
 SET @fecha=DATEADD(day,1,@fecha); SET @intentos+=1;
END;
IF (SELECT COUNT(*) FROM dbo.Servicios)<4 OR (SELECT COUNT(*) FROM dbo.Productos)<4
 OR (SELECT COUNT(*) FROM dbo.Sucursales)<4 OR (SELECT COUNT(*) FROM dbo.HorariosSucursal)<4
 OR (SELECT COUNT(*) FROM dbo.BloqueosAgenda)<4
 THROW 51004, 'No se alcanzaron los mínimos; revise los datos de los avances anteriores.',1;
COMMIT;
SELECT N'Servicios' Tabla,COUNT(*) Cantidad FROM dbo.Servicios UNION ALL SELECT N'Productos',COUNT(*) FROM dbo.Productos
 UNION ALL SELECT N'Sucursales',COUNT(*) FROM dbo.Sucursales UNION ALL SELECT N'Horarios',COUNT(*) FROM dbo.HorariosSucursal UNION ALL SELECT N'Bloqueos',COUNT(*) FROM dbo.BloqueosAgenda;
SELECT s.Nombre,COUNT(u.UsuarioId) Encargados FROM dbo.Sucursales s LEFT JOIN dbo.Usuarios u ON u.SucursalId=s.SucursalId
 AND u.RolId IN(SELECT RolId FROM dbo.Roles WHERE Nombre=N'Encargado') GROUP BY s.SucursalId,s.Nombre;
SELECT b.BloqueoAgendaId,h.HorarioSucursalId FROM dbo.BloqueosAgenda b JOIN dbo.HorariosSucursal h ON h.SucursalId=b.SucursalId
 AND h.Activo=1 AND h.DiaSemana=((DATEDIFF(day,CONVERT(date,'19000101',112),b.Fecha)%7)+1)
 AND h.FechaVigenciaDesde<=b.Fecha AND (h.FechaVigenciaHasta IS NULL OR h.FechaVigenciaHasta>=b.Fecha)
 WHERE b.DiaCompleto=1 OR (b.HoraInicio<h.HoraFin AND b.HoraFin>h.HoraInicio);
SELECT a.BloqueoAgendaId,b.BloqueoAgendaId FROM dbo.BloqueosAgenda a JOIN dbo.BloqueosAgenda b
 ON a.SucursalId=b.SucursalId AND a.Fecha=b.Fecha AND a.BloqueoAgendaId<b.BloqueoAgendaId
 WHERE a.DiaCompleto=1 OR b.DiaCompleto=1 OR (a.HoraInicio<b.HoraFin AND a.HoraFin>b.HoraInicio);
DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS;

GO
SET XACT_ABORT ON;
SET NOCOUNT ON;
IF DB_NAME()<>N'WashLabsDB' THROW 51000,'Ejecute en WashLabsDB.',1;
BEGIN TRANSACTION;
DECLARE @sede int=(SELECT SucursalId FROM dbo.Sucursales WHERE Nombre=N'WashLabs San Rafael');
DECLARE @rol int=(SELECT RolId FROM dbo.Roles WHERE Nombre=N'Encargado');
IF @sede IS NULL OR @rol IS NULL THROW 51001,'Falta la sucursal o el rol Encargado.',1;
INSERT dbo.Usuarios(RolId,SucursalId,NombreCompleto,Correo,Telefono,Direccion,Activo,PasswordHash)
SELECT @rol,@sede,d.Nombre,d.Correo,N'22224444',N'San Rafael de Alajuela',1,N'!SIN-ACCESO-'+CONVERT(nvarchar(36),NEWID())
FROM (VALUES(N'Encargado de operaciones San Rafael',N'operaciones.sanrafael@washlabs.cr'),
(N'Encargado de atención San Rafael',N'atencion.sanrafael@washlabs.cr'))d(Nombre,Correo)
WHERE NOT EXISTS(SELECT 1 FROM dbo.Usuarios u WITH(UPDLOCK,HOLDLOCK) WHERE u.Correo=d.Correo);
COMMIT;
SELECT s.Nombre,u.NombreCompleto,r.Nombre Rol FROM dbo.Usuarios u JOIN dbo.Sucursales s ON s.SucursalId=u.SucursalId JOIN dbo.Roles r ON r.RolId=u.RolId WHERE s.SucursalId=@sede;
-- Las cuentas nuevas no tienen contraseña utilizable. Configurar acceso local antes de iniciar sesión.



GO
SET NOCOUNT ON;
SELECT a.HorarioSucursalId HorarioA,b.HorarioSucursalId HorarioB
FROM dbo.HorariosSucursal a JOIN dbo.HorariosSucursal b
 ON a.SucursalId=b.SucursalId AND a.DiaSemana=b.DiaSemana AND a.HorarioSucursalId<b.HorarioSucursalId
 AND a.Activo=1 AND b.Activo=1
 AND a.FechaVigenciaDesde<=ISNULL(b.FechaVigenciaHasta,CONVERT(date,'99991231',112))
 AND b.FechaVigenciaDesde<=ISNULL(a.FechaVigenciaHasta,CONVERT(date,'99991231',112))
WHERE a.HoraInicio<b.HoraFin AND a.HoraFin>b.HoraInicio;
SELECT CategoriaProductoId,Nombre FROM dbo.CategoriasProducto;
SELECT u.UsuarioId FROM dbo.Usuarios u LEFT JOIN dbo.Sucursales s ON s.SucursalId=u.SucursalId
WHERE u.SucursalId IS NOT NULL AND s.SucursalId IS NULL;

