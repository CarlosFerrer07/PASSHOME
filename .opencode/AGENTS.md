# PassHome — Contexto para el agente

> 📖 La guía completa de Docker, migraciones y setup está en `docs/GUIA-DOCKER-Y-SETUP.md`
> 📖 La documentación general del proyecto está en `README.md`

## Descripción del proyecto

Gestor de contraseñas personales full-stack. Backend .NET 10 + Frontend Angular 21. Cifrado AES-256-CBC con clave derivada del master password del usuario mediante PBKDF2.

## Stack

| Capa | Tecnología |
|------|-----------|
| Backend | .NET 10 / C# 14 / Minimal API |
| ORM | Entity Framework Core 10 + SQL Server |
| Frontend | Angular 21.2 (standalone, zoneless) |
| Auth | JWT Bearer + PBKDF2 (100k iteraciones) |
| Cifrado | AES-256-CBC con DEK por usuario |
| Contenedores | Docker (SQL Server) |
| Cloud | Azure (previsto) |
| Nodo | Node.js 20 LTS / npm 11 |

## Estructura del proyecto

```
C:\PROYECTOS\PASSHOME\
├── backend\
│   ├── PasswordManager.slnx
│   ├── PasswordManager.Api\          # Minimal API endpoints
│   │   └── Program.cs                 # Todo el código de endpoints
│   ├── PasswordManager.Core\          # Entidades + Interfaces
│   │   ├── Entities\                  # User, PasswordEntry, Category
│   │   └── Interfaces\               # IEncryptionService, IJwtService, IDataKeyService
│   ├── PasswordManager.Infrastructure\ # EF Core + Servicios
│   │   ├── Data\AppDbContext.cs
│   │   └── Services\                  # EncryptionService, JwtService, DataKeyService
│   └── PasswordManager.DTOs\         # Records inmutables
├── frontend\                          # (pendiente — Fase 2)
├── .opencode\AGENTS.md               # ← este archivo
├── README.md                          # Documentación completa
└── docker-compose.yml                 # (pendiente — Fase 4)
```

## Comandos esenciales

```powershell
# Docker — SQL Server
docker start sqlserver
docker stop sqlserver

# Backend — ejecutar
dotnet run --project .\backend\PasswordManager.Api

# Backend — migraciones
dotnet ef migrations add Nombre --project .\backend\PasswordManager.Infrastructure --startup-project .\backend\PasswordManager.Api
dotnet ef database update --project .\backend\PasswordManager.Infrastructure --startup-project .\backend\PasswordManager.Api

# Backend — compilar
dotnet build .\backend\PasswordManager.slnx
```

## Estado actual

- [x] Fase 1: Backend completo (auth, CRUD contraseñas, CRUD categorías, cifrado)
- [ ] Fase 2: Frontend Angular 21
- [ ] Fase 3: Features extra (generador, búsqueda, import/export, copia rápida)
- [ ] Fase 4: Docker Compose + Azure

## API Endpoints

| Método | Ruta | Auth | Body/Params |
|--------|------|------|-------------|
| POST | /api/auth/register | No | { email, masterPassword } |
| POST | /api/auth/login | No | { email, masterPassword } |
| GET | /api/passwords | JWT | — |
| POST | /api/passwords | JWT | { title, username, password, url, categoryId, notes } |
| PUT | /api/passwords/{id} | JWT | { title, username, password?, url, categoryId, notes } |
| DELETE | /api/passwords/{id} | JWT | — |
| GET | /api/passwords/generate | No | ?length=16&includeUpper=true&... |
| GET | /api/categories | JWT | — |
| POST | /api/categories | JWT | { name, icon? } |
| DELETE | /api/categories/{id} | JWT | — |

## Modelo de seguridad

1. **Registro**: Se genera salt + DEK (Data Encryption Key) aleatoria. La DEK se cifra con clave derivada del master password (PBKDF2). Se almacena: hash, salt, DEK cifrada, IV.
2. **Login**: Se verifica el master password, se descifra la DEK en memoria (ConcurrentDictionary singleton — `DataKeyService`).
3. **Contraseñas**: Cada contraseña se cifra con AES-256-CBC usando la DEK. Cada una tiene su propio IV.
4. **JWT**: Token con claims (userId, email), expiración 24h, firmado HMAC-SHA256.

## Convenciones de código

- C# 14: collection expressions (`[]`), target-typed new, primary constructors
- DTOs: `record` inmutables
- Endpoints: Minimal API (sin controllers, todo en Program.cs)
- DI: nativa (AddScoped / AddSingleton)
- EF Core: Fluent API en OnModelCreating
- Naming: PascalCase clases/métodos, camelCase parámetros

## Perfil de ejecución

El proyecto Api tiene dos perfiles en launchSettings.json:
- **http**:  `http://localhost:5249`
- **https**: `https://localhost:7052` / `http://localhost:5249`

## Notas adicionales

- La `appsettings.json` tiene connection string para Docker SQL Server (sa/<TU_CONTRASEÑA>) — cambiar antes de commitear
- La JWT Key es un placeholder (`YourSuperSecretKeyAtLeast32CharactersLong!`) — cambiar en producción
- El `DataKeyService` usa `ConcurrentDictionary` en memoria — los datos se pierden al reiniciar la API (el usuario debe reloguear)
- El frontend Angular 21 usará zoneless change detection, Signals, y Signal Forms (experimental)
