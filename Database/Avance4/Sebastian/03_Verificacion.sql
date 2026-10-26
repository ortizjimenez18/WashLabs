/* Solo lectura. SQL Server 2019. Ejecutar en WashLabsDB después de 01 y 02.
   Los resultados de anomalías deben tener cero filas para datos DEMO.
   La consulta global también permite distinguir conflictos históricos del Avance 3.
*/
SET NOCOUNT ON;
IF DB_NAME()<>N'WashLabsDB' THROW 51000,'Ejecute en WashLabsDB.',1;
SELECT t.name Tabla,c.name Columna,ty.name Tipo,c.max_length,c.is_nullable
FROM sys.tables t JOIN sys.columns c ON c.object_id=t.object_id JOIN sys.types ty ON ty.user_type_id=c.user_type_id
WHERE t.name IN ('Sucursales','Usuarios','HorariosSucursal','BloqueosAgenda') ORDER BY t.name,c.column_id;
SELECT name,definition,is_disabled,is_not_trusted FROM sys.check_constraints
WHERE parent_object_id IN (OBJECT_ID('dbo.Usuarios'),OBJECT_ID('dbo.HorariosSucursal'),OBJECT_ID('dbo.BloqueosAgenda'));
SELECT name,OBJECT_NAME(parent_object_id) Tabla,OBJECT_NAME(referenced_object_id) Referencia,is_disabled,is_not_trusted
FROM sys.foreign_keys WHERE parent_object_id IN (OBJECT_ID('dbo.Usuarios'),OBJECT_ID('dbo.HorariosSucursal'),OBJECT_ID('dbo.BloqueosAgenda'));
SELECT s.SucursalId,s.Nombre,s.Descripcion,s.Telefono,s.Direccion,s.Correo,s.Activa,COUNT(u.UsuarioId) Encargados
FROM dbo.Sucursales s LEFT JOIN dbo.Usuarios u ON u.SucursalId=s.SucursalId AND u.RolId=(SELECT RolId FROM dbo.Roles WHERE Nombre=N'Encargado')
GROUP BY s.SucursalId,s.Nombre,s.Descripcion,s.Telefono,s.Direccion,s.Correo,s.Activa ORDER BY s.Nombre;
SELECT u.UsuarioId,u.NombreCompleto,r.Nombre Rol,u.Activo,u.SucursalId,s.Nombre Sucursal
FROM dbo.Usuarios u JOIN dbo.Roles r ON r.RolId=u.RolId LEFT JOIN dbo.Sucursales s ON s.SucursalId=u.SucursalId
WHERE r.Nombre=N'Encargado' ORDER BY u.UsuarioId;
SELECT h.*,s.Nombre Sucursal,CASE WHEN h.FechaVigenciaDesde=h.FechaVigenciaHasta THEN N'Fecha específica' ELSE N'Recurrente' END Tipo
FROM dbo.HorariosSucursal h JOIN dbo.Sucursales s ON s.SucursalId=h.SucursalId ORDER BY h.SucursalId,h.FechaVigenciaDesde,h.DiaSemana;
SELECT b.BloqueoAgendaId,b.SucursalId,s.Nombre Sucursal,b.Fecha,b.DiaCompleto,b.HoraInicio,b.HoraFin,b.Motivo,u.NombreCompleto RegistradoPor
FROM dbo.BloqueosAgenda b JOIN dbo.Sucursales s ON s.SucursalId=b.SucursalId JOIN dbo.Usuarios u ON u.UsuarioId=b.CreadoPorUsuarioId ORDER BY b.SucursalId,b.Fecha;
-- Debe arrojar 4 sucursales, 8 encargados, 4 horarios y 4 bloqueos tras instalación inicial.
SELECT (SELECT COUNT(*) FROM dbo.Sucursales WHERE Correo LIKE 'a4.sebastian.sucursal%@example.invalid') SucursalesDemo,
 (SELECT COUNT(*) FROM dbo.Usuarios WHERE Correo LIKE 'a4.sebastian.encargado%@example.invalid') EncargadosDemo,
 (SELECT COUNT(*) FROM dbo.HorariosSucursal h JOIN dbo.Sucursales s ON s.SucursalId=h.SucursalId WHERE s.Correo LIKE 'a4.sebastian.sucursal%@example.invalid') HorariosDemo,
 (SELECT COUNT(*) FROM dbo.BloqueosAgenda b JOIN dbo.Sucursales s ON s.SucursalId=b.SucursalId WHERE s.Correo LIKE 'a4.sebastian.sucursal%@example.invalid') BloqueosDemo;
-- Asignaciones huérfanas: cero filas. Un usuario tiene una sola FK, no una tabla puente.
SELECT u.UsuarioId,u.SucursalId FROM dbo.Usuarios u LEFT JOIN dbo.Sucursales s ON s.SucursalId=u.SucursalId WHERE u.SucursalId IS NOT NULL AND s.SucursalId IS NULL;
-- Conflictos horario-horario, contemplando vigencias y día semanal. Cero filas en DEMO.
SELECT a.SucursalId,a.HorarioSucursalId HorarioA,b.HorarioSucursalId HorarioB
FROM dbo.HorariosSucursal a JOIN dbo.HorariosSucursal b ON a.SucursalId=b.SucursalId AND a.HorarioSucursalId<b.HorarioSucursalId AND a.DiaSemana=b.DiaSemana
WHERE a.Activo=1 AND b.Activo=1 AND a.HoraInicio<b.HoraFin AND b.HoraInicio<a.HoraFin
 AND a.FechaVigenciaDesde<=ISNULL(b.FechaVigenciaHasta,CONVERT(date,'99991231')) AND b.FechaVigenciaDesde<=ISNULL(a.FechaVigenciaHasta,CONVERT(date,'99991231'))
 AND DATEADD(day,(CONVERT(int,a.DiaSemana)-1-DATEDIFF(day,CONVERT(date,'19000101'),CASE WHEN a.FechaVigenciaDesde>b.FechaVigenciaDesde THEN a.FechaVigenciaDesde ELSE b.FechaVigenciaDesde END)%7+7)%7,
 CASE WHEN a.FechaVigenciaDesde>b.FechaVigenciaDesde THEN a.FechaVigenciaDesde ELSE b.FechaVigenciaDesde END)
 <= CASE WHEN ISNULL(a.FechaVigenciaHasta,CONVERT(date,'99991231'))<ISNULL(b.FechaVigenciaHasta,CONVERT(date,'99991231')) THEN ISNULL(a.FechaVigenciaHasta,CONVERT(date,'99991231')) ELSE ISNULL(b.FechaVigenciaHasta,CONVERT(date,'99991231')) END;
-- Conflictos bloqueo-bloqueo: cero filas en DEMO.
SELECT a.SucursalId,a.BloqueoAgendaId BloqueoA,b.BloqueoAgendaId BloqueoB
FROM dbo.BloqueosAgenda a JOIN dbo.BloqueosAgenda b ON a.SucursalId=b.SucursalId AND a.Fecha=b.Fecha AND a.BloqueoAgendaId<b.BloqueoAgendaId
WHERE a.DiaCompleto=1 OR b.DiaCompleto=1 OR (a.HoraInicio<b.HoraFin AND b.HoraInicio<a.HoraFin);
-- Conflictos horario-bloqueo: cero filas en DEMO; no modifica conflictos históricos.
SELECT h.SucursalId,h.HorarioSucursalId,b.BloqueoAgendaId
FROM dbo.HorariosSucursal h JOIN dbo.BloqueosAgenda b ON b.SucursalId=h.SucursalId
WHERE h.Activo=1 AND h.DiaSemana=(DATEDIFF(day,CONVERT(date,'19000101'),b.Fecha)%7)+1
 AND h.FechaVigenciaDesde<=b.Fecha AND (h.FechaVigenciaHasta IS NULL OR h.FechaVigenciaHasta>=b.Fecha)
 AND (b.DiaCompleto=1 OR (h.HoraInicio<b.HoraFin AND b.HoraInicio<h.HoraFin));
