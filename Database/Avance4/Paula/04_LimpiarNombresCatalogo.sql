-- Ejecutar manualmente en la base de WashLabs. Idempotente: solo cambia Nombre.
-- No modifica descripciones históricas de facturas, IDs, relaciones ni importes.
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

BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @prefijo nvarchar(20) = N'A4 Paula - ';
    -- LEN excluye el espacio final; DATALENGTH conserva la longitud exacta.
    DECLARE @longitud int = DATALENGTH(@prefijo) / 2;

    UPDATE dbo.Servicios
    SET Nombre = STUFF(Nombre, 1, @longitud, N'')
    OUTPUT inserted.ServicioId, deleted.Nombre AS NombreAnterior, inserted.Nombre AS NombreNuevo
    WHERE DATALENGTH(Nombre) >= DATALENGTH(@prefijo)
      AND LEFT(Nombre, @longitud) COLLATE Latin1_General_100_BIN2 = @prefijo COLLATE Latin1_General_100_BIN2;

    UPDATE dbo.Productos
    SET Nombre = STUFF(Nombre, 1, @longitud, N'')
    OUTPUT inserted.ProductoId, deleted.Nombre AS NombreAnterior, inserted.Nombre AS NombreNuevo
    WHERE DATALENGTH(Nombre) >= DATALENGTH(@prefijo)
      AND LEFT(Nombre, @longitud) COLLATE Latin1_General_100_BIN2 = @prefijo COLLATE Latin1_General_100_BIN2;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
