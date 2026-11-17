# Entrega 5 — Galería multimedia

Fecha de cierre: 2026-11-13 (Colombia).

## Alcance y comparación

Referencia de solo lectura: `C:\Users\Yurgen Velasco\source\repos\ugpmma.com.co\ugpmma.com.co`.
La exportación `index.html` contiene la composición `ugp-exp` (aproximadamente líneas 394–608): videos MP4 con fondo duplicado, reproducción automática silenciada, control de sonido y tiras de fotografías repetidas. `estilos.css` y `public/css/estilos.css` definen grid, tarjetas, videos y pista animada (líneas 664, 700 y 931–948). La plantilla `views/pages/index.ejs` no contiene toda esa composición.

Se recuperan tarjetas oscuras con borde dorado, tipografía existente, grupos horizontales con navegación y disposición de dos columnas/una columna móvil. No se copian ofertas, medios ni textos testimoniales estáticos. La galería nueva está en `/#galeria`; `/#experiencias` conserva las reseñas anteriores.

Diferencias deliberadas: una diapositiva visible por grupo, navegación manual, sin duplicar fotografías para simular un bucle infinito y sin autoplay. La transición respeta `prefers-reduced-motion`. Los modelos actuales discriminan Imagen/VideoYouTube; no se inventó un tipo MP4 ni se trasladaron videos externos automáticamente. No es una réplica visual exacta del original.

## Archivos de esta entrega

Modificados:

- `Components/Pages/Home.razor`: inserción del componente antes de las reseñas; no reconstrucción de Home.
- `Tests/Reservas/Program.cs`: opción independiente `--gallery`, antes de ejecutar otras pruebas.

Creados:

- `Components/Multimedia/MultimediaGallery.razor` y `.razor.css`: carga y estados, composición de grupos.
- `Components/Multimedia/GalleryCarousel.razor` y `.razor.css`: anterior/siguiente, indicadores, flechas izquierda/derecha, foco visible, controles táctiles de al menos 44 px.
- `Components/Multimedia/MediaCard.razor` y `.razor.css`: imagen adaptable, error de imagen y video bajo solicitud explícita.
- `Components/Multimedia/MediaUrl.cs`: política de URLs y representación de medios.
- `Tests/Reservas/GalleryChecks.cs`: pruebas aisladas de seguridad y renderizado Razor.
- Este informe.

No se modificaron modelos, DbContext, servicios de compra, pagos, QR, SQL ni configuración. Los cambios previos de esos archivos siguen en el árbol.

## Consultas y dependencias

Se reutiliza `ApplicationDbContext`, sin introducir un sistema paralelo ni paquetes:

- `SeccionesExperiencia`: activos, primer registro por ID; encabezado/subtítulo/descripción. No hay campo Orden en este modelo. Si hay varias configuraciones activas, la selección por ID es determinista, pero requiere revisión editorial.
- `SeccionesMultimedia`: activos por Orden e ID; textos y videos principal/secundario, en ese orden.
- `ExperienciaMultimediaItems`: activos por CarruselNumero, Orden e ID. Agrupación por CarruselNumero.
- Todas las consultas son `AsNoTracking`. Se usa un scope propio con contexto separado para no ejecutar consultas simultáneas sobre el DbContext scoped de Home.
- CarruselNumero no es una FK a SeccionMultimedia; no se inventó una asociación. Los grupos de ambos orígenes se muestran por separado.
- `Experiencia` continúa siendo una reseña: no alimenta la galería.

No se renderizan BeneficiosJson, enlaces CMS ni HTML arbitrario. TiempoAutoplay no se utiliza: el carrusel es manual. No se crearon tablas ni se ejecutaron scripts.

## Seguridad y estados

- Imágenes: rutas locales `/images/` o HTTPS en hosts exactos ugpmma.com/www.ugpmma.com/ugpmma.com.co/www.ugpmma.com.co; formatos jpg/jpeg/png/webp/avif/gif. Se rechazan esquemas peligrosos, rutas con traversal, codificación porcentual, barras invertidas, credenciales en URL, puertos alternativos y SVG.
- Videos: formatos YouTube watch, embed, shorts o youtu.be con ID de 11 caracteres; iframe reconstruido exclusivamente en `www.youtube-nocookie.com`, sin trasladar parámetros recibidos. No hay iframe antes de pulsar «Cargar video» ni autoplay.
- Títulos y descripciones escapados por Razor. Sin MarkupString, innerHTML ni descargas al servidor. Referrer-policy no-referrer en medios.
- Los recursos inválidos se omiten con aviso genérico y contador en logging, sin imprimir URLs.
- Estados diferenciados: carga, vacío exitoso, recursos no admitidos, error SQL y fallo de imagen. Error SQL no se presenta como colección vacía; botón de reintento. Logging registra tipo de excepción, no conexión ni SQL.
- Titulo se usa como alt/título accesible. Si falta se usa una etiqueta genérica; completar descripciones editoriales sigue pendiente. Subtítulos y permisos de reproducción dependen del video autorizado en YouTube.

## Pruebas ejecutadas

| Caso | Resultado | Evidencia / límite |
|---|---|---|
| Compilación aplicación | PASA | `dotnet build --no-restore -p:UseSharedCompilation=false -o artifacts/entrega5`: 0 errores y 0 advertencias |
| Compilación pruebas | PASA | build de Tests/Reservas/Reservas.Checks.csproj a artifacts/entrega5-tests: 0 errores y 0 advertencias |
| Seguridad de URLs | PASA, aislada | Esquemas, hosts impostores, userinfo, traversal, puerto, SVG, IDs inválidos y duplicados; normalización sin autoplay |
| Render Razor | PASA, aislada | Texto script escapado, semántica de carrusel/indicador, ausencia de iframe antes de solicitar video |
| Suite galería | PASA, aislada | `dotnet artifacts/entrega5-tests/Reservas.Checks.dll --gallery`: 24 comprobaciones |
| Suite existente | PASA, aislada | `dotnet artifacts/entrega5-tests/Reservas.Checks.dll --isolated`: 30 comprobaciones; no ejecutó SQL |
| Home y galería vacía | PASA, HTTP/SQL y navegador | Host loopback 127.0.0.1:5195, Development, LocalDB/UGP_Integracion_Test. Mostró «Aún no hay contenido multimedia publicado». Datos sintéticos ya existentes de eventos; no se insertaron registros |
| Responsive vacío 360/768/1440 | PARCIAL | Anchos de documento 345/753/1425 respectivamente, menores que viewport. Capturas legibles a 360/768. Captura de 1440 limitada por superficie visible; no se afirma inspección visual completa de escritorio |
| Consola navegador | PASA en recorrido observado | Ningún error/advertencia capturado en consulta de logs; no equivale a inspección exhaustiva de Network |
| Carrusel con medios SQL reales, anterior/siguiente/indicadores/teclado | PENDIENTE | La base aislada no devolvió medios; no se añadieron datos para forzar el caso |
| Reproducción YouTube, contenido externo bloqueado, subtítulos | PENDIENTE | No hubo un recurso publicado disponible |
| SQL inaccesible y recuperación/reintento | PENDIENTE | Manejo implementado y revisado, no probado en navegador en esta entrega |
| Inactivos y orden persistido con conjunto mixto | PENDIENTE integración | Filtros/orden revisados en código; faltan registros adecuados para demostrarlo |
| git diff --check | PASA | Sin errores; Git emitió avisos de normalización LF/CRLF en archivos previos |

El primer arranque restringido falló por permisos del directorio Data Protection. Se repitió con ejecución local autorizada, sin cambiar código de seguridad. El host usó únicamente flags de proceso `Reservas:Habilitadas=false` y `Wompi:Webhooks:Habilitados=false`. El worker sale sin procesar reservas en esas condiciones. No hubo habilitación de compras, escritura SQL ni cambio persistido de secretos/configuración. Host detenido al terminar. Se observó advertencia de redirección HTTPS sin puerto en el host HTTP loopback de prueba, no un fallo de la galería.

## Riesgos residuales y aceptación

1. Revisar catálogo real, permisos editoriales y orígenes de imágenes antes de ampliar la lista permitida. Otros CDN quedan rechazados deliberadamente.
2. Completar pruebas visuales con medios existentes y navegación del carrusel, así como error SQL controlado. No se declara reproducción verificada.
3. Disponibilidad, redirecciones y políticas de embedding del proveedor externo no pueden garantizarse con validación sintáctica de URL.
4. Los grupos se consultan completos; si el catálogo crece considerablemente, evaluar paginación en una entrega posterior.
5. Imágenes remotas conectan con el host editorial al entrar en pantalla; YouTube requiere acción explícita.

## Git y dictamen

Rama `main`, HEAD `62bdf1d864aa02de4e5e52442083d74dbb43c494`, ahead 1 respecto a la referencia local origin/main. Un único worktree. Índice sin cambios preparados. Se conservaron los 23 archivos versionados modificados de entregas previas y los archivos sin seguimiento. Home acumula cambios anteriores; no atribuir todo su diff a Entrega 5. Tests ya estaba sin seguimiento. No se realizó commit, push, reset, clean ni stash.

**Dictamen: implementación compilada y validada parcialmente; aceptación funcional completa pendiente de medios reales y pruebas de interacción.** El vacío SQL y la protección/renderización aislada están comprobados. Wompi y checkout permanecen sin activar. No se inicia otra entrega.
