-- Datos sintéticos: completar la lista solo con usuarios DEMO aprobados.
-- No cambia contraseñas, roles, estado activo ni identificaciones existentes.
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF DB_NAME() <> N'WashLabsDB' AND DB_NAME() NOT LIKE N'WashLabs[_]Pruebas[_]Avance3%'
    THROW 51000, N'Base de destino no permitida.', 1;
DECLARE @UsuariosDemo TABLE (UsuarioId int PRIMARY KEY);
-- INSERT INTO @UsuariosDemo (UsuarioId) VALUES (ID_DEMO_APROBADO);
IF NOT EXISTS (SELECT 1 FROM @UsuariosDemo)
    THROW 51001, N'Complete y apruebe la lista explícita de usuarios de demostración.', 1;
IF EXISTS (SELECT 1 FROM @UsuariosDemo d WHERE NOT EXISTS (SELECT 1 FROM dbo.Usuarios u WHERE u.UsuarioId = d.UsuarioId))
    THROW 51002, N'La lista contiene un usuario inexistente.', 1;
IF COL_LENGTH(N'dbo.Usuarios', N'Identificacion') IS NULL
    THROW 51003, N'Falta Identificacion; este script no modifica el esquema.', 1;
BEGIN TRY
    BEGIN TRANSACTION;
    IF EXISTS (
        SELECT 1 FROM @UsuariosDemo d JOIN dbo.Usuarios u WITH (UPDLOCK, HOLDLOCK)
            ON u.Identificacion = CONCAT('DEMO-A3-', d.UsuarioId) AND u.UsuarioId <> d.UsuarioId)
        THROW 51004, N'Conflicto con una identificación sintética.', 1;
    UPDATE u SET Identificacion = CONCAT('DEMO-A3-', u.UsuarioId), FechaActualizacion = SYSDATETIME()
    FROM dbo.Usuarios u WITH (UPDLOCK, HOLDLOCK) JOIN @UsuariosDemo d ON d.UsuarioId = u.UsuarioId
    WHERE NULLIF(LTRIM(RTRIM(u.Identificacion)), '') IS NULL;
    SELECT @@ROWCOUNT IdentificacionesDemoCompletadas;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
