SET NOCOUNT ON;

SELECT N'Clientes activos' AS Entidad, COUNT(*) AS Registros
FROM dbo.Usuarios u JOIN dbo.Roles r ON r.RolId = u.RolId WHERE r.Nombre = N'Cliente' AND u.Activo = 1
UNION ALL SELECT N'Encargados activos', COUNT(*) FROM dbo.Usuarios u JOIN dbo.Roles r ON r.RolId = u.RolId WHERE r.Nombre = N'Encargado' AND u.Activo = 1
UNION ALL SELECT N'Sucursales activas', COUNT(*) FROM dbo.Sucursales WHERE Activa = 1
UNION ALL SELECT N'Servicios activos', COUNT(*) FROM dbo.Servicios WHERE Activo = 1
UNION ALL SELECT N'Productos activos', COUNT(*) FROM dbo.Productos WHERE Activo = 1
UNION ALL SELECT N'Vehículos activos', COUNT(*) FROM dbo.Vehiculos WHERE Activo = 1
UNION ALL SELECT N'Preguntas activas', COUNT(*) FROM dbo.PreguntasReserva WHERE Activa = 1
UNION ALL SELECT N'Citas', COUNT(*) FROM dbo.Citas
UNION ALL SELECT N'Proformas', COUNT(*) FROM dbo.Facturas WHERE TipoDocumento = 'PROFORMA'
UNION ALL SELECT N'Facturas', COUNT(*) FROM dbo.Facturas WHERE TipoDocumento = 'FACTURA'
UNION ALL SELECT N'Detalles de factura', COUNT(*) FROM dbo.DetallesFactura
UNION ALL SELECT N'Tarjetas WashPoints', COUNT(*) FROM dbo.TarjetasWashPoints
UNION ALL SELECT N'Movimientos WashPoints', COUNT(*) FROM dbo.MovimientosWashPoints;
