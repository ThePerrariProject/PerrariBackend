# Especificación de API - Perrari Backend

---

## 👥 Módulo de Clientes (`/api/clients`)

Gestión y expediente único de clientes de la concesionaria.

### Permisos Requeridos
- `GET /api/clients`: `CLIENTS:READ`
- `GET /api/clients/{id}`: `CLIENTS:READ`
- `POST /api/clients`: `CLIENTS:CREATE`
- `PUT /api/clients/{id}`: `CLIENTS:UPDATE`
- `POST /api/clients/{id}/deactivate`: `CLIENTS:DELETE`
- `POST /api/clients/{id}/reactivate`: `CLIENTS:UPDATE`

---

### Endpoints

#### 1. Listar Clientes (Paginado)
`GET /api/clients?search={texto}&isActive={true|false}&page={1}&pageSize={20}`

- **Parámetros de consulta (opcionales):**
  - `search`: Búsqueda insensible a mayúsculas/minúsculas por nombre o RFC. Escapa caracteres especiales (`%`, `_`, `\`).
  - `isActive`: Filtra por estado activo (`true`) o desactivado (`false`).
  - `page`: Número de página (mínimo 1, por defecto 1).
  - `pageSize`: Elementos por página (1 a 100, por defecto 20).
- **Respuesta (200 OK):**
  ```json
  {
    "items": [
      {
        "id": "018f1a2b-3c4d-7e8f-9a0b-1c2d3e4f5a6b",
        "name": "Juan Perez Hernandez",
        "rfc": "PEHJ900101ABC",
        "phone": "5512345678",
        "email": "juan.perez@example.com",
        "isActive": true,
        "createdAt": "2026-11-23T06:00:00Z"
      }
    ],
    "page": 1,
    "pageSize": 20,
    "totalCount": 1,
    "totalPages": 1
  }
  ```

#### 2. Detalle de Cliente
`GET /api/clients/{id}`

- **Respuesta (200 OK):**
  ```json
  {
    "id": "018f1a2b-3c4d-7e8f-9a0b-1c2d3e4f5a6b",
    "name": "Juan Perez Hernandez",
    "rfc": "PEHJ900101ABC",
    "phone": "5512345678",
    "email": "juan.perez@example.com",
    "isActive": true,
    "createdAt": "2026-11-23T06:00:00Z",
    "updatedAt": "2026-11-23T06:00:00Z"
  }
  ```
- **Error (404 Not Found):** Si el cliente no existe.

#### 3. Registrar Cliente
`POST /api/clients`

- **Cuerpo de petición:**
  ```json
  {
    "name": "Empresa Automotriz SA de CV",
    "rfc": "EAU101010XYZ",
    "phone": "55-9876-5432",
    "email": "contacto@automotriz.com"
  }
  ```
- **Respuesta (201 Created):**
  - Encabezado `Location`: `/api/clients/{id}`
  - Cuerpo: `ClientDetailDto`
- **Errores:**
  - `400 Bad Request`: Formato de RFC inválido, teléfono con menos o más de 10 dígitos, correo no válido o espacios al inicio/final del nombre.
  - `409 Conflict`: Ya existe un cliente con ese RFC (`"Ya existe un cliente con ese RFC."`).

#### 4. Editar Cliente
`PUT /api/clients/{id}`

- **Cuerpo:** Mismos campos que el alta.
- **Respuesta (200 OK):** `ClientDetailDto`.
- **Errores:**
  - `404 Not Found`: Cliente no existe.
  - `409 Conflict`: Cliente desactivado (`"No se puede editar un cliente desactivado."`) o RFC repetido.

#### 5. Baja Lógica (Desactivar)
`POST /api/clients/{id}/deactivate`

- **Respuesta (204 No Content):** Idempotente. Si ya estaba desactivado, responde 204 sin cambios.

#### 6. Reactivación
`POST /api/clients/{id}/reactivate`

- **Respuesta (204 No Content):** Idempotente. Si ya estaba activo, responde 204 sin cambios.

---

### Validación de RFC
- **Persona Física:** 13 caracteres (`4 letras` + `6 dígitos fecha YYMMDD` + `3 caracteres homoclave`).
- **Persona Moral:** 12 caracteres (`3 letras` + `6 dígitos fecha YYMMDD` + `3 caracteres homoclave`).
- Admite letras `Ñ` y `&`.
- Valida que la fecha `YYMMDD` sea una fecha real en el calendario.
- Admite RFCs genéricos del SAT: `XAXX010101000` (público general) y `XEXX010101000` (extranjero).

---

### Auditoría Inmutable
La entidad `Client` implementa `IAuditable`. El `AuditInterceptor` registra de forma automática cualquier creación, edición o cambio de estado en la tabla `audit_logs` con valores anteriores y nuevos en JSONB.

