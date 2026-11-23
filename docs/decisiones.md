# Decisiones de diseño

Registro breve de las decisiones que dan forma al proyecto: el contexto, lo que se eligió, lo que se descartó y por qué. Si una decisión cambia, se agrega una entrada nueva en lugar de editar la anterior.

## 1. Dapper en lugar de Entity Framework Core

- **Contexto:** el modelo tiene cuatro entidades y consultas sencillas (filtros por propietario, paginación y algún `JOIN`).
- **Decisión:** SQL escrito a mano con Dapper sobre MySqlConnector, y esquema en scripts SQL versionados que aplica DbUp al arrancar.
- **Descartado:** EF Core con migraciones generadas. Añade un modelo de cambios, *tracking* y un proveedor de MySQL de terceros que no aportan nada a este tamaño de dominio.
- **Consecuencia:** cada consulta es visible y parametrizada; a cambio, los mapeos de columnas (`u_key AS Id`…) se mantienen a mano.

## 2. Sesiones guardadas en la base de datos

- **Contexto:** con la cookie de autenticación autocontenida, una copia robada sigue siendo válida hasta caducar, aunque el usuario cierre sesión.
- **Decisión:** `ITicketStore` sobre la tabla `a_sessions`: la cookie solo lleva un identificador aleatorio, y además un sello de seguridad por usuario invalida todas sus sesiones al cambiar la contraseña o el correo.
- **Descartado:** cookie autocontenida (no se puede revocar) y JWT (mismo problema, y la app es MVC con formularios, no una API).
- **Consecuencia:** una consulta extra por petición, mitigada con una caché corta del sello (30 s).

## 3. Tokens de correo sin estado con Data Protection

- **Contexto:** confirmación de cuenta, restablecimiento de contraseña y cambio de correo necesitan enlaces firmados y con caducidad.
- **Decisión:** `ITimeLimitedDataProtector` de ASP.NET Core. El token de restablecimiento incluye el sello de seguridad, así que deja de servir en cuanto la contraseña cambia.
- **Descartado:** guardar tokens aleatorios en una tabla. Habría que purgarlos y protegerlos (hash) igual que una contraseña.
- **Consecuencia:** las claves de Data Protection deben persistir (volumen en Docker); si se pierden, se invalidan los enlaces pendientes y las sesiones.

## 4. Bandeja de salida para los correos

- **Contexto:** enviar por SMTP dentro de la petición hacía que una caída del servidor de correo rompiera el registro con la cuenta ya creada, y que el tiempo de respuesta delatara si un correo estaba registrado.
- **Decisión:** las peticiones insertan en `a_outbox` y un `BackgroundService` envía con reintentos y espera exponencial, bloqueando filas con `FOR UPDATE SKIP LOCKED` para poder correr varias réplicas.
- **Descartado:** `try/catch` alrededor del envío síncrono (más simple, pero el correo se pierde y la petición sigue esperando al SMTP) y un broker de mensajes externo (infraestructura de más para este volumen).
- **Consecuencia:** el correo llega unos instantes después de la acción; los mensajes enviados se borran porque llevan tokens.

## 5. Contadores de intentos en MySQL

- **Contexto:** el rate limiting por IP y el bloqueo por cuenta vivían en memoria, así que cada réplica llevaba su propia cuenta y un reinicio la ponía a cero.
- **Decisión:** tabla `a_attempt_counters` con ventanas fijas e incremento atómico (`INSERT … ON DUPLICATE KEY UPDATE`), aplicada con un filtro de MVC (`[RateLimit]`).
- **Descartado:** el middleware de rate limiting de ASP.NET Core (solo guarda estado en memoria) y Redis (un servicio más que operar).
- **Consecuencia:** dos consultas por petición limitada; `DatabaseCleanupService` purga las filas caducadas.

## 6. Imagen *chiseled* y .NET 10 LTS

- **Contexto:** la imagen de producción debe tener la menor superficie posible.
- **Decisión:** runtime `aspnet:10.0-noble-chiseled` (sin shell ni gestor de paquetes, usuario no root). El `HEALTHCHECK` lo hace la propia app (`dotnet Encuestas.Web.dll healthcheck`) porque no hay `curl`. Se mantiene .NET 10 por ser LTS; Dependabot ignora los saltos de versión mayor.
- **Descartado:** imagen `aspnet` completa (más paquetes que parchear) y seguir las versiones STS.
