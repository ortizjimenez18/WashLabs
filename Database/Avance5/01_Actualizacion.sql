-- Ejecutar sobre la base WashLabs que ya tiene aplicado el Avance 4.
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.Facturas', N'U') IS NULL OR OBJECT_ID(N'dbo.Usuarios', N'U') IS NULL
    THROW 51000, N'Selecciona una base de WashLabs antes de ejecutar el Avance 5.', 1;

BEGIN TRANSACTION;

IF COL_LENGTH(N'dbo.Facturas', N'TipoCambioVenta') IS NULL
    ALTER TABLE dbo.Facturas ADD TipoCambioVenta decimal(18,6) NULL;
IF COL_LENGTH(N'dbo.Facturas', N'FechaTipoCambioVenta') IS NULL
    ALTER TABLE dbo.Facturas ADD FechaTipoCambioVenta date NULL;
IF COL_LENGTH(N'dbo.Facturas', N'EmitidaPorUsuarioId') IS NULL
    ALTER TABLE dbo.Facturas ADD EmitidaPorUsuarioId int NULL;
IF COL_LENGTH(N'dbo.Facturas', N'SolicitudEmisionId') IS NULL
    ALTER TABLE dbo.Facturas ADD SolicitudEmisionId uniqueidentifier NULL;

IF OBJECT_ID(N'dbo.CK_Facturas_TipoCambioVenta', N'C') IS NULL
    EXEC(N'ALTER TABLE dbo.Facturas WITH CHECK ADD CONSTRAINT CK_Facturas_TipoCambioVenta CHECK (
        (TipoCambioVenta IS NULL AND FechaTipoCambioVenta IS NULL)
        OR (TipoCambioVenta IS NOT NULL AND TipoCambioVenta > 0 AND FechaTipoCambioVenta IS NOT NULL)
    );');

IF OBJECT_ID(N'dbo.FK_Facturas_EmitidaPor', N'F') IS NULL
    EXEC(N'ALTER TABLE dbo.Facturas WITH CHECK ADD CONSTRAINT FK_Facturas_EmitidaPor
        FOREIGN KEY (EmitidaPorUsuarioId) REFERENCES dbo.Usuarios(UsuarioId);');

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Facturas') AND name = N'UX_Facturas_SolicitudEmision')
    EXEC(N'CREATE UNIQUE INDEX UX_Facturas_SolicitudEmision ON dbo.Facturas (EmitidaPorUsuarioId, SolicitudEmisionId)
        WHERE SolicitudEmisionId IS NOT NULL;');

DECLARE @PreguntasFaltantes int = 3 - (SELECT COUNT(*) FROM dbo.PreguntasReserva WHERE Activa = 1);
IF @PreguntasFaltantes > 0
BEGIN
    INSERT dbo.PreguntasReserva (Pregunta, TipoRespuesta, Obligatoria, Orden, Activa)
    SELECT TOP (@PreguntasFaltantes) p.Texto, 'TEXTO', 1, p.Orden, 1
    FROM (VALUES
        (N'¿El vehículo presenta suciedad excesiva o requiere una limpieza especial?', 20),
        (N'¿Qué accesorios o zonas del vehículo requieren un cuidado especial?', 21),
        (N'¿Tiene alguna indicación que debamos seguir durante el lavado?', 22)
    ) p(Texto, Orden)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.PreguntasReserva existente WHERE existente.Pregunta = p.Texto AND existente.Activa = 1)
    ORDER BY p.Orden;
END;

COMMIT TRANSACTION;
