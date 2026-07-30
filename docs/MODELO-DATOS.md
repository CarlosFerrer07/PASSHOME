# Modelo de Datos

## PasswordEntry (Entrada de contraseña)

Cada entrada almacena una contraseña de un sitio o servicio.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Id` | int | Identificador único |
| `Title` | string | Nombre del sitio o servicio (ej: "Facebook", "Gmail", "Banco Santander") |
| `Username` | string? | Nombre de usuario o email con el que te registraste en ese sitio |
| `Password` | string | La contraseña (se cifra con AES-256-CBC antes de guardarse) |
| `Url` | string? | Dirección web del sitio (ej: "https://facebook.com") |
| `CategoryId` | int? | Categoría a la que pertenece (ej: Redes Sociales, Banca, Trabajo) |
| `Notes` | string? | Notas adicionales (ej: "Pregunta secreta: color favorito") |
| `CreatedAt` | datetime | Fecha de creación |
| `UpdatedAt` | datetime | Fecha de última modificación |

### Ejemplo

```json
{
  "id": 1,
  "title": "Facebook",
  "username": "carlos@email.com",
  "password": "MiClave123!",
  "url": "https://facebook.com",
  "categoryId": 2,
  "categoryName": "Redes Sociales",
  "notes": "Creada el 2024",
  "createdAt": "2026-07-30T13:52:13Z",
  "updatedAt": "2026-07-30T13:52:13Z"
}
```

## Category (Categoría)

Agrupa contraseñas por tipo.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Id` | int | Identificador único |
| `Name` | string | Nombre (ej: "Redes Sociales", "Banca", "Trabajo") |
| `Icon` | string? | Emoji o icono (ej: "🔒", "🏦") |

## User (Usuario)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Id` | int | Identificador único |
| `Email` | string | Correo electrónico del usuario |
| `MasterPasswordHash` | byte[] | Hash de la contraseña maestra (PBKDF2) |
| `Salt` | byte[] | Salt aleatorio usado en el hash |
| `EncryptedDataKey` | string | DEK (Data Encryption Key) cifrada — solo descifrable con la contraseña maestra |
| `DataKeyIV` | string | IV usado para cifrar la DEK |
| `CreatedAt` | datetime | Fecha de registro |
