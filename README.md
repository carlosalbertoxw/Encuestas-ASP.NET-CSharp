# Encuestas

Aplicación web de encuestas escrita en C# con ASP.NET Core MVC (.NET 10) y MySQL. Cada usuario registrado puede crear encuestas, ordenarlas en su tablero, compartir su perfil público y recibir respuestas de otros usuarios (calificación de 1 a 5 estrellas más comentario).

## Características

- Registro con **verificación de correo** e inicio de sesión con autenticación por cookies (claims de ASP.NET Core).
- **Sesiones en el servidor** (tabla `a_sessions` vía `ITicketStore`): la cookie solo lleva un identificador, así que cerrar sesión o borrar la cuenta invalida la sesión aunque alguien haya copiado la cookie.
- **Restablecimiento de contraseña** por enlace con token firmado y caducidad (Data Protection, con claves persistidas en un volumen).
- **Cambio de correo con confirmación**: el correo nuevo queda pendiente y solo se aplica al abrir el enlace enviado a esa dirección; se avisa al correo anterior y se cierran todas las sesiones.
- Enlaces de los correos construidos con la URL pública configurada (`App:BaseUrl`), nunca con la cabecera `Host` de la petición.
- Envío de correos por **SMTP** (MailKit); en local, **Mailpit** captura los correos y los muestra en una interfaz web.
- Contraseñas con hash PBKDF2 (ASP.NET Core Identity) y re-hash transparente.
- Sello de seguridad (*security stamp*): al cambiar la contraseña o el correo se invalidan las demás sesiones.
- Gestión de cuenta: editar nombre, cambiar usuario, correo y contraseña, y eliminar la cuenta (con borrado en cascada de sus datos).
- CRUD de encuestas con posición configurable; tablero, perfil público y respuestas **paginados**.
- Perfil público por nombre de usuario (`/User/Index?profile=usuario`).
- Respuestas: calificación de 1 a 5 estrellas y comentario, **una por usuario y encuesta**.
- Protección CSRF (antiforgery) en todos los POST y validación en servidor (ViewModels + DataAnnotations) y en cliente (sin jQuery).
- Rate limiting por IP en login/registro/recuperación más **bloqueo por cuenta**; cabeceras de seguridad (CSP sin `unsafe-inline` en scripts) y logs sin correos en claro.
- Logs en **JSON** fuera de desarrollo, con `TraceId`/`RequestId` por petición y cabecera `X-Request-Id` en cada respuesta.
- Acceso a datos con **Dapper** sobre MySqlConnector; migraciones con DbUp aplicadas al arrancar.
- Sondas `/health/live` (proceso) y `/health/ready` (incluye la base de datos), con `HEALTHCHECK` en la imagen.
- Imagen Docker *chiseled* (sin shell, usuario no root) y analizadores de .NET (`latest-recommended`) en cada compilación.

## Requisitos

- [SDK de .NET 10](https://dotnet.microsoft.com/download) (`global.json` fija la línea 10.0).
- [Docker](https://www.docker.com/) con Docker Compose, para MySQL 8.4 y Mailpit.

## Instalación y ejecución

### Opción A — Todo en Docker

Construye y levanta la aplicación (puerto 8080), MySQL y Mailpit:

```bash
docker compose up --build
```

La app arranca en `http://localhost:8080` en modo Producción (sin datos de demostración). Regístrate para crear una cuenta y abre los correos (confirmación, restablecimiento, cambio de correo) en Mailpit: `http://localhost:8025`.

### Opción B — Base de datos y Mailpit en Docker + app local (desarrollo)

```bash
# 1. Levantar MySQL y Mailpit
docker compose up -d db mailpit

# 2. Ejecutar la aplicación (aplica migraciones y, en Development, inserta datos demo)
dotnet run --project src/Encuestas.Web --launch-profile http
```

El perfil `http` de `launchSettings.json` apunta el SMTP a Mailpit (`localhost:1025`). Sin SMTP configurado, en Development los correos se escriben en el log.

### Usuarios de demostración

En el entorno **Development** (Opción B), si la base está vacía se insertan dos cuentas de prueba **ya confirmadas**:

| Correo               | Contraseña | Nota                                            |
|----------------------|------------|-------------------------------------------------|
| `demo@encuestas.dev` | `demo1234` | Tiene una encuesta de ejemplo con una respuesta |
| `ana@encuestas.dev`  | `ana12345` | Responde la encuesta de la cuenta demo          |

## Configuración

| Clave (variable de entorno)                        | Descripción                                                         | Desarrollo |
|----------------------------------------------------|---------------------------------------------------------------------|------------|
| `ConnectionStrings:Default` (`ConnectionStrings__Default`) | Cadena de conexión de MySQL                                  | `appsettings.Development.json` |
| `App:BaseUrl` (`App__BaseUrl`)                     | URL pública; base de los enlaces de los correos. **Obligatoria**    | `http://localhost:5090` |
| `Smtp:Host`, `Smtp:Port` (`Smtp__Host`…)           | Servidor SMTP. **Obligatorio** fuera de Development                 | Mailpit en `launchSettings.json` |
| `Smtp:UseStartTls`, `Smtp:User`, `Smtp:Password`, `Smtp:From` | STARTTLS (por defecto `true`), credenciales y remitente  | — |
| `DataProtection:KeysPath` (`DataProtection__KeysPath`) | Directorio donde se guardan las claves de Data Protection      | perfil del usuario |

Para no versionar secretos locales, usa *user-secrets* de .NET:

```bash
dotnet user-secrets --project src/Encuestas.Web set "ConnectionStrings:Default" "Server=...;Password=...;"
```

Docker Compose se personaliza copiando `.env.example` a `.env`:

| Variable              | Descripción                                   | Por defecto             |
|-----------------------|-----------------------------------------------|-------------------------|
| `MYSQL_ROOT_PASSWORD` | Contraseña de root de MySQL                   | `root`                  |
| `MYSQL_DATABASE`      | Nombre de la base de datos                    | `encuestas`             |
| `MYSQL_USER`          | Usuario de la aplicación                      | `encuestas`             |
| `MYSQL_PASSWORD`      | Contraseña del usuario                        | `encuestas`             |
| `MYSQL_PORT`          | Puerto de MySQL en el host                    | `3306`                  |
| `WEB_PORT`            | Puerto de la aplicación web (Opción A)        | `8080`                  |
| `APP_BASE_URL`        | URL pública (cámbiala si cambias `WEB_PORT`)  | `http://localhost:8080` |
| `MAILPIT_SMTP_PORT`   | Puerto SMTP de Mailpit                        | `1025`                  |
| `MAILPIT_UI_PORT`     | Interfaz web de Mailpit                       | `8025`                  |

Los valores por defecto son solo para desarrollo local.

Tras un *reverse proxy*, agrega su red a `KnownIPNetworks` (o `KnownProxies`) en `Program.cs` para que se respeten `X-Forwarded-For`/`X-Forwarded-Proto`.

## Estructura del proyecto

```
src/
  Encuestas.Web/      # ASP.NET Core MVC: controladores, vistas Razor, wwwroot (Bootstrap 5.3, sin jQuery)
    Controllers/      # User (inicio, acceso, recuperación), Account (gestión de la cuenta), Poll, Answer, Error
    Models/           # ViewModels con validación DataAnnotations
    Services/         # AuthService, PasswordService, TokenService, correo (SMTP / log), AppUrls, AccountLockout...
    Infrastructure/   # MigrationRunner (DbUp), MySqlTicketStore, DevDataSeeder, DatabaseHealthCheck
    Migrations/       # Scripts SQL versionados, embebidos en el ensamblado
  Encuestas.Data/     # Repositorios con Dapper sobre MySqlConnector (RepositoryResult, PagedResult)
  Encuestas.Model/    # Entidades: User, UserProfile, Poll, Answer
tests/
  Encuestas.Tests/    # xUnit v3: unitarias + integración (trait Category=Integration, Testcontainers y WebApplicationFactory)
Dockerfile            # Imagen multi-stage (runtime chiseled, usuario no root, HEALTHCHECK)
docker-compose.yml    # web + MySQL 8.4 + Mailpit, con volúmenes para datos y claves
global.json           # Versión del SDK y runner de pruebas (Microsoft.Testing.Platform)
```

## Pruebas

Las pruebas usan xUnit v3 sobre Microsoft.Testing.Platform (activado en `global.json`):

```bash
# Suite completa (las de integración requieren Docker en ejecución)
dotnet test --solution Encuestas.slnx

# Solo unitarias (rápidas, sin Docker)
dotnet test --solution Encuestas.slnx --filter-not-trait "Category=Integration"

# Solo integración (Testcontainers)
dotnet test --solution Encuestas.slnx --filter-trait "Category=Integration"
```

- **Unitarias**: hashing y re-hash (`PasswordService`), flujo de inicio de sesión (`AuthService`), tokens firmados de confirmación, cambio de correo y restablecimiento (`TokenService`), bloqueo por cuenta, caché del sello, paginación, `AppUrls`, enmascarado de correos en logs y validación de ViewModels.
- **Integración**: repositorios contra un MySQL 8.4 efímero (incluido el flujo del correo pendiente) y el pipeline HTTP completo con `WebApplicationFactory` (sondas de salud, autorización, login con sesión en BD y antiforgery).

## Integración continua

En cada push a `master` y en cada pull request, GitHub Actions ejecuta:

| Workflow / job                     | Qué hace                                                                                         |
|------------------------------------|--------------------------------------------------------------------------------------------------|
| `ci.yml` · `pruebas-unitarias`     | Restaura en modo *locked* (falla si NuGetAudit detecta una vulnerabilidad), compila con `-warnaserror` y ejecuta las unitarias |
| `ci.yml` · `pruebas-integracion`   | Ejecuta las pruebas de integración con Testcontainers                                            |
| `ci.yml` · `imagen`                | Construye la imagen Docker y la escanea con Trivy (falla con vulnerabilidades altas o críticas corregibles) |
| `codeql.yml`                       | Análisis estático de seguridad (CodeQL) en cada cambio y semanalmente                            |

Las versiones exactas de todas las dependencias (incluidas las transitivas) están fijadas en los `packages.lock.json`. Dependabot ([dependabot.yml](.github/dependabot.yml)) propone cada semana actualizaciones de NuGet, de las imágenes del Dockerfile y de Docker Compose, y de GitHub Actions.

## Base de datos

El esquema vive en [src/Encuestas.Web/Migrations](src/Encuestas.Web/Migrations) como scripts versionados que DbUp aplica al arrancar (MySQL 8.4, `utf8mb4`, InnoDB). Para evolucionar el esquema, agrega un script nuevo (`0003_...sql`); nunca edites uno ya aplicado:

| Tabla              | Contenido                                                             | Relaciones                                       |
|--------------------|-----------------------------------------------------------------------|--------------------------------------------------|
| `a_users`          | Cuentas: correo, correo pendiente, confirmación, hash y sello         | —                                                |
| `a_users_profiles` | Perfil público: usuario y nombre                                      | 1–1 con `a_users` (cascada)                      |
| `a_polls`          | Encuestas: título, descripción, posición                              | N–1 con `a_users_profiles` (cascada)             |
| `a_answers`        | Respuestas: estrellas (1–5) y comentario, únicas por usuario/encuesta | N–1 con `a_polls` y `a_users_profiles` (cascada) |
| `a_sessions`       | Sesiones de autenticación (ticket serializado y caducidad)            | N–1 con `a_users` (cascada)                      |

Para regenerar la base de datos desde cero (las migraciones y los datos demo se reaplican al arrancar la app):

```bash
docker compose down -v && docker compose up -d
```

## Licencia

[MIT](LICENSE).
