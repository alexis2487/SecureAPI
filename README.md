# SecureAPI

API REST desarrollada en **ASP.NET Core 8** con autenticación basada en **JWT (JSON Web Tokens)**, desplegada sobre **Windows Server 2022** en una máquina virtual de **Microsoft Azure**, con **IIS** como servidor web de producción.

---

## Tabla de contenidos

- [Descripción general](#descripción-general)
- [Stack tecnológico](#stack-tecnológico)
- [Arquitectura](#arquitectura)
- [Endpoints](#endpoints)
- [Autenticación JWT](#autenticación-jwt)
- [Estructura del proyecto](#estructura-del-proyecto)
- [Configuración local](#configuración-local)
- [Despliegue en Azure](#despliegue-en-azure)
- [Seguridad aplicada](#seguridad-aplicada)
- [Acceso en producción](#acceso-en-producción)

---

## Descripción general

SecureAPI es una API RESTful que implementa un sistema de gestión de usuarios con control de acceso basado en roles. Todos los endpoints de recursos están protegidos mediante autenticación JWT: sin un token válido, el servidor responde con `401 Unauthorized`.

El proyecto simula un entorno corporativo real donde:

- Los clientes se autentican contra un endpoint de login
- Reciben un token firmado con una clave secreta simétrica (HMAC-SHA256)
- Presentan ese token en cada petición posterior mediante el header `Authorization: Bearer <token>`
- El servidor valida el token en cada request antes de procesar cualquier operación

---

## Stack tecnológico

| Componente | Tecnología |
|---|---|
| Framework | ASP.NET Core 8 |
| Lenguaje | C# 12 |
| Autenticación | JWT Bearer (Microsoft.AspNetCore.Authentication.JwtBearer 8.x) |
| Documentación | Swagger / OpenAPI (Swashbuckle) |
| Servidor web | IIS (Internet Information Services) en Windows Server 2022 |
| Infraestructura | Microsoft Azure (VM Standard B1s, East US 2) |
| Control de versiones | Git / GitHub |

---

## Arquitectura

```
Cliente (Swagger / Postman / app)
        │
        ▼
   Azure NSG (firewall)
   Puerto 80 abierto
        │
        ▼
   IIS - Windows Server 2022
   Sitio: SecureAPI
   Ruta física: C:\inetpub\SecureAPI
        │
        ▼
   ASP.NET Core 8 (self-contained)
   ├── AuthController   → /api/auth
   └── UsersController  → /api/users
```

El runtime de .NET está embebido directamente en el binario publicado (`--self-contained true`), lo que significa que el servidor no necesita tener .NET instalado de forma global. IIS actúa como reverse proxy y delega las peticiones al proceso de ASP.NET Core a través del **ASP.NET Core Module v2**.

---

## Endpoints

### Autenticación

| Método | Ruta | Descripción | Autenticación requerida |
|---|---|---|---|
| POST | `/api/auth/login` | Genera un token JWT | No |

**Body de login:**
```json
{
  "username": "admin",
  "password": "admin123"
}
```

**Respuesta exitosa:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

### Usuarios

| Método | Ruta | Descripción | Autenticación requerida |
|---|---|---|---|
| GET | `/api/users` | Lista todos los usuarios | Sí |
| GET | `/api/users/{id}` | Obtiene un usuario por ID | Sí |
| POST | `/api/users` | Crea un nuevo usuario | Sí |
| DELETE | `/api/users/{id}` | Elimina un usuario | Sí |

**Ejemplo de respuesta GET /api/users:**
```json
[
  {
    "id": 1,
    "name": "Alexis Martinez",
    "email": "alexis@empresa.com",
    "role": "admin",
    "createdAt": "2026-05-05T00:43:04.601Z"
  }
]
```

---

## Autenticación JWT

### Flujo completo

```
1. Cliente envía POST /api/auth/login con credenciales
2. Servidor valida credenciales
3. Servidor genera token JWT firmado (expira en 2 horas)
4. Cliente recibe el token
5. Cliente incluye el token en cada petición:
   Authorization: Bearer <token>
6. Servidor valida firma, issuer, audience y expiración
7. Si todo es válido → procesa la petición
8. Si no → 401 Unauthorized
```

### Estructura del token JWT

Un JWT tiene tres partes separadas por puntos: `header.payload.signature`

**Header:**
```json
{
  "alg": "HS256",
  "typ": "JWT"
}
```

**Payload (claims):**
```json
{
  "name": "admin",
  "role": "admin",
  "jti": "c3ea6135-9d38-45fe-a131-3aa7099ccbb4",
  "exp": 1777948868,
  "iss": "SecureAPI",
  "aud": "SecureAPIUsers"
}
```

**Signature:**
Generada con HMAC-SHA256 usando la clave secreta definida en `appsettings.json`.

### Usuarios disponibles

| Username | Password | Rol |
|---|---|---|
| admin | admin123 | admin |
| user | user123 | user |

---

## Estructura del proyecto

```
SecureAPI/
├── Controllers/
│   ├── AuthController.cs       # Endpoint de login y generación de JWT
│   └── UsersController.cs      # CRUD de usuarios protegido con [Authorize]
├── Models/
│   ├── User.cs                 # Modelo de dominio: usuario
│   └── LoginRequest.cs         # DTO para el body del login
├── Properties/
│   └── launchSettings.json     # Configuración de perfiles de ejecución local
├── appsettings.json            # Configuración de JWT (Key, Issuer, Audience)
├── appsettings.Development.json
├── Program.cs                  # Pipeline de middleware y configuración de servicios
└── SecureAPI.csproj            # Definición del proyecto y dependencias NuGet
```

### Responsabilidades por archivo

**Program.cs** — Punto de entrada. Registra servicios (controllers, Swagger, JWT Bearer) y configura el pipeline de middleware en el orden correcto: Swagger → Authentication → Authorization → MapControllers.

**AuthController.cs** — Valida credenciales y construye el token JWT usando `JwtSecurityTokenHandler`. Incluye claims de nombre, rol e identificador único (jti).

**UsersController.cs** — Controlador decorado con `[Authorize]` a nivel de clase, lo que protege automáticamente todos sus endpoints. Los datos viven en una lista estática en memoria.

**appsettings.json** — Contiene la configuración del token: clave secreta, issuer y audience. En producción real, la clave debe estar en variables de entorno o Azure Key Vault, nunca en el repositorio.

---

## Configuración local

### Requisitos previos

- [.NET SDK 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- Visual Studio Code o Visual Studio 2022

### Pasos

```bash
# Clonar el repositorio
git clone https://github.com/alexis2487/SecureAPI.git
cd SecureAPI

# Restaurar dependencias
dotnet restore

# Ejecutar en modo desarrollo
dotnet run
```

La API queda disponible en `http://localhost:{puerto}/swagger`. El puerto exacto lo indica la consola al iniciar.

### Probar autenticación localmente

```bash
# 1. Obtener token
curl -X POST http://localhost:5169/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'

# 2. Usar el token
curl -X GET http://localhost:5169/api/users \
  -H "Authorization: Bearer <token>"
```

---

## Despliegue en Azure

### Infraestructura creada

| Recurso | Tipo | Detalle |
|---|---|---|
| Azureserver | Virtual Machine | Windows Server 2022, Standard B1s |
| Azureserver-ip | IP pública | 52.167.40.61 |
| Azureserver-nsg | Network Security Group | Puerto 80 (HTTP) y 22 (SSH) abiertos |
| Azureserver-vnet | Red virtual | Subred default |

### Proceso de publicación

**1. Publicar como self-contained**

```bash
dotnet publish -c Release -r win-x64 --self-contained true -o C:\publish
```

Esto genera un ejecutable completo que incluye el runtime de .NET. No requiere instalación de .NET en el servidor destino.

**2. Comprimir el output**

```powershell
Compress-Archive -Path C:\publish\* -DestinationPath C:\Users\usuario\Desktop\publish.zip
```

**3. Subir al servidor**

El archivo se transfirió a través de Azure Blob Storage (contenedor con acceso público temporal), y se descargó en el servidor con:

```powershell
Invoke-WebRequest -Uri "https://<storage>.blob.core.windows.net/<contenedor>/publish.zip" -OutFile "C:\publish_new.zip"
```

**4. Desplegar en IIS**

```powershell
# Detener IIS para liberar archivos bloqueados
Stop-Service W3SVC

# Reemplazar archivos
Remove-Item "C:\inetpub\SecureAPI\*" -Recurse -Force
Expand-Archive -Path "C:\publish_new.zip" -DestinationPath "C:\inetpub\SecureAPI" -Force

# Reiniciar IIS y el sitio
Start-Service W3SVC
Import-Module WebAdministration
Start-Website -Name "SecureAPI"
```

**5. Configuración del sitio en IIS**

```powershell
Import-Module WebAdministration
New-Website -Name "SecureAPI" -Port 80 -PhysicalPath "C:\inetpub\SecureAPI" -Force
```

IIS usa el **ASP.NET Core Module v2** (instalado con el Hosting Bundle) para comunicarse con el proceso de Kestrel internamente.

---

## Seguridad aplicada

### Autenticación y autorización
- Todos los endpoints de `/api/users` requieren token JWT válido (`[Authorize]` a nivel de controlador)
- Sin token → `401 Unauthorized` con header `WWW-Authenticate: Bearer`
- Los tokens expiran en 2 horas (`exp` claim)
- Cada token tiene un identificador único (`jti`) para prevenir reutilización

### Validaciones del token
```csharp
ValidateIssuer = true           // Verifica que el token lo emitió este servidor
ValidateAudience = true         // Verifica que el token es para esta aplicación
ValidateLifetime = true         // Rechaza tokens expirados
ValidateIssuerSigningKey = true // Verifica la firma HMAC-SHA256
```

### Consideraciones para producción
- La `Jwt:Key` debe moverse a variables de entorno o **Azure Key Vault**
- Habilitar HTTPS con un certificado TLS válido
- Implementar rate limiting en el endpoint de login para prevenir fuerza bruta
- Reemplazar el almacenamiento en memoria por una base de datos real
- Agregar refresh tokens para evitar que el usuario tenga que re-autenticarse cada 2 horas
- Registrar intentos de autenticación fallidos en un sistema de logging centralizado

---

## Acceso en producción

| Recurso | URL |
|---|---|
| Swagger UI | http://52.167.40.61/swagger |
| Login | POST http://52.167.40.61/api/auth/login |
| Usuarios | GET http://52.167.40.61/api/users |

> **Nota:** El servidor corre sobre HTTP. La implementación de HTTPS con dominio propio y certificado TLS está planificada como siguiente fase del proyecto.

---

## Autor

**Jair Alexis Martinez**  
Ingeniero de Sistemas — Especialización en Seguridad Informática  
[LinkedIn](https://www.linkedin.com/in/) · [GitHub](https://github.com/alexis2487) · [blacktechsec.com](https://www.blacktechsec.com)
