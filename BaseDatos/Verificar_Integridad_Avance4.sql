SET NOCOUNT ON;
SELECT a.HorarioSucursalId HorarioA,b.HorarioSucursalId HorarioB
FROM dbo.HorariosSucursal a JOIN dbo.HorariosSucursal b
 ON a.SucursalId=b.SucursalId AND a.DiaSemana=b.DiaSemana AND a.HorarioSucursalId<b.HorarioSucursalId
 AND a.Activo=1 AND b.Activo=1
 AND a.FechaVigenciaDesde<=ISNULL(b.FechaVigenciaHasta,CONVERT(date,'99991231',112))
 AND b.FechaVigenciaDesde<=ISNULL(a.FechaVigenciaHasta,CONVERT(date,'99991231',112))
WHERE a.HoraInicio<b.HoraFin AND a.HoraFin>b.HoraInicio;
SELECT CategoriaProductoId,Nombre FROM dbo.CategoriasProducto;
SELECT u.UsuarioId FROM dbo.Usuarios u LEFT JOIN dbo.Sucursales s ON s.SucursalId=u.SucursalId
WHERE u.SucursalId IS NOT NULL AND s.SucursalId IS NULL;
