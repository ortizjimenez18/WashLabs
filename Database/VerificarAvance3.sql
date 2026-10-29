-- Solo lectura; no muestra nombres, correos, identificaciones ni hashes.
SET NOCOUNT ON;
SELECT compatibility_level FROM sys.databases WHERE name = DB_NAME();
SELECT COUNT(*) Entidades FROM sys.tables WHERE name <> 'sysdiagrams';
SELECT COUNT(*) ClavesForaneas FROM sys.foreign_keys;
SELECT name AS TriggerExistente FROM sys.triggers WHERE parent_class = 1;
SELECT 'Sucursales' Tipo, COUNT(*) Cantidad FROM dbo.Sucursales
UNION ALL SELECT 'Productos', COUNT(*) FROM dbo.Productos
UNION ALL SELECT 'Citas', COUNT(*) FROM dbo.Citas
UNION ALL SELECT 'Documentos', COUNT(*) FROM dbo.Facturas
UNION ALL SELECT 'FACTURA', COUNT(*) FROM dbo.Facturas WHERE TipoDocumento = 'FACTURA'
UNION ALL SELECT 'Vehiculos', COUNT(*) FROM dbo.Vehiculos
UNION ALL SELECT 'Tarjetas', COUNT(*) FROM dbo.TarjetasWashPoints
UNION ALL SELECT 'Movimientos', COUNT(*) FROM dbo.MovimientosWashPoints;
SELECT s.SucursalId, COUNT(c.CitaId) Citas
FROM dbo.Sucursales s LEFT JOIN dbo.Citas c ON c.SucursalId = s.SucursalId
WHERE s.Activa = 1 GROUP BY s.SucursalId;
SELECT r.Nombre Rol, u.Activo, u.SucursalId, COUNT(*) Usuarios,
    SUM(CASE WHEN u.PasswordHash LIKE 'AQAAAA%' THEN 1 ELSE 0 END) FormatoIdentity,
    SUM(CASE WHEN NULLIF(LTRIM(RTRIM(u.Identificacion)), '') IS NULL THEN 1 ELSE 0 END) SinIdentificacion
FROM dbo.Usuarios u JOIN dbo.Roles r ON r.RolId = u.RolId
GROUP BY r.Nombre, u.Activo, u.SucursalId;
SELECT name, is_unique, filter_definition FROM sys.indexes
WHERE object_id IN (OBJECT_ID('dbo.MovimientosWashPoints'), OBJECT_ID('dbo.TarjetasWashPoints'));
SELECT MetaLavados, Activa FROM dbo.ConfiguracionWashPoints;
SELECT COUNT(*) FacturasConTotalesInconsistentes FROM dbo.Facturas f
WHERE f.TipoDocumento = 'FACTURA' AND (
    f.Subtotal <> (SELECT COALESCE(SUM(d.Subtotal), 0) FROM dbo.DetallesFactura d WHERE d.FacturaId = f.FacturaId)
    OR f.Impuesto <> (SELECT COALESCE(SUM(d.Impuesto), 0) FROM dbo.DetallesFactura d WHERE d.FacturaId = f.FacturaId)
    OR f.Total <> f.Subtotal + f.Impuesto);
SELECT COUNT(*) CitasConAcumulacionesDuplicadas FROM (
    SELECT CitaId FROM dbo.MovimientosWashPoints WHERE CitaId IS NOT NULL AND TipoMovimiento = 'ACUMULA'
    GROUP BY CitaId HAVING COUNT(*) > 1) duplicadas;
