-- Nombres ficticios para las cuentas existentes. Conserva los datos de acceso.
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

UPDATE u
SET NombreCompleto = n.Nombre,
    FechaActualizacion = SYSDATETIME()
FROM dbo.Usuarios AS u
INNER JOIN (VALUES
    (5, N'Daniel Vargas Solano'),
    (6, N'Andrés Navarro Rojas'),
    (7, N'Valeria Castro Méndez'),
    (8, N'Esteban Salazar Mora'),
    (9, N'Camila Jiménez Araya'),
    (10, N'Diego Ramírez Vega'),
    (11, N'Lucía Fernández Soto'),
    (12, N'Gabriela Campos Herrera'),
    (13, N'Mateo Rojas Delgado'),
    (25, N'Sofía Aguilar Brenes'),
    (26, N'Nicolás Cordero Fallas')
) AS n(UsuarioId, Nombre) ON n.UsuarioId = u.UsuarioId;

IF @@ROWCOUNT <> 11
    THROW 51000, N'No se encontraron los 11 usuarios esperados.', 1;

COMMIT TRANSACTION;

SELECT UsuarioId, NombreCompleto, RolId, Activo
FROM dbo.Usuarios
ORDER BY UsuarioId;
