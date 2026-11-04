# Arquitectura de BackendPerrari

Este documento explica **cómo está organizado el backend y dónde va cada cosa**. Si vas a crear una entidad, un endpoint, una validación o cualquier otra pieza, empieza aquí.

## Índice

1. [Visión general](#1-visión-general)
2. [Regla de dependencias](#2-regla-de-dependencias)
3. [Las 4 capas](#3-las-4-capas)
4. [¿Dónde va...? (consulta rápida)](#4-dónde-va-consulta-rápida)
5. [Mapa detallado de carpetas](#5-mapa-detallado-de-carpetas)
6. [Cómo agregar una funcionalidad nueva (paso a paso)](#6-cómo-agregar-una-funcionalidad-nueva-paso-a-paso)
7. [Convenciones de nombres](#7-convenciones-de-nombres)
8. [Aspectos transversales](#8-aspectos-transversales)
9. [Pruebas](#9-pruebas)
10. [Reglas de oro (qué NO hacer)](#10-reglas-de-oro-qué-no-hacer)

---

## 1. Visión general

El backend implementa **Clean Architecture**: el código se divide en capas con responsabilidades separadas, y las dependencias apuntan siempre **hacia adentro**, hacia el dominio. El negocio no depende de frameworks, base de datos ni de HTTP.

```
┌──────────────────────────────────────────────────────────┐
│  Perrari.Api  (4. Presentación)                          │
│  Controllers · Middlewares · OpenAPI                     │
└───────────────┬──────────────────────────┬───────────────┘
                │ consume                  │ inyecta (solo DI en Program.cs)
                ▼                          ▼
┌───────────────────────────┐   ┌──────────────────────────┐
│ Perrari.Application       │◄──│ Perrari.Infrastructure   │
│ (2. Aplicación)           │   │ (3. Infraestructura)     │
│ Servicios · DTOs ·        │   │ EF Core · Repositorios · │
│ Validadores · Interfaces  │   │ AuditInterceptor ·       │
│                           │   │ Seguridad · PostgreSQL   │
└─────────────┬─────────────┘   └─────────────┬────────────┘
              │ usa                           │ implementa interfaces
              ▼                               │ y mapea entidades
┌───────────────────────────┐                 │
│ Perrari.Domain            │◄────────────────┘
│ (1. Dominio)              │
│ Entidades · Value Objects │
│ Eventos · Excepciones     │
└───────────────────────────┘
```

---

## 2. Regla de dependencias

| Proyecto | Puede referenciar a | NO puede referenciar a |
|---|---|---|
| `Perrari.Domain` | nada | Application, Infrastructure, Api, EF Core, ASP.NET |
| `Perrari.Application` | Domain | Infrastructure, Api, EF Core, ASP.NET |
| `Perrari.Infrastructure` | Application (y Domain, por transitividad) | Api |
| `Perrari.Api` | Application e Infrastructure (esta última solo para registrar DI) | — |

Si una referencia rompe esta tabla, algo está en la capa equivocada.

**Cómo funciona el desacoplamiento:** Application define *interfaces* (por ejemplo `IVehiculoRepository`) y Infrastructure las *implementa* (`VehiculoRepository` con EF Core). Application nunca sabe que existe PostgreSQL.

---

## 3. Las 4 capas

### 3.1 `Perrari.Domain`: el núcleo del negocio

**Qué es:** las reglas del negocio puras. Sin dependencias externas.

**Aquí va:**
- Entidades (`Vehiculo`, `Cliente`, `Venta`, `Usuario`, `BitacoraAuditoria`) con su comportamiento (métodos como `Vehiculo.BloquearParaVenta(...)`, `Venta.Cerrar()`).
- Value Objects (`Rfc`, `Vin`, `Dinero`) que se validan al construirse.
- Enums del negocio (`EstadoVehiculo`, `EstadoVenta`, `RolSistema`).
- Eventos de dominio (`VentaCerradaEvent`).
- Excepciones de dominio (`DomainException`, `VehiculoNoDisponibleException`).

**Aquí NO va:** atributos de EF Core o de validación (`[Key]`, `[Required]`), DTOs, acceso a datos, nada de HTTP.

### 3.2 `Perrari.Application`: los casos de uso

**Qué es:** orquesta el negocio. Recibe DTOs, usa entidades del dominio y habla con el exterior solo a través de interfaces.

**Aquí va:**
- Servicios / casos de uso (`VentaService`, `ClienteService`...).
- Interfaces de repositorios y de servicios técnicos (`IVentaRepository`, `IUnitOfWork`, `IJwtTokenGenerator`, `IPasswordHasher`, `ICurrentUserService`).
- DTOs de entrada y salida.
- Validadores de entrada (FluentValidation).
- Excepciones de aplicación (`NotFoundException`, `ConflictException`, `ForbiddenException`).
- Mapeos entidad ↔ DTO.
- `DependencyInjection.cs` con `AddApplication()`.

**Aquí NO va:** `DbContext`, `HttpContext`, código de EF Core, implementaciones concretas de repositorios.

### 3.3 `Perrari.Infrastructure`: los detalles técnicos

**Qué es:** todo lo que toca el mundo exterior: base de datos, criptografía, tokens.

**Aquí va:**
- `PerrariDbContext` y las configuraciones Fluent API (`IEntityTypeConfiguration<T>`).
- Repositorios concretos y `UnitOfWork`.
- `AuditInterceptor` (`SaveChangesInterceptor`).
- Servicios de seguridad: `JwtTokenGenerator`, `PasswordHasher`, `JwtSettings`.
- Migraciones y datos semilla.
- `DependencyInjection.cs` con `AddInfrastructure(IConfiguration)`.

**Aquí NO va:** lógica de negocio, referencias a controllers ni a `HttpContext`.

### 3.4 `Perrari.Api`: la entrada HTTP

**Qué es:** expone el sistema como API REST. Es una capa **delgada**: recibe la petición, llama a un servicio de Application y devuelve la respuesta.

**Aquí va:**
- Controllers REST con sus atributos `[Authorize(Roles = "...")]`.
- Middlewares (manejo global de excepciones).
- `CurrentUserService` (implementa `ICurrentUserService` usando `HttpContext`: usuario e IP).
- Configuración de JWT Bearer, OpenAPI/Scalar y CORS (en `Extensions/`).
- `Program.cs`, `appsettings*.json`.

**Aquí NO va:** reglas de negocio, consultas a la base de datos, uso directo de `DbContext` o de entidades de Domain en las respuestas.

---

## 4. ¿Dónde va...? (consulta rápida)

| Quiero crear... | Proyecto | Carpeta |
|---|---|---|
| Una entidad (ej. `Vehiculo`) | Domain | `Entities/<Modulo>/` |
| Una regla de negocio (ej. "no se puede vender un auto ya vendido") | Domain | como método de la entidad |
| Un Value Object (ej. `Rfc`) | Domain | `ValueObjects/` |
| Un enum de negocio (ej. `EstadoVehiculo`) | Domain | `Enums/` |
| Un evento de dominio | Domain | `Events/` |
| Una excepción de negocio | Domain | `Exceptions/` |
| La interfaz de un repositorio | Application | `Interfaces/Repositories/` |
| La interfaz de un servicio (de negocio o técnico) | Application | `Interfaces/Services/` |
| La lógica de un caso de uso | Application | `Services/` |
| Un DTO | Application | `DTOs/<Modulo>/` |
| Un validador de entrada | Application | `Validators/` |
| Un mapeo entidad ↔ DTO | Application | `Mappings/` |
| Una excepción tipo "no encontrado" o "conflicto" | Application | `Common/Exceptions/` |
| Un `Result<T>` o `PagedResult<T>` | Application | `Common/` |
| La configuración de una tabla (índices, relaciones) | Infrastructure | `Persistence/Configurations/` |
| La implementación de un repositorio | Infrastructure | `Persistence/Repositories/` |
| Datos de prueba | Infrastructure | `Persistence/Seed/` |
| Una migración | Infrastructure | `Persistence/Migrations/` (la genera EF) |
| Un interceptor de EF Core | Infrastructure | `Persistence/Interceptors/` |
| Generación de JWT, hash de contraseñas | Infrastructure | `Security/` |
| Un endpoint | Api | `Controllers/` |
| Un middleware | Api | `Middlewares/` |
| Algo que necesita `HttpContext` | Api | `Services/` |
| Configuración de auth, CORS, OpenAPI | Api | `Extensions/` |
| Una prueba | tests | ver [sección 9](#9-pruebas) |

---

## 5. Mapa detallado de carpetas

```
BackendPerrari/
├── BackendPerrari.slnx
├── README.md
├── docs/
│
├── src/
│   ├── Perrari.Domain/
│   │   ├── Common/                 BaseEntity, IDomainEvent
│   │   ├── Entities/
│   │   │   ├── Personal/           Usuario, Rol
│   │   │   ├── Inventarios/        Vehiculo
│   │   │   ├── Clientes/           Cliente
│   │   │   ├── Ventas/             Venta
│   │   │   └── Auditorias/         BitacoraAuditoria
│   │   ├── Enums/
│   │   ├── ValueObjects/
│   │   ├── Events/
│   │   └── Exceptions/
│   │
│   ├── Perrari.Application/
│   │   ├── Interfaces/
│   │   │   ├── Repositories/       IXxxRepository, IUnitOfWork
│   │   │   └── Services/           IXxxService, IJwtTokenGenerator, IPasswordHasher, ICurrentUserService
│   │   ├── Services/               XxxService
│   │   ├── DTOs/
│   │   │   ├── Auth/
│   │   │   ├── Usuarios/
│   │   │   ├── Inventarios/
│   │   │   ├── Clientes/
│   │   │   └── Ventas/
│   │   ├── Validators/
│   │   ├── Common/                 Result, PagedResult
│   │   │   └── Exceptions/         NotFound, Forbidden, Conflict
│   │   ├── Mappings/
│   │   └── DependencyInjection.cs
│   │
│   ├── Perrari.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── Configurations/     XxxConfiguration (Fluent API)
│   │   │   ├── Interceptors/       AuditInterceptor
│   │   │   ├── Repositories/       XxxRepository
│   │   │   ├── Seed/
│   │   │   ├── Migrations/
│   │   │   ├── PerrariDbContext.cs
│   │   │   └── UnitOfWork.cs
│   │   ├── Security/               JwtSettings, JwtTokenGenerator, PasswordHasher
│   │   └── DependencyInjection.cs
│   │
│   └── Perrari.Api/
│       ├── Controllers/
│       ├── Middlewares/            ExceptionHandlingMiddleware
│       ├── Services/               CurrentUserService
│       ├── Extensions/
│       ├── appsettings.json
│       └── Program.cs
│
└── tests/
    ├── Perrari.Domain.Tests/
    ├── Perrari.Application.Tests/
    └── Perrari.Api.IntegrationTests/
```

---

## 6. Cómo agregar una funcionalidad nueva (paso a paso)

Ejemplo: **registrar un cliente** (`POST /api/clientes`). El orden recomendado va de adentro hacia afuera.

### Paso 1: Domain

- Crea o ajusta la entidad en `Entities/Clientes/Cliente.cs` con sus reglas.
- Crea los Value Objects necesarios (`ValueObjects/Rfc.cs`).
- Si hay errores de negocio específicos, agrega la excepción en `Exceptions/`.

### Paso 2: Application

- `Interfaces/Repositories/IClienteRepository.cs`: los métodos que necesitas (`ExisteRfcAsync`, `AgregarAsync`, `ObtenerPorIdAsync`...).
- `DTOs/Clientes/CrearClienteDto.cs` (entrada) y `ClienteDto.cs` (salida).
- `Validators/CrearClienteDtoValidator.cs`: formato de RFC, campos obligatorios, etc.
- `Interfaces/Services/IClienteService.cs` y `Services/ClienteService.cs`: valida, aplica reglas, usa el repositorio y `IUnitOfWork`, devuelve un DTO.
- Registra el servicio y el validador en `DependencyInjection.cs` de Application.

### Paso 3: Infrastructure

- Agrega el `DbSet<Cliente>` en `PerrariDbContext`.
- Crea `Persistence/Configurations/ClienteConfiguration.cs` (nombres de tabla/columna, índices por RFC y nombre, conversión de Value Objects).
- Implementa `Persistence/Repositories/ClienteRepository.cs`.
- Registra el repositorio en `DependencyInjection.cs` de Infrastructure.
- Genera la migración (ver comandos en el [README](../README.md#migraciones-de-ef-core)).

### Paso 4: Api

- Agrega el endpoint en `Controllers/ClientesController.cs`. Inyecta `IClienteService`, protege con `[Authorize(Roles = "...")]` y devuelve DTOs.

### Paso 5: Pruebas

- Prueba unitaria de las reglas del Domain y del servicio en Application. Prueba de integración del endpoint si aplica.

### Ejemplo mínimo de un controller

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClientesController(IClienteService clienteService) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Administrador,AsesorVentas")]
    public async Task<ActionResult<ClienteDto>> Crear(CrearClienteDto dto, CancellationToken ct)
    {
        var cliente = await clienteService.CrearAsync(dto, ct);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = cliente.Id }, cliente);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClienteDto>> ObtenerPorId(Guid id, CancellationToken ct)
        => Ok(await clienteService.ObtenerPorIdAsync(id, ct));
}
```

El controller no tiene lógica: valida el contrato HTTP, delega y responde.

---

## 7. Convenciones de nombres

| Elemento | Convención | Ejemplo |
|---|---|---|
| Entidad | Singular, PascalCase, en español | `Vehiculo`, `Cliente` |
| Value Object | Nombre del concepto | `Rfc`, `Vin`, `Dinero` |
| Interfaz | Prefijo `I` | `IVentaRepository`, `IVentaService` |
| Servicio de aplicación | `<Entidad>Service` | `VentaService` |
| Repositorio | `<Entidad>Repository` | `VehiculoRepository` |
| Configuración EF | `<Entidad>Configuration` | `VehiculoConfiguration` |
| DTO de entrada | `<Accion><Entidad>Dto` | `CrearVehiculoDto`, `ActualizarPrecioDto` |
| DTO de salida | `<Entidad>Dto` | `VehiculoDto` |
| Validador | `<NombreDelDto>Validator` | `CrearVehiculoDtoValidator` |
| Controller | `<Entidades>Controller` (plural) | `VehiculosController` |
| Evento de dominio | `<Hecho>Event` en pasado | `VentaCerradaEvent` |
| Métodos asíncronos | Sufijo `Async` y `CancellationToken` | `ObtenerPorIdAsync(Guid id, CancellationToken ct)` |
| Rutas HTTP | minúsculas, plural | `api/vehiculos`, `api/clientes` |
| Namespaces | Igual a la ruta de carpetas | `Perrari.Application.Services` |

Los nombres de negocio (entidades, módulos, enums) van en **español**, igual que la documentación del proyecto.

---

## 8. Aspectos transversales

### 8.1 Registro de dependencias (DI)

Cada capa expone un método de extensión en su `DependencyInjection.cs`, y `Program.cs` solo los llama:

```csharp
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
```

Si creas un servicio o repositorio nuevo y no lo registras, la API fallará al arrancar o al resolver la dependencia.

### 8.2 Autenticación y autorización (RBAC)

- Login emite un **JWT**. Lo genera `JwtTokenGenerator` (Infrastructure) a través de la interfaz `IJwtTokenGenerator` (Application).
- La validación del token se configura en Api (`Extensions/`) con `JwtBearer`.
- Roles del MVP: `Administrador`, `AsesorVentas`, `EncargadoInventario`.
- La restricción por rol se declara en el controller con `[Authorize(Roles = "...")]`. Principio de **mínimo privilegio**: cada endpoint solo permite los roles que lo necesitan.
- Las contraseñas se guardan con hash (`IPasswordHasher`), nunca en texto plano.

### 8.3 Auditoría

- `AuditInterceptor` (Infrastructure) intercepta `SaveChanges` y registra en `BitacoraAuditoria`: **quién, qué, cuándo y desde qué IP**.
- Para conocer usuario e IP usa `ICurrentUserService` (interfaz en Application, implementación en Api con `HttpContext`). Así Infrastructure no depende de HTTP.
- La bitácora es **inmutable**: solo se insertan registros, nunca se actualizan ni borran.
- En el MVP se auditan: creación/cancelación de ventas y cambios de precio/estatus de vehículos.

### 8.4 Concurrencia y transacciones (ventas e inventario)

- Un vehículo pasa a `EnProcesoDeVenta` cuando un asesor inicia la venta, con un bloqueo temporal.
- La entidad usa un **token de concurrencia** (en PostgreSQL, vía `xmin`) para que dos asesores no puedan vender el mismo VIN al mismo tiempo; el segundo recibe un conflicto (409).
- El cierre de venta debe ser **atómico (ACID)**: cambiar el vehículo a `Vendido`, crear la venta y registrar la auditoría ocurren en **una sola transacción** (un `SaveChanges` o `IUnitOfWork`).

### 8.5 Manejo de errores

`ExceptionHandlingMiddleware` (Api) convierte las excepciones en respuestas HTTP consistentes:

| Excepción | Código HTTP |
|---|---|
| Error de validación (FluentValidation) | 400 |
| `DomainException` | 422 (o 400) |
| `ForbiddenException` | 403 |
| `NotFoundException` | 404 |
| `ConflictException` / `DbUpdateConcurrencyException` | 409 |
| Cualquier otra | 500 (sin exponer detalles internos) |

Los servicios **lanzan** excepciones; los controllers **no** hacen `try/catch`.

### 8.6 Base de datos

- Cambios de esquema: **solo mediante migraciones** de EF Core, nunca a mano en la base compartida.
- Triggers o vistas materializadas se escriben como SQL dentro de la migración (`migrationBuilder.Sql(...)`) para que queden versionados.
- Los índices (RFC, nombre del cliente, VIN) se declaran en las `Configurations/`.

---

## 9. Pruebas

| Proyecto de pruebas | Qué prueba | Ejemplo |
|---|---|---|
| `Perrari.Domain.Tests` | Reglas de entidades y Value Objects, sin mocks | "Un vehículo `Vendido` no se puede bloquear" |
| `Perrari.Application.Tests` | Servicios y validadores, con repositorios simulados | "`VentaService` rechaza vender un vehículo no disponible" |
| `Perrari.Api.IntegrationTests` | Endpoints completos (autenticación, roles, códigos HTTP) | "Un `AsesorVentas` no puede acceder a `/api/auditorias`" |

Comandos en el [README](../README.md#comandos-del-día-a-día).

---

## 10. Reglas de oro (qué NO hacer)

1. **No** pongas lógica de negocio en controllers. Va en Domain (reglas) o Application (orquestación).
2. **No** inyectes `PerrariDbContext` en controllers ni en Application. Se usa a través de repositorios.
3. **No** devuelvas entidades de Domain en las respuestas de la API. Siempre DTOs.
4. **No** pongas atributos de EF Core ni de validación en las entidades. La configuración va en `Configurations/` y la validación de entrada en `Validators/`.
5. **No** referencies Infrastructure desde Application o Domain.
6. **No** uses `HttpContext` fuera de la capa Api.
7. **No** subas secretos (connection strings con contraseña, `Jwt:Key`) al repositorio. Usa User Secrets o variables de entorno.
8. **No** modifiques ni borres registros de la bitácora de auditoría.
9. **No** edites migraciones ya aplicadas en otros ambientes; crea una nueva.
10. **No** dejes un servicio nuevo sin registrar en el `DependencyInjection.cs` de su capa.

---

## Alcance actual (MVP)

Módulos incluidos: **Personal y Seguridad, Inventarios, Clientes, Ventas y Auditorías**. Refacciones, Compras y Proveedores, Finanzas avanzado y Rentas están planeados para fases posteriores; cuando se implementen se agregan como nuevas subcarpetas por módulo dentro de cada capa, siguiendo esta misma estructura.