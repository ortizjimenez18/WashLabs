SET NOCOUNT ON;
SET XACT_ABORT ON;
IF DB_NAME()<>N'WashLabsDB' THROW 51000,'Ejecute en WashLabsDB.',1;
DECLARE @backup nvarchar(4000);
SELECT TOP (1) @backup=m.physical_device_name
FROM msdb.dbo.backupset b JOIN msdb.dbo.backupmediafamily m ON m.media_set_id=b.media_set_id
WHERE b.database_name=DB_NAME() AND b.type='D' AND b.has_backup_checksums=1
 AND b.backup_finish_date>=DATEADD(hour,-24,SYSDATETIME()) AND b.is_copy_only=1
ORDER BY b.backup_finish_date DESC;
IF @backup IS NULL THROW 51001,'Falta un respaldo FULL COPY_ONLY con CHECKSUM reciente.',1;
RESTORE VERIFYONLY FROM DISK=@backup WITH CHECKSUM;
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
BEGIN TRY
 BEGIN TRANSACTION;
 DECLARE @r int;
 EXEC @r=sys.sp_getapplock @Resource=N'WashLabs:Sucursales:Encargados',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000;
 IF @r<0 THROW 51002,'No se pudieron reservar las asignaciones.',1;
 DECLARE @sucursales TABLE(SucursalId int PRIMARY KEY);
 DECLARE @usuarios TABLE(UsuarioId int PRIMARY KEY);
 INSERT @sucursales SELECT s.SucursalId FROM dbo.Sucursales s
 JOIN (VALUES(1),(2),(3),(4)) n(Numero) ON s.Correo=CONCAT('a4.sebastian.sucursal',n.Numero,'@example.invalid')
 WHERE s.Nombre=CONCAT(N'A4 DEMO Sebastián ',n.Numero);
 INSERT @usuarios SELECT u.UsuarioId FROM dbo.Usuarios u
 JOIN (VALUES(1),(2),(3),(4),(5),(6),(7),(8)) n(Numero) ON u.Correo=CONCAT('a4.sebastian.encargado',n.Numero,'@example.invalid')
 JOIN dbo.Roles r ON r.RolId=u.RolId AND r.Nombre=N'Encargado'
 WHERE u.NombreCompleto=CONCAT(N'A4 DEMO Encargado Sebastián ',n.Numero) AND u.PasswordHash LIKE N'!DEMO-SIN-ACCESO-%';
 DECLARE @id int=(SELECT MIN(SucursalId) FROM @sucursales);
 WHILE @id IS NOT NULL
 BEGIN
  DECLARE @recurso nvarchar(255)=CONCAT(N'WashLabs:Agenda:Sucursal:',@id);
  EXEC @r=sys.sp_getapplock @Resource=@recurso,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000;
  IF @r<0 THROW 51003,'No se pudo reservar la agenda.',1;
  SELECT @id=MIN(SucursalId) FROM @sucursales WHERE SucursalId>@id;
 END;
 IF EXISTS(SELECT 1 FROM dbo.Citas c JOIN @sucursales s ON s.SucursalId=c.SucursalId)
  OR EXISTS(SELECT 1 FROM dbo.Facturas f JOIN @sucursales s ON s.SucursalId=f.SucursalId)
  OR EXISTS(SELECT 1 FROM dbo.SucursalServicios x JOIN @sucursales s ON s.SucursalId=x.SucursalId)
  THROW 51004,'Una sucursal de prueba tiene datos operativos; no se eliminó información.',1;
 IF EXISTS(SELECT 1 FROM dbo.Usuarios u JOIN @sucursales s ON s.SucursalId=u.SucursalId WHERE NOT EXISTS(SELECT 1 FROM @usuarios d WHERE d.UsuarioId=u.UsuarioId))
  OR EXISTS(SELECT 1 FROM dbo.Usuarios u JOIN @usuarios d ON d.UsuarioId=u.UsuarioId WHERE u.SucursalId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM @sucursales s WHERE s.SucursalId=u.SucursalId))
  THROW 51005,'Se detectaron asignaciones ajenas a los datos de prueba; no se eliminó información.',1;
 IF EXISTS(SELECT 1 FROM dbo.HorariosSucursal h JOIN @sucursales s ON s.SucursalId=h.SucursalId
  WHERE h.HoraInicio<>'08:00' OR h.HoraFin<>'10:00' OR h.FechaVigenciaHasta IS NULL OR h.FechaVigenciaDesde<>h.FechaVigenciaHasta)
  OR EXISTS(SELECT 1 FROM dbo.BloqueosAgenda b JOIN @sucursales s ON s.SucursalId=b.SucursalId
  WHERE b.Motivo<>N'A4 DEMO Sebastián: bloqueo de evaluación' OR b.DiaCompleto<>0 OR b.HoraInicio<>'12:00' OR b.HoraFin<>'13:00')
  THROW 51006,'La agenda contiene registros distintos a los precargados; no se eliminó información.',1;
 IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE referenced_object_id IN (OBJECT_ID('dbo.Usuarios'),OBJECT_ID('dbo.Sucursales')) AND delete_referential_action<>0)
  THROW 51007,'La estructura tiene eliminaciones en cascada no previstas.',1;
 DECLARE @bloqueos int,@horarios int,@encargados int,@sedes int;
 DELETE b FROM dbo.BloqueosAgenda b JOIN @sucursales s ON s.SucursalId=b.SucursalId;
 SET @bloqueos=@@ROWCOUNT;
 DELETE h FROM dbo.HorariosSucursal h JOIN @sucursales s ON s.SucursalId=h.SucursalId;
 SET @horarios=@@ROWCOUNT;
 DELETE u FROM dbo.Usuarios u JOIN @usuarios d ON d.UsuarioId=u.UsuarioId;
 SET @encargados=@@ROWCOUNT;
 DELETE s FROM dbo.Sucursales s JOIN @sucursales d ON d.SucursalId=s.SucursalId;
 SET @sedes=@@ROWCOUNT;
 COMMIT;
 SELECT @sedes SucursalesEliminadas,@encargados EncargadosEliminados,@horarios HorariosEliminados,@bloqueos BloqueosEliminados;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK;
 THROW;
END CATCH;
