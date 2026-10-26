/* Datos DEMO identificados; SQL Server 2019, idempotente.
   No altera cuentas existentes, contraseñas, roles ni sucursales de otros usuarios.
   Las cuentas nuevas no tienen una contraseña utilizable. Si hace falta iniciar
   sesión con ellas, usar el configurador interactivo existente con autorización.
   Las fechas se fijan al instalar los datos por primera vez; repetir no las mueve.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF DB_NAME()<>N'WashLabsDB' THROW 51000,'Ejecute en WashLabsDB.',1;
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name='CK_Usuarios_EncargadoSucursal')
 THROW 51001,'Ejecute primero 01_Esquema.sql.',1;
DECLARE @backup nvarchar(4000);
SELECT TOP (1) @backup=m.physical_device_name
FROM msdb.dbo.backupset b JOIN msdb.dbo.backupmediafamily m ON m.media_set_id=b.media_set_id
WHERE b.database_name=DB_NAME() AND b.type='D' AND b.has_backup_checksums=1
 AND b.backup_finish_date>=DATEADD(hour,-24,SYSDATETIME()) AND b.is_copy_only=1
ORDER BY b.backup_finish_date DESC;
IF @backup IS NULL THROW 51002,'Falta respaldo FULL COPY_ONLY con CHECKSUM reciente.',1;
RESTORE VERIFYONLY FROM DISK=@backup WITH CHECKSUM;
DECLARE @rol tinyint=(SELECT RolId FROM dbo.Roles WHERE Nombre=N'Encargado');
DECLARE @actor int=(SELECT UsuarioId FROM dbo.Usuarios u JOIN dbo.Roles r ON r.RolId=u.RolId WHERE r.Nombre=N'Administrador' AND u.Activo=1);
IF @rol IS NULL OR @actor IS NULL THROW 51003,'Faltan rol Encargado o administrador activo.',1;
DECLARE @fecha date=DATEADD(day,14,CONVERT(date,SYSUTCDATETIME() AT TIME ZONE 'UTC' AT TIME ZONE 'Central America Standard Time'));
BEGIN TRANSACTION;
DECLARE @r int;
EXEC @r=sys.sp_getapplock @Resource=N'WashLabs:Sucursales:Encargados',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000;
IF @r<0 THROW 51004,'No se pudo reservar la instalación DEMO.',1;
DECLARE @i int=1;
WHILE @i<=4
BEGIN
 DECLARE @nombre nvarchar(100)=CONCAT(N'A4 DEMO Sebastián ',@i);
 DECLARE @correo varchar(150)=CONCAT('a4.sebastian.sucursal',@i,'@example.invalid');
 IF NOT EXISTS(SELECT 1 FROM dbo.Sucursales WHERE Correo=@correo)
  INSERT dbo.Sucursales(Nombre,Descripcion,Telefono,Direccion,Correo,Activa)
  VALUES(@nombre,N'Sucursal de demostración del Avance 4; datos de evaluación.','2222-3333',N'Dirección de evaluación del módulo de sucursales.',@correo,1);
 DECLARE @sucursal int=(SELECT SucursalId FROM dbo.Sucursales WHERE Correo=@correo);
 DECLARE @recurso nvarchar(255)=CONCAT(N'WashLabs:Agenda:Sucursal:',@sucursal);
 EXEC @r=sys.sp_getapplock @Resource=@recurso,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000;
 IF @r<0 THROW 51005,'No se pudo reservar la agenda DEMO.',1;
 -- Solo agrega la agenda inicial a sucursales DEMO sin registros de este tipo.
 IF NOT EXISTS(SELECT 1 FROM dbo.HorariosSucursal WHERE SucursalId=@sucursal)
  INSERT dbo.HorariosSucursal(SucursalId,DiaSemana,HoraInicio,HoraFin,FechaVigenciaDesde,FechaVigenciaHasta,Activo)
  VALUES(@sucursal,CONVERT(tinyint,(DATEDIFF(day,CONVERT(date,'19000101'),@fecha)%7)+1),'08:00','10:00',@fecha,@fecha,1);
 DECLARE @fechaSucursal date=(SELECT MIN(FechaVigenciaDesde) FROM dbo.HorariosSucursal WHERE SucursalId=@sucursal);
 IF NOT EXISTS(SELECT 1 FROM dbo.BloqueosAgenda WHERE SucursalId=@sucursal)
  INSERT dbo.BloqueosAgenda(SucursalId,Fecha,DiaCompleto,HoraInicio,HoraFin,Motivo,CreadoPorUsuarioId)
  VALUES(@sucursal,@fechaSucursal,0,'12:00','13:00',N'A4 DEMO Sebastián: bloqueo de evaluación',@actor);
 SET @i+=1;
END;
SET @i=1;
WHILE @i<=8
BEGIN
 DECLARE @correoUsuario varchar(150)=CONCAT('a4.sebastian.encargado',@i,'@example.invalid');
 -- 1 y 2 => sucursal 1; 3 y 4 => sucursal 2; 5 => 3; 6 => 4; 7 y 8 sin asignación.
 DECLARE @numero int=CASE WHEN @i<=4 THEN (@i+1)/2 WHEN @i<=6 THEN @i-2 ELSE NULL END;
 DECLARE @asignada int=(SELECT SucursalId FROM dbo.Sucursales WHERE Correo=CONCAT('a4.sebastian.sucursal',@numero,'@example.invalid') AND @numero IS NOT NULL);
 IF NOT EXISTS(SELECT 1 FROM dbo.Usuarios WHERE Correo=@correoUsuario)
  INSERT dbo.Usuarios(RolId,SucursalId,NombreCompleto,Telefono,Correo,Direccion,PasswordHash,Activo)
  VALUES(@rol,@asignada,CONCAT(N'A4 DEMO Encargado Sebastián ',@i),'22223333',@correoUsuario,N'Datos de evaluación.',
    CONCAT(N'!DEMO-SIN-ACCESO-',CONVERT(varchar(128),CRYPT_GEN_RANDOM(64),2)),1);
 SET @i+=1;
END;
COMMIT;
SELECT SucursalId,Nombre,Correo FROM dbo.Sucursales WHERE Correo LIKE 'a4.sebastian.sucursal%@example.invalid';
SELECT UsuarioId,NombreCompleto,SucursalId FROM dbo.Usuarios WHERE Correo LIKE 'a4.sebastian.encargado%@example.invalid';
