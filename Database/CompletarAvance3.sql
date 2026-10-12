-- Actualización de esquema. No crea clientes, sucursales, vehículos, citas ni lavados.
USE WashLabsDB;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.Usuarios','Identificacion') IS NULL
    ALTER TABLE dbo.Usuarios ADD Identificacion varchar(30) NULL;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Usuarios') AND name='UX_Usuarios_Identificacion')
    EXEC(N'CREATE UNIQUE INDEX UX_Usuarios_Identificacion ON dbo.Usuarios(Identificacion) WHERE Identificacion IS NOT NULL;');
COMMIT;
