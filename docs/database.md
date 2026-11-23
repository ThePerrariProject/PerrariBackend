# Diccionario de Base de Datos - Perrari

Este documento describe la estructura de las tablas de la base de datos de PerrariBackend en PostgreSQL.

---

## 👥 Tabla: `clients`

Expediente único de clientes de la concesionaria. Registra datos fiscales, contacto y estado de activación para baja lógica.

### Columnas

| Columna | Tipo de Dato | Nulable | Default | Descripción |
|---|---|---|---|---|
| `id` | `UUID` | No | — | Identificador único del cliente (Guid versión 7). |
| `name` | `VARCHAR(255)` | No | — | Nombre completo o razón social del cliente. |
| `rfc` | `VARCHAR(13)` | No | — | Registro Federal de Contribuyentes (normalizado en mayúsculas y sin espacios). |
| `phone` | `VARCHAR(20)` | No | — | Teléfono de contacto (10 dígitos limpios). |
| `email` | `VARCHAR(255)` | No | — | Correo electrónico de contacto (en minúsculas). |
| `is_active` | `BOOLEAN` | No | `TRUE` | Bandera de baja lógica. `TRUE` = Activo, `FALSE` = Desactivado. |
| `created_at` | `TIMESTAMP` | No | — | Fecha y hora UTC de registro. |
| `updated_at` | `TIMESTAMP` | No | — | Fecha y hora UTC de la última modificación. |

### Índices y Restricciones

| Nombre del Índice | Columnas | Tipo | Propósito |
|---|---|---|---|
| `pk_clients` | `id` | Clave Primaria (B-Tree) | Identidad única del registro. |
| `ix_clients_rfc` | `rfc` | Único (B-Tree) | Garantiza unicidad del RFC a nivel de base de datos y optimiza búsquedas exactas. |
| `ix_clients_is_active` | `is_active` | B-Tree | Optimiza el filtrado frecuente por clientes activos (`WHERE is_active = true`). |
| `ix_clients_name` | `name` | B-Tree | Búsquedas y ordenamiento por nombre del cliente. |

### Decisión de Indexación: Búsqueda por Nombre y Trigramas (`pg_trgm`)

1. **Índice B-Tree actual (`ix_clients_name`):**
   - Aprovechado directamente por PostgreSQL para ordenamiento (`ORDER BY name`) y búsquedas por prefijo (`LIKE 'Juan%'`).
   - Mínimo overhead de escritura y almacenamiento.
2. **Evaluación de Trigramas (`pg_trgm`):**
   - Para búsquedas difusas o por texto contenido arbitrario (`ILIKE '%texto%'`), el optimizador de PostgreSQL realiza un *Sequential Scan* en índices B-Tree estándar.
   - En una fase con alto volumen de clientes (>100,000 registros), se recomienda habilitar la extensión `pg_trgm` y crear un índice GIN:
     ```sql
     CREATE EXTENSION IF NOT EXISTS pg_trgm;
     CREATE INDEX ix_clients_name_trgm ON clients USING gin (name gin_trgm_ops);
     ```
   - **Decisión actual para MVP:** Se mantiene el índice B-Tree estándar por simplicidad de migración y portabilidad entre entornos de prueba, cubriendo búsquedas ágiles por RFC (exacto) y nombre combinado con `AsNoTracking()` y paginación acotada (máximo 100 registros).

