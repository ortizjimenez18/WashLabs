# Reporte final del Avance 4 — WashLabs

Fecha: 31 de octubre de 2026. Equipo EvoGroup. Rama conservada: `feature/avance4-paula`. No se realizaron commits ni push.

La revisión inicial encontró los mantenimientos implementados por capas, con transacciones para encargados e intervalos. Faltaban ajustes de precios, WEBP, vista previa, mensajes por campo, los textos exactos de superposición, la cuarta sucursal y cuatro bloqueos. No existían el script consolidado ni los documentos de entrega solicitados.

Ya estaban implementados: listados y detalles, creación y edición de servicios y productos; categorías consultadas en SQL; conservación y sustitución de imágenes; sucursales con selección múltiple; exclusión de encargados ocupados; agenda por sucursal; fecha vigente de Costa Rica; validación de intervalos contiguos, recurrentes, bloqueos de día completo y concurrencia; autorización y protección antifalsificación.

Correcciones realizadas: precios mínimos de 0,01 en Web y Application; marca y presentación obligatorias como campos adicionales del producto; carga WEBP mediante comprobación de firma RIFF/WEBP, tamaño y tipo; vista previa local; imagen requerida al crear en Web; mensajes bajo los campos y activación de validación cliente; cuatro mensajes de superposición ajustados al enunciado. Los servicios usan tipo de lavado y nivel de detalle como adicionales y duración en minutos. Se retiró el documento de instrucciones de generación de imágenes y se dejó un inventario técnico de recursos. No se alteró el historial.

Base de datos identificada y verificada: SQL Server local, `WashLabsDB`. Se creó y verificó un respaldo FULL COPY_ONLY con CHECKSUM antes de ejecutar el script. Se conserva la arquitectura database-first, sin migraciones EF versionadas. Usuarios.SucursalId ya representa varios encargados por sucursal; no requiere tabla asociativa. Se mantiene la FK y se reutilizan los ajustes de restricciones de los scripts existentes. No se eliminaron registros ni se sobrescribieron contraseñas.

El script `BaseDatos/WashLabs_Avance4_Completo.sql` consolida el esquema, completa los registros faltantes del entorno existente y verifica cantidades, encargados, cruces y restricciones. Requiere los datos de los avances anteriores y un respaldo verificado de las últimas 24 horas. Se ejecutaron los bloques y posteriormente el archivo consolidado completo: conserva los mínimos sin duplicar. Se añadió una consulta adicional de integridad de horarios y categorías, ejecutada por separado. Cantidades finales comprobadas: 10 servicios, 11 productos, 4 sucursales, 18 horarios y 4 bloqueos. Se añadieron dos cuentas operativas a San Rafael con hashes no utilizables; el equipo debe configurar sus accesos antes de iniciar sesión con ellas. La asignación múltiple puede inspeccionarse con una sesión administrativa.

Pruebas realizadas:

| Comprobación | Resultado | Evidencia |
|---|---|---|
| Restauración de solución | Correcta, tras reintento con acceso a NuGet | Ejecución de dotnet restore WashLabs.sln |
| Compilación Web y capas | 0 errores y 0 advertencias en ejecución final sin procesos de prueba ocupando archivos | Compilacion.txt |
| Servicios, productos, SQL, imágenes y HTTP | 87 aprobadas | Pruebas_Catalogos.txt |
| Sucursales, encargados, agenda, concurrencia y autorización HTTP | 97 aprobadas | Pruebas_Sucursales_Agenda.txt |
| Cuatro imágenes, filtros y catálogo en Edge | 31 aprobadas | Pruebas_Imagenes_Navegador.txt y catalogo-*.png |
| Conteos y restricciones SQL | Mínimos alcanzados; sin cruces horario-bloqueo o bloqueo-bloqueo en consulta | Verificacion_SQL.txt |
| Asignación persistida de dos encargados | Verificada por consulta SQL | Verificacion_Encargados.txt |

Las expectativas antiguas de precio cero y mensajes anteriores se actualizaron al requisito actual. Las primeras ejecuciones HTTP detectaron estas diferencias; las repeticiones completas finalizaron correctamente. Las pruebas usan transacciones o eliminan únicamente sus registros temporales. No se sustituyen estas comprobaciones por capturas simuladas.

| Requisito | Clasificación final | Alcance de la verificación |
|---|---|---|
| Servicios: listado, detalle, creación, edición y persistencia | CUMPLE | SQL y HTTP con imagen PNG, conservación y reemplazo |
| Productos: CRUD, categorías SQL, filtros y adicionales | CUMPLE | SQL, HTTP y catálogo real |
| Sucursales y asignación múltiple exclusiva | CUMPLE | SQL, HTTP, precarga, quitar/agregar, duplicados y concurrencia |
| Horarios y bloqueos: fechas, rango, cruces y separación por sucursal | CUMPLE | Suite de agenda y HTTP, incluyendo intervalos contiguos |
| Carga PNG, tamaño, nombres seguros y conservación | CUMPLE | Pruebas de archivos y HTTP |
| Nuevas cargas WEBP y previsualización interactiva | IMPLEMENTADO SIN VERIFICAR | Revisado y compilado; falta recorrido manual con WEBP y archivo corrupto con firma válida |
| Mensajes por campo y validación cliente en formularios administrativos | IMPLEMENTADO SIN VERIFICAR | Razor compilado y HTTP probado; falta interacción manual completa |
| Catálogo responsive y cuatro imágenes existentes | CUMPLE | Edge a 1440, 768 y 390 px |
| Mínimo de cuatro registros por entidad | CUMPLE | SQL real, sin registros temporales |
| Script consolidado SQL Server 2019 en el entorno existente | CUMPLE | Verificacion_SQL_Final.txt y Verificacion_Integridad.txt; requiere esquema y datos previos de Avance 3 |
| Arquitectura y autorización existentes preservadas | CUMPLE | Referencias por capas y regresión HTTP |
| Diez commits del tramo revisado | CUMPLE | Historial_Git.txt: 10 commits desde 975823c excluido |
| Evidencias visuales de todos los mantenimientos | PENDIENTE | Solo catálogo capturado automáticamente; seguir Guia_Entrega.md |
| Trello, videos, firmas y entrega institucional | PENDIENTE | Requieren intervención de los estudiantes |

Rutas: `/Servicios`, `/Servicios/Crear`, `/Servicios/Editar/{id}`, `/Servicios/Detalle/{id}`; `/Productos`, `/Productos/Mantenimiento`, `/Productos/Crear`, `/Productos/Editar/{id}`, `/Productos/DetalleMantenimiento/{id}`; `/Sucursales`, `/Sucursales/Crear`, `/Sucursales/Editar/{id}`, `/Sucursales/Detalle/{id}`; `/Horarios?sucursalId={id}`, `/Horarios/CrearHorario`, `/Horarios/CrearBloqueo`, `/Horarios/DetalleHorario/{id}`, `/Horarios/DetalleBloqueo/{id}`. Usar identificadores reales de los listados.

Archivos nuevos: BaseDatos/WashLabs_Avance4_Completo.sql, BaseDatos/Respaldo_Avance4.sql, BaseDatos/Encargados_Avance4.sql, wwwroot/js/imagen-formulario.js y documentos, registros y capturas en Evidencias/Avance4.

Archivos modificados: Controllers/ProductosController.cs y ServiciosController.cs; Services/ImagenService.cs; formularios Razor de Productos, Servicios, Sucursales y Horarios; ProductoFormulario.cs y ServicioFormulario.cs; AgendaAdministracionService.cs; pruebas de catálogos y expectativa HTTP de agenda; docs/imagenes-productos-washlabs.md. Archivo retirado: docs/prompts-imagenes-productos.md. Consultar Archivos_Cambiados.txt para inventario final.

Pendientes técnicos: comprobar manualmente WEBP y vista previa; revisar con el profesor el alcance de imágenes obligatorias para registros históricos; comprobar restauración del respaldo en otro entorno de desarrollo. La comprobación actual del contenido utiliza firmas y estructura básica, no una decodificación completa de todas las imágenes. No se afirma que cualquier archivo corrupto con firma válida sea rechazado.

Pasos manuales de entrega y guiones: Guia_Entrega.md. Este reporte constituye el documento técnico de entrega; completar la portada institucional y las firmas digitales certificadas por ambos estudiantes.

