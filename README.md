# Encuestas

Aplicación web de encuestas escrita en C# con ASP.NET Core MVC (.NET 10) y MySQL. Cada usuario registrado puede crear encuestas, ordenarlas en su tablero, compartir su perfil público y recibir respuestas de otros usuarios (calificación de 1 a 5 estrellas más comentario).

## Características

- Registro con **verificación de correo** e inicio de sesión con autenticación por cookies (claims de ASP.NET Core).
- **Restablecimiento de contraseña** por enlace con token firmado y caducidad (Data Protection).
- Contraseñas con hash PBKDF2 (ASP.NET Core Identity).
- Sello de seguridad (*security stamp*): al cambiar la contraseña se invalidan las demás sesiones abiertas.
- Gestión de cuenta: editar nombre, cambiar usuario, correo y contraseña, y eliminar la cuenta (con borrado en cascada de sus datos).
- CRUD de encuestas con posición configurable en el tablero.
- Perfil público por nombre de usuario (`/User/Index?profile=usuario`) con las encuestas de esa persona.
- Respuestas a encuestas: calificación de 1 a 5 estrellas y comentario, **una por usuario y encuesta**; el dueño ve todas las respuestas recibidas, paginadas.
- Protección CSRF (antiforgery) en todos los POST y validación en servidor (ViewModels + DataAnnotations) y en cliente (sin jQuery).
- Rate limiting por IP en login/registro más **bloqueo por cuenta** tras varios intentos fallidos; cabeceras de seguridad (incluida CSP sin `unsafe-inline` en scripts) y logging de eventos.
- Migraciones de esquema con DbUp: se aplican automáticamente al arrancar y quedan registradas en la tabla `schemaversions`.
- UI con Bootstrap 5.3 (sin jQuery) y assets con *cache busting* (`asp-append-version`).
- Endpoint de salud `/health`, app contenerizada (Dockerfile), y analizadores de .NET (`latest-recommended`) aplicados en cada compilación.

## Requisitos

- [SDK de .NET 10](https://dotnet.microsoft.com/download) (el repositorio compila con `net10.0`).
- [Docker](https://www.docker.com/) con Docker Compose, para la base de datos MySQL 8.4.

## Instalación y ejecución

### Opción A — Todo en Docker

Construye y levanta la aplicación (puerto 8080) junto con MySQL:

```bash
docker compose up --build
```

La app arranca en `http://localhost:8080` en modo Producción (sin datos de demostración). Regístrate para crear una cuenta; como el envío de correo está simulado, el enlace de confirmación aparece en los logs: `docker compose logs web`.

### Opción B — Base de datos en Docker + app local (desarrollo)

```bash
# 1. Levantar solo la base de datos
docker compose up -d db

# 2. Ejecutar la aplicación (aplica migraciones y, en Development, inserta datos demo)
dotnet run --project src/Encuestas.Web --urls http://localhost:5090
```

### Usuarios de demostración

En el entorno **Development** (Opción B), si la base está vacía se insertan dos cuentas de prueba **ya confirmadas**:

| Correo               | Contraseña | Nota                                          |
|----------------------|------------|-----------------------------------------------|
| `demo@encuestas.dev` | `demo1234` | Tiene una encuesta de ejemplo con una respuesta |
| `ana@encuestas.dev`  | `ana12345` | Responde la encuesta de la cuenta demo          |

Al registrar una cuenta nueva se requiere confirmar el correo. Con el envío simulado, el enlace de confirmación (y el de restablecimiento) se escriben en el log de la aplicación.

## Configuración

La cadena de conexión se lee de `ConnectionStrings:Default`. En desarrollo viene en `src/Encuestas.Web/appsettings.Development.json` (valores de localhost que coinciden con los del `docker-compose`); en otros entornos defínela con la variable de entorno `ConnectionStrings__Default`:

```bash
ConnectionStrings__Default="Server=mi-servidor;Port=3306;Database=encuestas;User ID=usuario;Password=secreto;"
```

Para no versionar secretos locales, usa *user-secrets* de .NET en lugar de editar `appsettings`:

```bash
dotnet user-secrets --project src/Encuestas.Web set "ConnectionStrings:Default" "Server=...;Password=...;"
```

El endpoint `GET /health` comprueba la conectividad con la base de datos (útil como sonda de *readiness/liveness*).

La base de datos en Docker se personaliza copiando `.env.example` a `.env`:

| Variable              | Descripción                        | Por defecto |
|-----------------------|------------------------------------|-------------|
| `MYSQL_ROOT_PASSWORD` | Contraseña de root de MySQL        | `root`      |
| `MYSQL_DATABASE`      | Nombre de la base de datos         | `encuestas` |
| `MYSQL_USER`          | Usuario de la aplicación           | `encuestas` |
| `MYSQL_PASSWORD`      | Contraseña del usuario             | `encuestas` |
| `MYSQL_PORT`          | Puerto expuesto en el host         | `3306`      |
| `WEB_PORT`            | Puerto de la aplicación web (Opción A) | `8080`  |

Los valores por defecto son solo para desarrollo local.

## Estructura del proyecto

```
src/
  Encuestas.Web/      # ASP.NET Core MVC: controladores, vistas Razor, wwwroot (Bootstrap 5.3, sin jQuery)
    Models/           # ViewModels con validación DataAnnotations
    Services/         # AuthService, PasswordService, TokenService, IEmailSender, AccountLockout, SecurityStampCache
    Infrastructure/   # MigrationRunner (DbUp), DevDataSeeder, DatabaseHealthCheck
    Migrations/       # Scripts SQL versionados, embebidos en el ensamblado
  Encuestas.Data/     # Repositorios ADO.NET async sobre MySqlConnector (RepositoryResult, PagedResult)
  Encuestas.Model/    # Entidades: User, UserProfile, Poll, Answer
tests/
  Encuestas.Tests/    # xUnit: unitarias + integración (trait Category=Integration, Testcontainers y WebApplicationFactory)
Dockerfile            # Imagen multi-stage de la app (usuario no root)
docker-compose.yml    # Servicios web + MySQL 8.4 con volumen persistente y healthcheck
Encuestas.slnx        # Solución (.NET 10)
```

## Pruebas

```bash
# Suite completa (las de integración requieren Docker en ejecución)
dotnet test

# Solo unitarias (rápidas, sin Docker)
dotnet test --filter "Category!=Integration"

# Solo integración (Testcontainers)
dotnet test --filter "Category=Integration"
```

La suite tiene 71 pruebas divididas en dos grupos:

- **56 unitarias** — hashing y re-hash transparente de hashes legados (`PasswordService`), flujo completo de inicio de sesión (`AuthService`: bloqueo, credenciales, confirmación de correo, claims), tokens firmados (`TokenService`), bloqueo por cuenta (`AccountLockout`), caché del sello de seguridad (`SecurityStampCache`), paginación (`PagedResult`) y validación de ViewModels.
- **15 de integración** — marcadas con el trait `Category=Integration`: repositorios contra un MySQL 8.4 efímero (Testcontainers) y extremo a extremo del pipeline HTTP con `WebApplicationFactory`. Requieren Docker en ejecución.

## Integración continua

En cada push y pull request, GitHub Actions ([ci.yml](.github/workflows/ci.yml)) ejecuta tres jobs en paralelo:

| Job                   | Qué hace                                                                        |
|-----------------------|----------------------------------------------------------------------------------|
| `pruebas-unitarias`   | Ejecuta solo las 56 unitarias; da señal rápida y no necesita Docker              |
| `auditoria`           | `dotnet list package --vulnerable`: falla si alguna dependencia (directa o transitiva) tiene vulnerabilidades conocidas |
| `build-e-integracion` | Compila con `-warnaserror` y ejecuta las 15 de integración                       |

Dependabot ([dependabot.yml](.github/dependabot.yml)) revisa mensualmente las dependencias de NuGet, Docker Compose y GitHub Actions.

## Base de datos

El esquema vive en [src/Encuestas.Web/Migrations](src/Encuestas.Web/Migrations) como scripts versionados que DbUp aplica al arrancar (MySQL 8.4, `utf8mb4`, InnoDB, con columnas de auditoría `created_at`/`updated_at`, sello de seguridad, confirmación de correo e índice compuesto para el tablero). Para evolucionar el esquema, agrega un script nuevo (`0002_...sql`) — nunca edites uno ya aplicado:

| Tabla              | Contenido                                           | Relaciones                                              |
|--------------------|-----------------------------------------------------|---------------------------------------------------------|
| `a_users`          | Cuentas: correo, confirmación, hash y sello         | —                                                       |
| `a_users_profiles` | Perfil público: usuario y nombre                    | 1–1 con `a_users` (cascada)                             |
| `a_polls`          | Encuestas: título, descripción, posición            | N–1 con `a_users_profiles` (cascada)                    |
| `a_answers`        | Respuestas: estrellas (1–5) y comentario, únicas por usuario/encuesta | N–1 con `a_polls` y `a_users_profiles` (cascada) |

Para regenerar la base de datos desde cero (las migraciones y los datos demo se reaplican al arrancar la app):

```bash
docker compose down -v && docker compose up -d
```
