-- UTN · María Paula Murillo Rosales · Actividad de comprobación · Avance 3
USE [WashLabsDB];
GO
SET NOCOUNT ON;

SELECT COUNT_BIG(*) AS TotalUsuariosRegistrados FROM dbo.Usuarios;

SELECT u.UsuarioId AS [Id Usuario], u.NombreCompleto AS [Nombre del Usuario],
       r.Nombre AS [Nombre del Rol Asociado], u.Correo AS [Correo Electrónico],
       CASE WHEN u.Activo = 1 THEN N'Activo' ELSE N'Inactivo' END AS [Estado del Usuario]
FROM dbo.Usuarios AS u
INNER JOIN dbo.Roles AS r ON r.RolId = u.RolId
ORDER BY u.UsuarioId;

-- No se insertan cuentas si ya existen al menos tres (9 al implementar).
-- Las contraseñas requieren PasswordHasher<Usuario> de CuentaService.
-- Si faltan usuarios, registrar únicamente los faltantes desde /Cuenta/Registrarse;
-- ese flujo valida duplicados, asigna Cliente y genera el hash de Identity.
-- No copiar hashes ni insertar contraseñas desde SQL ni modificar cuentas actuales.
IF (SELECT COUNT_BIG(*) FROM dbo.Usuarios) < 3
BEGIN
    ;THROW 51001, N'Faltan usuarios: registre únicamente los necesarios mediante /Cuenta/Registrarse y vuelva a ejecutar este script.', 1;
END;

SELECT u.UsuarioId, u.NombreCompleto, u.RolId, r.Nombre AS RolAsociado
FROM dbo.Usuarios AS u LEFT JOIN dbo.Roles AS r ON r.RolId = u.RolId
ORDER BY u.UsuarioId;

IF EXISTS (SELECT 1 FROM dbo.Usuarios AS u LEFT JOIN dbo.Roles AS r
           ON r.RolId = u.RolId WHERE r.RolId IS NULL)
BEGIN
    ;THROW 51002, N'Existen usuarios sin un rol válido asociado; revisar los registros.', 1;
END;
PRINT N'Validación correcta: al menos tres usuarios y todos con rol válido. No se modificaron datos.';
GO
