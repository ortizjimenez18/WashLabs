# Relaciones verificadas de WashLabsDB

34 entidades, 47 claves foráneas. Validación contra metadatos SQL; sin cambios de datos o esquema.

| Entidad dependiente | Propiedad FK | Entidad principal | Cardinalidad | Restricción SQL |
|---|---|---|---|---|
| BloqueoAgenda | SucursalId | Sucursal | N:1 requerida | FK_BloqueosAgenda_Sucursal |
| BloqueoAgenda | CreadoPorUsuarioId | Usuario | N:1 requerida | FK_BloqueosAgenda_Usuario |
| Cita | ClienteId | Usuario | N:1 requerida | FK_Citas_Cliente |
| Cita | CreadoPorUsuarioId | Usuario | N:1 requerida | FK_Citas_Creador |
| Cita | EstadoCitaId | EstadoCita | N:1 requerida | FK_Citas_Estado |
| Cita | ServicioId | Servicio | N:1 requerida | FK_Citas_Servicio |
| Cita | SucursalId | Sucursal | N:1 requerida | FK_Citas_Sucursal |
| Cita | VehiculoId | Vehiculo | N:1 requerida | FK_Citas_Vehiculo |
| CriterioProducto | ProductoId | Producto | N:1 requerida | FK_CriteriosProducto_Producto |
| DetalleFactura | FacturaId | Factura | N:1 requerida | FK_DetallesFactura_Factura |
| DetalleFactura | ProductoId | Producto | N:1 opcional | FK_DetallesFactura_Producto |
| DetalleFactura | ServicioId | Servicio | N:1 opcional | FK_DetallesFactura_Servicio |
| DetalleFacturaPersonalizacion | CriterioProductoId | CriterioProducto | N:1 requerida | FK_DFP_Criterio |
| DetalleFacturaPersonalizacion | DetalleFacturaId | DetalleFactura | N:1 requerida | FK_DFP_Detalle |
| DetalleFacturaPersonalizacion | OpcionCriterioId | OpcionCriterioProducto | N:1 requerida | FK_DFP_Opcion |
| Factura | CitaId | Cita | 1:1 opcional | FK_Facturas_Cita |
| Factura | ClienteId | Usuario | N:1 requerida | FK_Facturas_Cliente |
| Factura | EstadoFacturaId | EstadoFactura | N:1 requerida | FK_Facturas_Estado |
| Factura | SucursalId | Sucursal | N:1 requerida | FK_Facturas_Sucursal |
| HistorialEstadoCita | EstadoAnteriorId | EstadoCita | N:1 opcional | FK_HistorialEstadoCita_Anterior |
| HistorialEstadoCita | CitaId | Cita | N:1 requerida | FK_HistorialEstadoCita_Cita |
| HistorialEstadoCita | EstadoNuevoId | EstadoCita | N:1 requerida | FK_HistorialEstadoCita_Nuevo |
| HistorialEstadoCita | UsuarioId | Usuario | N:1 opcional | FK_HistorialEstadoCita_Usuario |
| HorarioSucursal | SucursalId | Sucursal | N:1 requerida | FK_HorariosSucursal_Sucursal |
| InteraccionAsistente | OpcionAsistenteId | OpcionAsistente | N:1 requerida | FK_InteraccionesAsistente_Opcion |
| InteraccionAsistente | PreguntaAsistenteId | PreguntaAsistente | N:1 requerida | FK_InteraccionesAsistente_Pregunta |
| InteraccionAsistente | SesionAsistenteId | SesionAsistente | N:1 requerida | FK_InteraccionesAsistente_Sesion |
| MovimientoWashPoints | CitaId | Cita | 1:1 opcional | FK_MovimientosWP_Cita |
| MovimientoWashPoints | TarjetaWashPointsId | TarjetaWashPoints | N:1 requerida | FK_MovimientosWP_Tarjeta |
| NotificacionCita | CitaId | Cita | N:1 requerida | FK_NotificacionesCita_Cita |
| OpcionAsistente | PreguntaAsistenteId | PreguntaAsistente | N:1 requerida | FK_OpcionesAsistente_Pregunta |
| OpcionAsistente | SiguientePreguntaId | PreguntaAsistente | N:1 opcional | FK_OpcionesAsistente_Siguiente |
| OpcionCriterioProducto | CriterioProductoId | CriterioProducto | N:1 requerida | FK_OpcionesCriterio_Criterio |
| PagoFactura | FacturaId | Factura | N:1 requerida | FK_PagosFactura_Factura |
| PagoFactura | MetodoPagoId | MetodoPago | N:1 requerida | FK_PagosFactura_Metodo |
| PreguntaAsistente | CategoriaAsistenteId | CategoriaAsistente | N:1 requerida | FK_PreguntasAsistente_Categoria |
| Producto | CategoriaProductoId | CategoriaProducto | N:1 requerida | FK_Productos_Categoria |
| RecompensaWashPoints | TarjetaWashPointsId | TarjetaWashPoints | N:1 requerida | FK_RecompensasWP_Tarjeta |
| RespuestaReserva | CitaId | Cita | N:1 requerida | FK_RespuestasReserva_Cita |
| RespuestaReserva | PreguntaReservaId | PreguntaReserva | N:1 requerida | FK_RespuestasReserva_Pregunta |
| SesionAsistente | UsuarioId | Usuario | N:1 opcional | FK_SesionesAsistente_Usuario |
| SucursalServicio | ServicioId | Servicio | N:1 requerida | FK_SucursalServicios_Servicio |
| SucursalServicio | SucursalId | Sucursal | N:1 requerida | FK_SucursalServicios_Sucursal |
| TarjetaWashPoints | VehiculoId | Vehiculo | 1:1 requerida | FK_TarjetasWP_Vehiculo |
| Usuario | RolId | Rol | 1:1 requerida | FK_Usuarios_Roles |
| Usuario | SucursalId | Sucursal | N:1 opcional | FK_Usuarios_Sucursales |
| Vehiculo | ClienteId | Usuario | N:1 requerida | FK_Vehiculos_Cliente |
