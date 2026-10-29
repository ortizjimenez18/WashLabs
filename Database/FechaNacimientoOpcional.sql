-- Las cuentas de acceso pueden no tener fecha de nacimiento registrada.
USE WashLabsDB;
IF EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('dbo.Usuarios') AND name='FechaNacimiento' AND is_nullable=0)
    ALTER TABLE dbo.Usuarios ALTER COLUMN FechaNacimiento date NULL;
