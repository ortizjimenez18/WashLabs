-- SQL Server 2019. Solo lectura: ejecutar en la base seleccionada en SSMS.
SET NOCOUNT ON;

SELECT EstadoFacturaId, Nombre FROM dbo.EstadosFactura;
SELECT i.name, i.is_unique, i.filter_definition
FROM sys.indexes i
WHERE i.object_id IN (OBJECT_ID('dbo.Facturas'), OBJECT_ID('dbo.MovimientosWashPoints'), OBJECT_ID('dbo.TarjetasWashPoints'));
SELECT c.name, c.definition, c.is_persisted
FROM sys.computed_columns c WHERE c.object_id = OBJECT_ID('dbo.DetallesFactura');
SELECT name, definition FROM sys.check_constraints
WHERE parent_object_id IN (OBJECT_ID('dbo.Facturas'), OBJECT_ID('dbo.DetallesFactura'));
SELECT name FROM sys.triggers WHERE parent_class = 1;

SELECT CitaId, COUNT(*) Documentos FROM dbo.Facturas
WHERE CitaId IS NOT NULL GROUP BY CitaId HAVING COUNT(*) > 1;
SELECT CitaId, COUNT(*) Acumulaciones FROM dbo.MovimientosWashPoints
WHERE TipoMovimiento = 'ACUMULA' AND CitaId IS NOT NULL GROUP BY CitaId HAVING COUNT(*) > 1;
SELECT f.FacturaId, f.CitaId FROM dbo.Facturas f JOIN dbo.Citas c ON c.CitaId = f.CitaId
WHERE f.ClienteId <> c.ClienteId OR f.SucursalId <> c.SucursalId;
SELECT f.FacturaId, f.Subtotal, f.Impuesto, f.Total, d.Subtotal LineasSubtotal, d.Impuesto LineasImpuesto
FROM dbo.Facturas f
CROSS APPLY (SELECT COALESCE(SUM(Subtotal),0) Subtotal, COALESCE(SUM(Impuesto),0) Impuesto
             FROM dbo.DetallesFactura WHERE FacturaId = f.FacturaId) d
WHERE f.Subtotal <> d.Subtotal OR f.Impuesto <> d.Impuesto OR f.Total <> f.Subtotal + f.Impuesto;
