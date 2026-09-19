# GestionTareas API

API REST en .NET 9 - Clean Architecture para gestion de tareas.

## Stack
- Backend: .NET 9 Web API
- Base de datos: SQL Server
- Mensajeria: RabbitMQ + MassTransit
- Secretos produccion: Azure Key Vault
- ORM: Entity Framework Core 8

## Pasos para ejecutar

1. Ejecutar database.sql en SQL Server Management Studio (sa / 12345)
2. Tener RabbitMQ corriendo en localhost:5672 (usuario guest/guest)
3. dotnet run --project src/GestionTareas.API`n4. Swagger UI en http://localhost:5050

## Endpoints

| Metodo | Ruta | Descripcion |
|--------|------|-------------|
| GET | /api/users | Listar usuarios |
| POST | /api/users | Crear usuario |
| GET | /api/tasks | Listar tareas (filtros: usuarioId, estado) |
| POST | /api/tasks | Crear tarea |
| PUT | /api/tasks/{id}/status | Cambiar estado de tarea |

## Azure Key Vault

Configura AzureKeyVault:VaultUri en appsettings.json.
Secretosrequeridos: ConnectionStrings--DefaultConnection, RabbitMQ--Username, RabbitMQ--Password`n
## Reglas de negocio

- Titulo obligatorio.
- Usuario asignado obligatorio.
- No se puede cambiar de Pending a Done directamente.

## JSON en SQL Server

Columna InfoAdicional NVARCHAR(MAX) con constraint ISJSON.
Ver database.sql para ejemplos con JSON_VALUE, JSON_QUERY, OPENJSON, JSON_MODIFY.

## Funcionalidades pendientes

- JWT Authentication
- Paginacion en GET
- Consumers RabbitMQ con logica real
- Tests unitarios e integracion
