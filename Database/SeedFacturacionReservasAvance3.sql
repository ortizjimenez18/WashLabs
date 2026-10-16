-- Verificación de los datos existentes de facturación y reservas.
-- No inserta sucursales, clientes, contactos, vehículos, citas ni facturas.
USE WashLabsDB;
SET NOCOUNT ON;
SELECT s.SucursalId,s.Nombre,COUNT(c.CitaId) Citas
FROM Sucursales s LEFT JOIN Citas c ON c.SucursalId=s.SucursalId
WHERE s.Activa=1 GROUP BY s.SucursalId,s.Nombre;
SELECT COUNT(*) FacturasVisiblesAdministrador FROM Facturas f WHERE f.TipoDocumento='FACTURA';
SELECT c.CitaId,COUNT(r.RespuestaReservaId) Respuestas FROM Citas c
LEFT JOIN RespuestasReserva r ON r.CitaId=c.CitaId GROUP BY c.CitaId;
