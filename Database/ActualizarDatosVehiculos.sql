-- Sustituye los datos de demostración y conserva los dos vehículos reales.
USE [WashLabsDB];
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

UPDATE v
SET Matricula = n.Matricula,
    Marca = n.Marca,
    Modelo = n.Modelo,
    Anio = n.Anio,
    Color = n.Color
FROM dbo.Vehiculos AS v
INNER JOIN (VALUES
    (2, 'BKL-482', N'Hyundai', N'Accent', 2018, N'Gris'),
    (3, 'CFM-615', N'Nissan', N'Versa', 2020, N'Negro'),
    (4, 'BVR-294', N'Kia', N'Rio', 2019, N'Blanco'),
    (5, 'DGS-738', N'Suzuki', N'Swift', 2022, N'Gris')
) AS n(VehiculoId, Matricula, Marca, Modelo, Anio, Color)
    ON n.VehiculoId = v.VehiculoId;

IF @@ROWCOUNT <> 4
    THROW 51000, N'No se encontraron los cuatro vehículos esperados.', 1;

COMMIT TRANSACTION;

SELECT VehiculoId, ClienteId, Matricula, Marca, Modelo, Anio, Color, Activo
FROM dbo.Vehiculos
ORDER BY VehiculoId;
