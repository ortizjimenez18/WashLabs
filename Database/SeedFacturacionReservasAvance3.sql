-- Verificación de los datos existentes de facturación y reservas.
-- No inserta sucursales, clientes, contactos, vehículos, citas ni facturas.
USE WashLabsDB;
SET NOCOUNT ON;
SELECT s.SucursalId,s.Nombre,COUNT(c.CitaId) Citas
FROM Sucursales s LEFT JOIN Citas c ON c.SucursalId=s.SucursalId
    AND EXISTS(SELECT 1 FROM Usuarios u WHERE u.UsuarioId=c.ClienteId AND u.Activo=1)
    AND EXISTS(SELECT 1 FROM Vehiculos v WHERE v.VehiculoId=c.VehiculoId AND v.Activo=1)
WHERE s.Activa=1 GROUP BY s.SucursalId,s.Nombre;
SELECT COUNT(*) FacturasVisibles FROM Facturas f JOIN Usuarios u ON u.UsuarioId=f.ClienteId AND u.Activo=1
JOIN Sucursales s ON s.SucursalId=f.SucursalId AND s.Activa=1 WHERE f.TipoDocumento='FACTURA';
SELECT c.CitaId,COUNT(r.RespuestaReservaId) Respuestas FROM Citas c
JOIN Usuarios u ON u.UsuarioId=c.ClienteId AND u.Activo=1
JOIN Vehiculos v ON v.VehiculoId=c.VehiculoId AND v.Activo=1
LEFT JOIN RespuestasReserva r ON r.CitaId=c.CitaId GROUP BY c.CitaId;
