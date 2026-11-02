# Auditoría del Avance 4

Fecha: 31/10/2026. Rama inicial: feature/avance4-paula. Árbol de trabajo limpio. Se conserva la rama y el historial del equipo.

| Requisito | Estado inicial | Archivo o evidencia | Problema | Corrección necesaria | Estado final | Prueba |
|---|---|---|---|---|---|---|
| Servicios CRUD | Parcial | Controllers/ServiciosController.cs, ServicioService.cs | Tarifa cero permitida; errores sin ubicación | Tarifa positiva y validación visual | CUMPLE: 87 comprobaciones SQL/HTTP | Suite WashLabs.Paula |
| Productos CRUD y categorías SQL | Parcial | ProductoService.cs, ProductoRepository.cs | Precio cero; adicionales opcionales | Precio positivo, marca y presentación requeridas | CUMPLE: 87 comprobaciones SQL/HTTP | Suite WashLabs.Paula |
| Imágenes | Parcial | Services/ImagenService.cs | Sin WEBP ni vista previa | Validación de contenido y previsualización | IMPLEMENTADO SIN VERIFICAR completamente: WEBP y vista previa pendientes | PruebasImagen y 31 comprobaciones de catálogo/Edge |
| Sucursales y encargados múltiples | Completo | SucursalAdministracionRepository.cs | Requiere repetir verificación | Mantener FK Usuarios.SucursalId: muchos usuarios por sucursal | CUMPLE: 97 comprobaciones de sucursales y agenda | Suite WashLabs.Avance4 |
| Horarios y bloqueos | Parcial | AgendaAdministracionService.cs | Mensajes distintos a los solicitados | Ajustar cuatro mensajes | CUMPLE: cruces, fechas y concurrencia verificados | PruebasAgenda y HTTP |
| Datos mínimos | Parcial | Consulta SQL real a WashLabsDB | 10 servicios, 11 productos, 3 sucursales, 18 horarios, 0 bloqueos | Completar cuarta sucursal y cuatro bloqueos sin cruces | CUMPLE: 10/11/4/18/4 registros y dos encargados en San Rafael | Verificacion_SQL_Final.txt |
| Script consolidado | No implementado | Database/Avance4 contiene scripts separados | Falta archivo de entrega único | Crear BaseDatos/WashLabs_Avance4_Completo.sql | CUMPLE en la base existente; archivo consolidado ejecutado | Verificacion_SQL_Final.txt |
| Evidencias y entrega | Parcial | Historial Git y tests existentes | Faltan informe y guiones | Documentar resultados reales y pendientes manuales | Parcial: documentos y capturas de catálogo completos; entrega manual pendiente | REPORTE_FINAL_AVANCE4.md |

Arquitectura existente: Models (DTO y formularios), Services (casos de uso), Data (EF y repositorios), Web (controladores y Razor). Database-first, sin migraciones EF versionadas. La asignación de encargados y los intervalos utilizan transacciones y bloqueos de aplicación para evitar carreras. No se requiere duplicar módulos ni crear una API.


