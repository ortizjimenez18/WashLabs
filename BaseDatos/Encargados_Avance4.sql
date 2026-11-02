SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
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

