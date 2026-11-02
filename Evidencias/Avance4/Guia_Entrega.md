# Guía de evidencias y entrega

Iniciar WashLabs en Development con su conexión local configurada y entrar como Administrador. Conservar únicamente capturas reales y ocultar información privada.

1. Abrir /Servicios: capturar listado y detalle; crear con imagen, tipo y nivel; editar precio y duración. Capturar confirmaciones y rechazo de precio cero y duración cero.
2. Abrir /Productos/Mantenimiento: crear con categoría real, marca, presentación e imagen. Capturar vista previa. Editar sin imagen y luego reemplazarla. Consultar /Productos y verificar búsqueda y categoría.
3. Abrir /Sucursales y editar WashLabs San Rafael: capturar sus dos encargados seleccionados. Retirar uno, guardar y comprobar que aparece disponible al crear otra sucursal; devolver la asignación al terminar. Confirmar que encargados de otras sucursales no aparecen.
4. Abrir /Horarios, elegir una sucursal y capturar ambas secciones. Crear un horario futuro en rango libre; crear bloqueo contiguo. Intentar cada uno de los cuatro cruces y capturar el mensaje específico. Repetir con fecha anterior y horas invertidas. Usar rangos libres distintos de los horarios recurrentes existentes.
5. Ejecutar las consultas del script y capturar cantidades y asociaciones en SQL Server Management Studio. No capturar contraseñas ni cadenas de conexión.
6. Comprobar carga de WEBP válido, archivo renombrado, archivo corrupto y archivo mayor de 2 MB. Verificar los mensajes junto a cada entrada y la vista previa. Estas interacciones quedan pendientes de comprobación manual.

Guion de demostración de 9 minutos:

| Tiempo | Demostración |
|---|---|
| 0:00–0:40 | Ambos integrantes con rostro visible, curso, equipo y objetivo |
| 0:40–2:00 | Servicios: listado, creación, detalle y edición |
| 2:00–3:30 | Productos: categorías SQL, adicionales, carga, vista previa y reemplazo |
| 3:30–5:00 | Sucursales: dos encargados, exclusión de ocupados, edición de asociaciones |
| 5:00–6:20 | Horarios por sucursal: fecha vigente, rango y separación entre sucursales |
| 6:20–7:50 | Bloqueos y cuatro mensajes de superposición; rango contiguo permitido |
| 7:50–9:00 | SQL con mínimos, pruebas, historial real y cierre |

El enunciado menciona además un video explicativo de máximo 5 minutos. Confirmar con el profesor si son entregables separados. Guion breve de 4:50: presentación 0:20; arquitectura 0:40; servicios/productos 1:00; sucursales y encargados 1:00; agenda y validaciones 1:00; BD, pruebas y cierre 0:50. Ambos estudiantes deben aparecer; grabación pendiente.

Trello: propuestas sustentadas por el historial, pendientes de confirmación y actualización por cada integrante:

| Integrante | Actividades identificadas | Evidencia |
|---|---|---|
| María Paula Murillo | Servicios, productos, pruebas de catálogos, mejoras visuales | 5530b64, 41b6f3f, 5879b7b, 8e6b10b |
| Sebastián Ortiz | Sucursales, agenda, pruebas y usuarios | 9a29802, ff65dbe, a35622c, 68f50b2 |

Las correcciones de esta revisión no se atribuyen a ninguno de los integrantes. Distribuirlas según quién las revise y pruebe realmente. Adjuntar capturas reales de tarjetas y participantes; no se actualizó Trello desde esta sesión.

El historial del tramo 975823c..HEAD contiene 10 commits reales. No se requieren cambios artificiales para alcanzar la cantidad. Para los cambios actuales se propone, después de revisar: validaciones de catálogos; carga y vista previa; agenda y pruebas; datos y script; documentación y evidencias. El equipo decide los commits auténticos y su autoría. No se hicieron commits ni push.

Checklist:

- [x] Solución compilada y pruebas registradas.
- [x] Script consolidado preparado y bloques ejecutados contra base identificada y respaldada.
- [x] Mínimos de datos y asignación múltiple consultados en SQL.
- [x] Historial Git real exportado.
- [x] Capturas reales del catálogo en tres tamaños.
- [x] Reporte técnico de entrega.
- [ ] Capturas de todos los mantenimientos y mensajes de error.
- [ ] Verificación interactiva de WEBP y vista previa.
- [ ] Capturas de Trello por los integrantes.
- [ ] Video de 8–10 minutos con rostros.
- [ ] Video breve si lo confirma el profesor.
- [ ] Portada y formato final institucional.
- [ ] Firmas digitales con certificado de los estudiantes.
- [ ] Configurar contraseñas locales de cuentas nuevas antes de utilizarlas.
- [ ] Revisión final de los estudiantes y carga en plataforma del curso.
