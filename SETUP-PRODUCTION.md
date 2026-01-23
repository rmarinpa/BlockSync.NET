# BlockSync.NET - Production Setup Guide

Esta guía explica cómo configurar BlockSync.NET para producción con Oracle (source) y SQL Server (destination).

## Prerequisitos

- **.NET 9.0 SDK** o superior
- **Oracle Database** 12c o superior con acceso a la tabla de ventas
- **SQL Server** 2016 o superior (o Azure SQL Database)
- Credenciales con permisos de lectura (Oracle) y lectura/escritura (SQL Server)

## Paso 1: Configurar Bases de Datos

### 1.1 Oracle (Source Database)

Ejecuta el script de creación de schema:

```bash
# Conectar a Oracle
sqlplus username/password@host:port/service

# Ejecutar script
@database/oracle-schema.sql
```

O usando SQL Developer:
1. Abrir `database/oracle-schema.sql`
2. Ejecutar script completo

**Verificar:**
```sql
SELECT COUNT(*) FROM VENTAS;
SELECT TABLE_NAME FROM USER_TABLES WHERE TABLE_NAME = 'VENTAS';
```

### 1.2 SQL Server (Destination Database)

Ejecuta el script de creación de schema:

```bash
# Usando sqlcmd
sqlcmd -S your_server -d your_database -U your_user -P your_password -i database/sqlserver-schema.sql
```

O usando Azure Data Studio / SQL Server Management Studio:
1. Abrir `database/sqlserver-schema.sql`
2. Ejecutar script completo

**Verificar:**
```sql
SELECT COUNT(*) FROM dbo.Ventas;
SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Ventas';
```

## Paso 2: Configurar Connection Strings

### 2.1 Usar User Secrets (Recomendado para Development)

```bash
cd src/BlockSync.API

# Inicializar user secrets
dotnet user-secrets init

# Oracle connection string
dotnet user-secrets set "ConnectionStrings:OracleSource" "User Id=your_oracle_user;Password=YOUR_PASSWORD;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=oracle-host.com)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=YOUR_SERVICE)))"

# SQL Server connection string
dotnet user-secrets set "ConnectionStrings:SqlServerDestination" "Server=sqlserver-host.com;Database=BlockSyncDest;User Id=your_sql_user;Password=YOUR_PASSWORD;TrustServerCertificate=True;Encrypt=True;"
```

### 2.2 Variables de Entorno (Recomendado para Production)

**Linux/Mac:**
```bash
export ConnectionStrings__OracleSource="User Id=...;Password=...;Data Source=..."
export ConnectionStrings__SqlServerDestination="Server=...;Database=...;User Id=...;Password=..."
```

**Windows:**
```powershell
$env:ConnectionStrings__OracleSource = "User Id=...;Password=...;Data Source=..."
$env:ConnectionStrings__SqlServerDestination = "Server=...;Database=...;User Id=...;Password=..."
```

### 2.3 Azure App Service Configuration

En el portal de Azure:
1. Ir a **Configuration** → **Application Settings**
2. Agregar:
   - `ConnectionStrings__OracleSource`
   - `ConnectionStrings__SqlServerDestination`

### 2.4 Docker Secrets

Crear archivos de secreto:
```bash
echo "User Id=...;Password=...;Data Source=..." > oracle_connection.txt
echo "Server=...;Database=...;User Id=...;Password=..." > sqlserver_connection.txt
```

En `docker-compose.yml`:
```yaml
services:
  blocksync-api:
    image: blocksync:latest
    environment:
      ConnectionStrings__OracleSource: ${ORACLE_CONNECTION}
      ConnectionStrings__SqlServerDestination: ${SQLSERVER_CONNECTION}
    secrets:
      - oracle_connection
      - sqlserver_connection
```

## Paso 3: Configurar Parámetros de Base de Datos

Editar `appsettings.json` o usar variables de entorno:

```json
{
  "DatabaseSettings": {
    "OracleTableName": "VENTAS",
    "SqlServerTableName": "Ventas",
    "CommandTimeout": 300,
    "BulkInsertBatchSize": 5000,
    "MaxRetries": 3,
    "RetryDelayMilliseconds": 1000
  }
}
```

**Variables de entorno:**
```bash
export DatabaseSettings__CommandTimeout=300
export DatabaseSettings__BulkInsertBatchSize=5000
```

## Paso 4: Build y Run

### 4.1 Development

```bash
cd src/BlockSync.API
dotnet run
```

La aplicación estará disponible en `http://localhost:5000`.

### 4.2 Production Build

```bash
# Build en modo Release
dotnet build --configuration Release

# Publicar
dotnet publish --configuration Release --output ./publish

# Ejecutar
cd publish
dotnet BlockSync.API.dll
```

### 4.3 Docker

```bash
# Build imagen
docker build -t blocksync:latest .

# Run container
docker run -d \
  -p 5000:8080 \
  -e ConnectionStrings__OracleSource="..." \
  -e ConnectionStrings__SqlServerDestination="..." \
  --name blocksync \
  blocksync:latest
```

## Paso 5: Verificar Funcionamiento

### 5.1 Health Check

```bash
curl http://localhost:5000/api/sync/status
```

**Respuesta esperada:**
```json
{
  "origen": {
    "totalRegistros": 1234567,
    "totalBloques": 48,
    "periodos": ["2022-01", "2022-02", ...]
  },
  "destino": {
    "totalRegistros": 0,
    "totalBloques": 0,
    "periodos": []
  },
  "estado": "Destino vacío - listo para sincronización inicial"
}
```

### 5.2 Sincronización Inicial

```bash
curl -X POST http://localhost:5000/api/sync
```

**Monitorear logs:**
```bash
tail -f logs/blocksync.log
```

### 5.3 Ver Diagnostics

```bash
curl http://localhost:5000/api/sync/diagnostics
```

## Paso 6: Configurar Logs (Opcional)

### 6.1 File Logging

Agregar `Serilog` para logging a archivo:

```bash
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.File
```

En `Program.cs`:
```csharp
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));
```

En `appsettings.json`:
```json
{
  "Serilog": {
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/blocksync-.log",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

### 6.2 Application Insights (Azure)

```bash
dotnet add package Microsoft.ApplicationInsights.AspNetCore
```

En `appsettings.json`:
```json
{
  "ApplicationInsights": {
    "InstrumentationKey": "your-key-here"
  }
}
```

## Troubleshooting

### Error: Oracle ORA-12154 (TNS:could not resolve the connect identifier)

**Causa:** Connection string mal formateado o servicio inexistente.

**Solución:**
```bash
# Verificar tnsnames.ora
cat $ORACLE_HOME/network/admin/tnsnames.ora

# Probar conexión
tnsping YOUR_SERVICE_NAME

# Usar formato completo en connection string
Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=host)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=service)))
```

### Error: SQL Server Login failed for user

**Causa:** Credenciales incorrectas o usuario sin permisos.

**Solución:**
```sql
-- Verificar usuario
SELECT name FROM sys.server_principals WHERE type = 'S';

-- Otorgar permisos
USE BlockSyncDest;
GRANT SELECT, INSERT, UPDATE, DELETE ON dbo.Ventas TO your_user;
```

### Error: Timeout expired

**Causa:** Query tarda más de `CommandTimeout` segundos.

**Solución:**
```json
{
  "DatabaseSettings": {
    "CommandTimeout": 600
  }
}
```

### Error: Out of Memory durante sincronización

**Causa:** Demasiados registros en memoria.

**Solución:**
```json
{
  "DatabaseSettings": {
    "BulkInsertBatchSize": 1000
  }
}
```

## Performance Tuning

### Oracle

1. **Índices:**
```sql
CREATE INDEX IX_PERIODO ON VENTAS(PERIODO);
EXEC DBMS_STATS.GATHER_TABLE_STATS(USER, 'VENTAS');
```

2. **Parallelismo:**
```sql
ALTER TABLE VENTAS PARALLEL 4;
```

3. **Particionamiento (para tablas grandes >10M):**
```sql
CREATE TABLE VENTAS (
    ...
) PARTITION BY RANGE (FECHA_VENTA) (
    PARTITION p2022 VALUES LESS THAN (TO_DATE('2023-01-01', 'YYYY-MM-DD')),
    PARTITION p2023 VALUES LESS THAN (TO_DATE('2024-01-01', 'YYYY-MM-DD')),
    ...
);
```

### SQL Server

1. **Índices:**
```sql
CREATE NONCLUSTERED INDEX IX_Ventas_Periodo ON dbo.Ventas(Periodo) INCLUDE (Monto);
UPDATE STATISTICS dbo.Ventas;
```

2. **Bulk Insert:**
```csharp
// Ya implementado con SqlBulkCopy en SqlServerRepository
BulkInsertBatchSize = 5000; // Ajustar según memoria disponible
```

3. **Particionamiento (para tablas grandes >10M):**
```sql
CREATE PARTITION FUNCTION PF_Periodo (VARCHAR(7))
AS RANGE RIGHT FOR VALUES ('2022-01', '2023-01', '2024-01');

CREATE PARTITION SCHEME PS_Periodo
AS PARTITION PF_Periodo ALL TO ([PRIMARY]);

CREATE TABLE dbo.Ventas (...) ON PS_Periodo(Periodo);
```

## Monitoring

### Métricas Clave

1. **Tiempo de sincronización:** Monitorear duración de `/api/sync`
2. **Acciones por tipo:** SKIP vs INSERT vs REPAIR
3. **Errores de conexión:** Logs de Oracle/SQL Server
4. **Memoria:** Uso de memoria durante bulk inserts

### Health Check Endpoint

Implementar health check para monitoreo:

```bash
curl http://localhost:5000/health
```

### Alertas Recomendadas

- Tiempo de sync > 30 minutos
- Más de 10% de bloques en REPAIR
- Errores de conexión consecutivos
- Uso de memoria > 80%

## Security Best Practices

1. **Nunca** commitear `secrets.json` a Git
2. Usar **Azure Key Vault** o **AWS Secrets Manager** en producción
3. Configurar **SSL/TLS** para conexiones de base de datos:
   - Oracle: `(PROTOCOL=TCPS)` + certificados
   - SQL Server: `Encrypt=True;TrustServerCertificate=False;` + certificado válido
4. Implementar **rate limiting** en endpoints públicos
5. Usar **service accounts** con permisos mínimos necesarios

## Backup y Disaster Recovery

### Oracle
```bash
# Export table
expdp username/password@service tables=VENTAS directory=DATA_PUMP_DIR dumpfile=ventas_$(date +%Y%m%d).dmp
```

### SQL Server
```sql
-- Full backup
BACKUP DATABASE BlockSyncDest TO DISK = 'C:\backup\blocksync_full.bak';

-- Transaction log backup (para point-in-time recovery)
BACKUP LOG BlockSyncDest TO DISK = 'C:\backup\blocksync_log.trn';
```

## Contacto y Soporte

Para problemas o preguntas:
- Issues: https://github.com/your-org/BlockSync.NET/issues
- Documentación: https://github.com/your-org/BlockSync.NET/wiki
