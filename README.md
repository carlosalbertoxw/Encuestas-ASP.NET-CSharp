# Encuestas

Aplicación web de encuestas escrita en C# con ASP.NET Core MVC (.NET 10) y MySQL. Cada usuario registrado puede crear encuestas, ordenarlas en su tablero, compartir su perfil público y recibir respuestas de otros usuarios (calificación de 1 a 5 estrellas más comentario).

> Migrada en 2026 desde ASP.NET MVC 5 / .NET Framework 4.8. Ver [Notas de migración](#notas-de-migración).

## Características

- Registro e inicio de sesión con autenticación por cookies (claims de ASP.NET Core).
- Contraseñas con hash PBKDF2 (ASP.NET Core Identity); los hashes SHA1 de la versión anterior se actualizan automáticamente al iniciar sesión.
- Gestión de cuenta: editar nombre, cambiar usuario, correo y contraseña, y eliminar la cuenta (con borrado en cascada de sus datos).
- CRUD de encuestas con posición configurable en el tablero.
- Perfil público por nombre de usuario (`/User/Index?profile=usuario`) con las encuestas de esa persona.
- Respuestas a encuestas: calificación de 1 a 5 estrellas y comentario; el dueño ve todas las respuestas recibidas.
- Protección CSRF (antiforgery) en todos los formularios POST.

## Requisitos

- [SDK de .NET 10](https://dotnet.microsoft.com/download) (el repositorio compila con `net10.0`).
- [Docker](https://www.docker.com/) con Docker Compose, para la base de datos MySQL 8.4.

## Instalación y ejecución

```bash
# 1. Levantar la base de datos (crea el esquema y los datos demo la primera vez)
docker compose up -d

# 2. Ejecutar la aplicación
dotnet run --project src/Encuestas.Web
```

La aplicación queda disponible en la URL que indique la consola (por defecto la del perfil `http` de `src/Encuestas.Web/Properties/launchSettings.json`). Para fijar una URL concreta:

```bash
dotnet run --project src/Encuestas.Web --urls http://localhost:5080
```

### Usuarios de demostración

La base de datos se inicializa con dos cuentas de prueba (solo para desarrollo):

| Correo  | Contraseña | Nota                                                      |
|---------|------------|-----------------------------------------------------------|
| `c@c.c` | `qwerty`   | Tiene una encuesta de ejemplo con una respuesta            |
| `a@a.a` | `123456`   | Conserva hash SHA1 legado; se migra a PBKDF2 al entrar     |

## Configuración

La cadena de conexión se lee de `ConnectionStrings:Default`. En desarrollo viene en `src/Encuestas.Web/appsettings.Development.json`; en otros entornos defínela con la variable de entorno `ConnectionStrings__Default`:

```bash
ConnectionStrings__Default="Server=mi-servidor;Port=3306;Database=encuestas;User ID=usuario;Password=secreto;"
```

La base de datos en Docker se personaliza copiando `.env.example` a `.env`:

| Variable              | Descripción                        | Por defecto |
|-----------------------|------------------------------------|-------------|
| `MYSQL_ROOT_PASSWORD` | Contraseña de root de MySQL        | `root`      |
| `MYSQL_DATABASE`      | Nombre de la base de datos         | `encuestas` |
| `MYSQL_USER`          | Usuario de la aplicación           | `encuestas` |
| `MYSQL_PASSWORD`      | Contraseña del usuario             | `encuestas` |
| `MYSQL_PORT`          | Puerto expuesto en el host         | `3306`      |

Los valores por defecto son solo para desarrollo local.

## Estructura del proyecto

```
src/
  Encuestas.Web/      # ASP.NET Core MVC: controladores, vistas Razor, wwwroot (Bootstrap 3, jQuery)
    Services/         # PasswordService: PBKDF2 + verificación de hashes SHA1 legados
  Encuestas.Data/     # Repositorios ADO.NET async sobre MySqlConnector
  Encuestas.Model/    # Entidades: User, UserProfile, Poll, Answer
db/
  init/               # Scripts SQL que MySQL ejecuta al crear el volumen (esquema + datos demo)
docker-compose.yml    # Servicio MySQL 8.4 con volumen persistente y healthcheck
Encuestas.slnx        # Solución (.NET 10)
```

## Base de datos

Esquema en [db/init/01-schema.sql](db/init/01-schema.sql) (MySQL 8.4, `utf8mb4`, InnoDB):

| Tabla              | Contenido                                   | Relaciones                                              |
|--------------------|---------------------------------------------|---------------------------------------------------------|
| `a_users`          | Cuentas: correo y hash de contraseña        | —                                                       |
| `a_users_profiles` | Perfil público: usuario y nombre            | 1–1 con `a_users` (cascada)                             |
| `a_polls`          | Encuestas: título, descripción, posición    | N–1 con `a_users_profiles` (cascada)                    |
| `a_answers`        | Respuestas: estrellas (1–5) y comentario    | N–1 con `a_polls` y `a_users_profiles` (cascada)        |

Los scripts de `db/init/` solo se ejecutan cuando el volumen se crea por primera vez. Para regenerar la base de datos desde cero:

```bash
docker compose down -v && docker compose up -d
```

## Notas de migración

Migración desde ASP.NET MVC 5 (.NET Framework 4.8, commit anterior en el historial de git):

- **Framework**: ASP.NET Core MVC sobre .NET 10 (multiplataforma; ya no requiere IIS).
- **Acceso a datos**: `MySql.Data` (DLL suelta, credenciales en el código) → `MySqlConnector` async con la cadena de conexión en configuración.
- **Autenticación**: sesión manual (`Session["id"]`) → cookies con claims y atributos `[Authorize]`.
- **Contraseñas**: SHA1 sin salt → PBKDF2 con migración automática al iniciar sesión.
- **Seguridad**: tokens antiforgery en todos los POST; el borrado de encuestas pasó de GET a POST.
- **Esquema**: `utf8` → `utf8mb4`, borrado en cascada, `p_position` a `INT` y `CHECK` de 1–5 en estrellas. El dump original (`DB/Dump20190420.sql`) fue reemplazado por `db/init/`.
- **Funcionalidad nueva**: el flujo de respuestas (`AnswerController`), cuyos enlaces existían en las vistas pero no estaba implementado.

La interfaz (Bootstrap 3.4.1, jQuery 3.3.1 y las validaciones JS originales) se conservó tal cual para mantener el mismo comportamiento visual.
