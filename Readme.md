# BackendPerrari

API RESTful del **Proyecto Perrari**: sistema integral de gestión para la concesionaria (Ventas, Inventarios, Clientes/CRM, Auditorías y Personal/Seguridad en el MVP).

- **Stack:** ASP.NET Core (C#, .NET 10) · Entity Framework Core · PostgreSQL 15+ · JWT
- **Arquitectura:** Clean Architecture en 4 capas. Antes de escribir código lee [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md), ahí se explica dónde va cada cosa.

---

## Requisitos previos

| Herramienta | Versión | Verificar |
|---|---|---|
| .NET SDK | 10.x | `dotnet --version` |
| PostgreSQL | 15 o superior | `psql --version` |
| Git | cualquiera reciente | `git --version` |
| dotnet-ef (herramienta de migraciones) | misma major que el SDK | `dotnet ef --version` |

Instalar `dotnet-ef` (una sola vez):

```bash
dotnet tool install --global dotnet-ef
```

Confiar en el certificado HTTPS de desarrollo (una sola vez):

```bash
dotnet dev-certs https --trust
```

---

## Primeros pasos

### 1. Clonar e instalar dependencias

```bash
git clone <url-del-repositorio>
cd BackendPerrari
git checkout development

dotnet restore
```

`dotnet restore` descarga todos los paquetes NuGet de la solución. Si agregas un paquete nuevo a un proyecto:

```bash
dotnet add src/<Proyecto> package <NombreDelPaquete>
```

### 2. Configurar secretos locales

Los secretos **nunca** se suben al repositorio. En desarrollo se guardan con *User Secrets* (fuera del repo):

```bash
dotnet user-secrets init --project src/Perrari.Api

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=perrari;Username=postgres;Password=<tu-password>" --project src/Perrari.Api

dotnet user-secrets set "Jwt:Key" "<llave-aleatoria-de-32-o-mas-caracteres>" --project src/Perrari.Api
```

Los valores no secretos (`Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpirationMinutes`) van en `src/Perrari.Api/appsettings.json`.

### 3. Crear / actualizar la base de datos

> Aplica cuando ya exista `PerrariDbContext` y al menos una migración.

```bash
dotnet ef database update --project src/Perrari.Infrastructure --startup-project src/Perrari.Api
```

Si la base `perrari` no existe, EF Core la crea.

### 4. Correr la API

```bash
dotnet run --project src/Perrari.Api
```

El puerto aparece en la consola y en `src/Perrari.Api/Properties/launchSettings.json`.

- Health check: `https://localhost:<puerto>/api/health`
- Documentación interactiva (solo en Development): `https://localhost:<puerto>/scalar`
- Especificación OpenAPI: `https://localhost:<puerto>/openapi/v1.json`

---

## Comandos del día a día

Todos se ejecutan desde la raíz del repo (donde está `BackendPerrari.slnx`).

| Quiero... | Comando |
|---|---|
| Instalar dependencias | `dotnet restore` |
| Compilar toda la solución | `dotnet build` |
| Compilar en Release | `dotnet build -c Release` |
| Correr la API | `dotnet run --project src/Perrari.Api` |
| Correr con recarga automática | `dotnet watch --project src/Perrari.Api` |
| Correr todas las pruebas | `dotnet test` |
| Correr pruebas de un proyecto | `dotnet test tests/Perrari.Domain.Tests` |
| Correr una prueba por nombre | `dotnet test --filter "FullyQualifiedName~NombreDeLaPrueba"` |
| Limpiar binarios | `dotnet clean` |
| Agregar un paquete | `dotnet add src/<Proyecto> package <Paquete>` |
| Listar paquetes desactualizados | `dotnet list package --outdated` |
| Publicar para despliegue | `dotnet publish src/Perrari.Api -c Release -o ./publish` |

### Migraciones de EF Core

Siempre con `--project` (donde viven las migraciones) y `--startup-project` (donde está la configuración):

```bash
# Crear una migración
dotnet ef migrations add NombreDeLaMigracion --project src/Perrari.Infrastructure --startup-project src/Perrari.Api --output-dir Persistence/Migrations

# Aplicar migraciones pendientes
dotnet ef database update --project src/Perrari.Infrastructure --startup-project src/Perrari.Api

# Deshacer la última migración (si NO se ha aplicado)
dotnet ef migrations remove --project src/Perrari.Infrastructure --startup-project src/Perrari.Api

# Volver a una migración anterior
dotnet ef database update NombreDeLaMigracionAnterior --project src/Perrari.Infrastructure --startup-project src/Perrari.Api
```

Las migraciones **sí se versionan** en Git.

---

## Problemas comunes

| Síntoma | Causa probable | Solución |
|---|---|---|
| `The SDK 'Microsoft.NET.Sdk.Web' specified could not be found` o versión incorrecta | No tienes el SDK 10 | Instala .NET SDK 10 y reabre la terminal |
| El navegador marca el HTTPS como inseguro | Certificado de desarrollo sin confiar | `dotnet dev-certs https --trust` |
| `password authentication failed` / `connection refused` | Connection string mal o PostgreSQL apagado | Revisa los User Secrets y que el servicio de PostgreSQL esté corriendo |
| `Unable to create a 'DbContext'` al migrar | Falta `--startup-project` | Usa los comandos de arriba con ambos parámetros |
| `dotnet ef` no se reconoce | Herramienta no instalada | `dotnet tool install --global dotnet-ef` |
| Error 401 en todos los endpoints | Falta el token o `Jwt:Key` distinta a la usada al generarlo | Inicia sesión de nuevo y revisa tus User Secrets |

---

## Flujo de trabajo con Git

Resumen (el detalle está en la documentación del proyecto):

- Ramas permanentes: `main` (producción) y `development` (integración).
- **No se hace push directo** a `main` ni a `development`; todo entra por Pull Request.
- Ramas de trabajo salen de `development` con el formato `tipo/modulo-descripcion_corta`, por ejemplo `feature/ventas-cotizador` o `fix/login-jwt-error`.
- Commits con [Conventional Commits](https://www.conventionalcommits.org/): `feat(ventas): agregar calculo de enganche en cotizador`.
- Los PR a `development` requieren al menos una aprobación y se fusionan con *Squash and Merge*.

---

## Estructura resumida

```
BackendPerrari/
├── docs/                      # Documentación (ARCHITECTURE.md, etc.)
├── src/
│   ├── Perrari.Domain/        # Entidades, reglas de negocio, value objects
│   ├── Perrari.Application/   # Casos de uso, DTOs, validadores, interfaces
│   ├── Perrari.Infrastructure/# EF Core, repositorios, JWT, auditoría
│   └── Perrari.Api/           # Controllers, middlewares, Program.cs
└── tests/                     # Pruebas por capa
```