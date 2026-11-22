-- Opcional. Ejecutar en una base de demostración con el Avance 4 precargado.
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
BEGIN TRANSACTION;

DECLARE @RolCliente tinyint = (SELECT RolId FROM dbo.Roles WHERE Nombre = N'Cliente');
IF @RolCliente IS NULL THROW 51001, N'El rol Cliente debe existir.', 1;

-- Las cuentas de muestra no tienen contraseña utilizable. Se pueden seleccionar como clientes.
DECLARE @ClientesFaltantes int = 4 - (SELECT COUNT(*) FROM dbo.Usuarios WHERE RolId = @RolCliente AND Activo = 1);
IF @ClientesFaltantes > 0
INSERT dbo.Usuarios (RolId, NombreCompleto, Correo, Telefono, Direccion, PasswordHash, Activo)
SELECT TOP (@ClientesFaltantes) @RolCliente, muestra.Nombre, muestra.Correo, muestra.Telefono, N'Dirección de demostración', N'ACCESO_NO_CONFIGURADO', 1
FROM (VALUES
    (N'Mariana Solano Demo', N'mariana.solano.avance5@example.invalid', N'8888-0105'),
    (N'Daniel Vargas Demo', N'daniel.vargas.avance5@example.invalid', N'8888-0106'),
    (N'Elena Mora Demo', N'elena.mora.avance5@example.invalid', N'8888-0107'),
    (N'Luis Rojas Demo', N'luis.rojas.avance5@example.invalid', N'8888-0108')
) muestra(Nombre, Correo, Telefono)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Usuarios u WHERE u.Correo = muestra.Correo)
ORDER BY muestra.Correo;

IF (SELECT COUNT(*) FROM dbo.PreguntasReserva WHERE Activa = 1) = 3
BEGIN
    INSERT dbo.PreguntasReserva (Pregunta, TipoRespuesta, Obligatoria, Orden, Activa)
    SELECT N'¿Hay objetos personales que deban permanecer en el vehículo?', 'TEXTO', 1, 30, 1
    WHERE NOT EXISTS (SELECT 1 FROM dbo.PreguntasReserva WHERE Pregunta = N'¿Hay objetos personales que deban permanecer en el vehículo?');
END;

-- Inicializa tarjetas con sus valores predeterminados, sin otorgar lavados.
DECLARE @TarjetasFaltantes int = 4 - (SELECT COUNT(*) FROM dbo.TarjetasWashPoints);
IF @TarjetasFaltantes > 0
    INSERT dbo.TarjetasWashPoints (VehiculoId)
    SELECT TOP (@TarjetasFaltantes) v.VehiculoId
    FROM dbo.Vehiculos v
    WHERE v.Activo = 1 AND NOT EXISTS (SELECT 1 FROM dbo.TarjetasWashPoints t WHERE t.VehiculoId = v.VehiculoId)
    ORDER BY v.VehiculoId;

COMMIT TRANSACTION;
