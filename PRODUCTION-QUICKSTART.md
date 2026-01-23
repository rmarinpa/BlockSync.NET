# BlockSync.NET - Production Quickstart

Esta rama (`feature/production-oracle-sqlserver`) contiene la implementación production-ready de BlockSync.NET que utiliza:

- **Oracle Database** como fuente de datos históricos
- **SQL Server** como destino de sincronización

## Cambios Principales vs. Branch Main

### 1. Nuevos Repositorios

#### `OracleRepository` (ISyncSource)
- Ubicación: `src/BlockSync.Infrastructure/Repositories/OracleRepository.cs`
- Conecta a Oracle Database usando Oracle.ManagedDataAccess.Core
- Usa Dapper para queries optimizados
- Implementa todos los métodos de `ISyncSource`

#### `SqlServerRepository` (ISyncDestination)
- Ubicación: `src/BlockSync.Infrastructure/Repositories/SqlServerRepository.cs`
- Conecta a SQL Server usando Microsoft.Data.SqlClient
- Usa SqlBulkCopy para inserts masivos de alta performance
- Implementa todos los métodos de `ISyncDestination`

### 2. Scripts de Base de Datos

```
database/
├── oracle-schema.sql         # Script para crear tabla VENTAS en Oracle
├── sqlserver-schema.sql      # Script para crear tabla Ventas en SQL Server
└── README.md                 # Documentación detallada de setup
```

### 3. Configuración

#### appsettings.json
```json
{
  "ConnectionStrings": {
    "OracleSource": "REPLACE_WITH_USER_SECRETS",
    "SqlServerDestination": "REPLACE_WITH_USER_SECRETS"
  },
  "DatabaseSettings": {
    "OracleTableName": "VENTAS",
    "SqlServerTableName": "Ventas",
    "CommandTimeout": 300,
    "BulkInsertBatchSize": 5000
  }
}
```

#### secrets.json.example
Template para configurar user secrets con connection strings reales.

### 4. NuGet Packages Agregados

- `Oracle.ManagedDataAccess.Core` v23.26.100
- `Microsoft.Data.SqlClient` v6.1.4
- `Dapper` v2.1.66
- `Microsoft.Extensions.Configuration.Abstractions` v10.0.2

## Setup Rápido

### Paso 1: Ejecutar Scripts SQL

**Oracle:**
```bash
sqlplus username/password@host:port/service @database/oracle-schema.sql
```

**SQL Server:**
```bash
sqlcmd -S server -d database -U username -P password -i database/sqlserver-schema.sql
```

### Paso 2: Configurar Connection Strings

```bash
cd src/BlockSync.API
dotnet user-secrets init

# Oracle
dotnet user-secrets set "ConnectionStrings:OracleSource" "User Id=your_user;Password=YOUR_PASSWORD;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=your-oracle-host)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=YOUR_SERVICE)))"

# SQL Server
dotnet user-secrets set "ConnectionStrings:SqlServerDestination" "Server=your-sql-server;Database=BlockSyncDest;User Id=your_user;Password=YOUR_PASSWORD;TrustServerCertificate=True;Encrypt=True;"
```

### Paso 3: Build y Run

```bash
dotnet build
cd src/BlockSync.API
dotnet run
```

La aplicación estará disponible en `http://localhost:5000`.

### Paso 4: Verificar

```bash
# Ver estado inicial
curl http://localhost:5000/api/sync/status

# Ejecutar sincronización inicial
curl -X POST http://localhost:5000/api/sync

# Verificar sincronización idempotente (debería ser ~100ms, todo SKIP)
curl -X POST http://localhost:5000/api/sync

# Ver diagnostics
curl http://localhost:5000/api/sync/diagnostics
```

## Arquitectura de Conexión

```
┌─────────────────────────────────────────────────────────┐
│                    BlockSync.NET API                    │
│                  (ASP.NET Core 9.0)                     │
└───────────────────┬────────────────────┬────────────────┘
                    │                    │
            ┌───────▼────────┐   ┌───────▼────────┐
            │ OracleRepository│   │SqlServerRepository│
            │   (ISyncSource) │   │(ISyncDestination) │
            └───────┬────────┘   └───────┬────────┘
                    │                    │
        ┌───────────▼────────┐  ┌────────▼──────────┐
        │  Oracle Database   │  │  SQL Server DB    │
        │  (Read-Only)       │  │  (Read/Write)     │
        │                    │  │                   │
        │  Tabla: VENTAS     │  │  Tabla: Ventas    │
        │  1M+ registros     │  │  Sincronizado     │
        └────────────────────┘  └───────────────────┘
```

## Características Clave

### 1. Hash-Based Sync
- Compara hashes MD5 de bloques mensuales
- Formula: `MD5(SumaMonto|TotalRegistros)`
- Solo descarga bloques que difieren

### 2. Tres Acciones de Sincronización
- **SKIP**: Hash match → No action (99% after initial sync)
- **INSERT**: Missing in destination → Download from Oracle
- **REPAIR**: Hash mismatch → Delete + Re-download

### 3. Performance
- **GetBlockHeadersAsync()**: Solo descarga agregados (COUNT, SUM), no data completa
- **SqlBulkCopy**: Inserts masivos optimizados (5000 records/batch)
- **Dapper**: Queries ligeros sin overhead de EF Core

### 4. Connection Management
- **Scoped lifetime**: Una conexión por HTTP request
- **Connection pooling**: Configurado automáticamente
- **Timeouts**: Configurable via `DatabaseSettings:CommandTimeout`

## Diferencias con POC (Main Branch)

| Aspecto | Main (POC) | This Branch (Production) |
|---------|------------|--------------------------|
| Source | `LegacyRepository` (in-memory, Bogus) | `OracleRepository` (Oracle DB) |
| Destination | `LocalRepository` (in-memory) | `SqlServerRepository` (SQL Server) |
| Data | 1M records generados en startup | Data real desde Oracle |
| Lifetime | Singleton (mantiene estado) | Scoped (por request) |
| Deployment | Dev only | Production-ready |
| Configuration | Hardcoded | secrets.json / env vars |

## Troubleshooting

### Error: "ConnectionString 'OracleSource' no configurado"
**Causa:** User secrets no inicializados.

**Solución:**
```bash
cd src/BlockSync.API
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:OracleSource" "..."
```

### Error: "ORA-12154: TNS:could not resolve the connect identifier"
**Causa:** Connection string mal formateado.

**Solución:** Usar formato completo con DESCRIPTION:
```
User Id=user;Password=pass;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=host)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=service)))
```

### Error: SQL Server Login Failed
**Causa:** Credenciales incorrectas o permisos insuficientes.

**Solución:**
```sql
-- Verificar usuario existe
SELECT name FROM sys.server_principals WHERE name = 'your_user';

-- Otorgar permisos
USE BlockSyncDest;
GRANT SELECT, INSERT, UPDATE, DELETE ON dbo.Ventas TO your_user;
```

### Performance: Sync tarda mucho
**Causa:** Batch size muy grande o timeout muy corto.

**Solución:** Ajustar configuración:
```json
{
  "DatabaseSettings": {
    "CommandTimeout": 600,
    "BulkInsertBatchSize": 1000
  }
}
```

## Testing

### 1. Volver a Modo POC (In-Memory)

Editar `Program.cs`:
```csharp
// Comentar repositorios de producción
// builder.Services.AddScoped<ISyncSource, OracleRepository>();
// builder.Services.AddScoped<ISyncDestination, SqlServerRepository>();

// Descomentar repositorios in-memory
builder.Services.AddSingleton<ISyncSource, LegacyRepository>();
builder.Services.AddSingleton<ISyncDestination, LocalRepository>();
```

### 2. Unit Tests

Los repositorios in-memory siguen disponibles para unit testing sin necesidad de bases de datos reales.

### 3. Integration Tests

Crear tests que usen Docker containers de Oracle/SQL Server:
```bash
docker run -d -p 1521:1521 container-registry.oracle.com/database/express:latest
docker run -d -p 1433:1433 -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourPassword123!" mcr.microsoft.com/mssql/server:2022-latest
```

## Deployment

### Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY publish/ .
ENV ConnectionStrings__OracleSource="..."
ENV ConnectionStrings__SqlServerDestination="..."
ENTRYPOINT ["dotnet", "BlockSync.API.dll"]
```

### Azure App Service

Configurar Application Settings:
- `ConnectionStrings__OracleSource`
- `ConnectionStrings__SqlServerDestination`
- `DatabaseSettings__CommandTimeout`

### Kubernetes

Crear secrets:
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: blocksync-secrets
type: Opaque
stringData:
  oracle-connection: "User Id=...;Password=...;Data Source=..."
  sqlserver-connection: "Server=...;Database=...;User Id=...;Password=..."
```

## Monitoreo

### Métricas Recomendadas
- Tiempo de sincronización por bloque
- Ratio SKIP vs INSERT vs REPAIR
- Errores de conexión a bases de datos
- Uso de memoria durante bulk inserts

### Logs
Los logs incluyen emojis para fácil identificación:
- 🚀 Inicialización
- ✅ Operaciones exitosas
- ⚠️ Warnings
- ❌ Errores
- 📊 Estadísticas
- 📥 Downloads

## Documentación Adicional

- **Setup Completo**: Ver `SETUP-PRODUCTION.md`
- **Scripts SQL**: Ver `database/README.md`
- **Arquitectura**: Ver `CLAUDE.md`
- **API Docs**: Swagger UI en `http://localhost:5000`

## Contacto

Para issues o preguntas sobre esta implementación production-ready, crear un issue en GitHub.

---

**Nota:** Esta rama mantiene compatibilidad completa con la API del POC. Todos los endpoints y respuestas son idénticos, solo cambia el backend de almacenamiento.
