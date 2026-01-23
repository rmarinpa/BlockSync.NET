# Quick Start: Configurar BlockSync para Cualquier Base de Datos

## Casos de Uso Comunes

### Caso 1: Oracle (Legacy) → SQL Server (Local)

**Escenario**: Sistema legacy en Oracle con nombres en portugués, sincronizar a SQL Server local con nombres en inglés.

**appsettings.json:**
```json
{
  "DatabaseMappings": {
    "Source": {
      "Provider": "Oracle",
      "ConnectionString": "Data Source=oracle-prod:1521/ORCL;User Id=legacy;Password=pass123;",
      "Tables": {
        "Ventas": {
          "PhysicalName": "TB_VENDAS",
          "Schema": "LEGACY_SCHEMA",
          "Columns": {
            "Id": { "PhysicalName": "ID_VENDA", "DataType": "RAW(16)" },
            "FechaVenta": { "PhysicalName": "DT_VENDA", "DataType": "DATE" },
            "Cliente": { "PhysicalName": "NM_CLIENTE", "DataType": "VARCHAR2(200)" },
            "Producto": { "PhysicalName": "DS_PRODUTO", "DataType": "VARCHAR2(200)" },
            "Monto": { "PhysicalName": "VL_CENTAVOS", "DataType": "NUMBER(18,0)", "Transformation": "CentavosToDecimal" },
            "Periodo": { "PhysicalName": "CD_PERIODO", "DataType": "VARCHAR2(7)" }
          }
        }
      }
    },
    "Destination": {
      "Provider": "SqlServer",
      "ConnectionString": "Server=localhost;Database=BlockSyncLocal;Integrated Security=true;",
      "Tables": {
        "Ventas": {
          "PhysicalName": "Sales",
          "Schema": "dbo",
          "Columns": {
            "Id": { "PhysicalName": "SaleId", "DataType": "UNIQUEIDENTIFIER" },
            "FechaVenta": { "PhysicalName": "SaleDate", "DataType": "DATETIME2" },
            "Cliente": { "PhysicalName": "CustomerName", "DataType": "NVARCHAR(200)" },
            "Producto": { "PhysicalName": "ProductName", "DataType": "NVARCHAR(200)" },
            "Monto": { "PhysicalName": "AmountCents", "DataType": "BIGINT", "Transformation": "CentavosToDecimal" },
            "Periodo": { "PhysicalName": "PeriodId", "DataType": "VARCHAR(7)" }
          }
        },
        "SyncLedger": {
          "PhysicalName": "SyncBlockchainLedger",
          "Schema": "dbo",
          "Columns": {
            "PeriodoId": { "PhysicalName": "BlockPeriod", "DataType": "VARCHAR(7)", "IsPrimaryKey": true },
            "Hash": { "PhysicalName": "BlockHash", "DataType": "CHAR(32)" },
            "Estado": { "PhysicalName": "SyncStatus", "DataType": "VARCHAR(20)" },
            "UltimaSync": { "PhysicalName": "LastSyncTimestamp", "DataType": "DATETIME2" },
            "TotalRegistros": { "PhysicalName": "RecordCount", "DataType": "INT" },
            "SumaMonto": { "PhysicalName": "TotalAmountCents", "DataType": "BIGINT", "Transformation": "CentavosToDecimal" },
            "UltimaAccion": { "PhysicalName": "LastSyncAction", "DataType": "VARCHAR(10)" },
            "CreatedAt": { "PhysicalName": "CreatedTimestamp", "DataType": "DATETIME2" },
            "UpdatedAt": { "PhysicalName": "UpdatedTimestamp", "DataType": "DATETIME2" }
          }
        }
      }
    }
  }
}
```

**NuGet Packages Requeridos:**
```bash
dotnet add package Oracle.ManagedDataAccess.Core
dotnet add package System.Data.SqlClient
```

---

### Caso 2: MySQL (Legacy) → PostgreSQL (Cloud)

**Escenario**: Sistema legacy en MySQL on-premise, sincronizar a PostgreSQL en AWS RDS.

**appsettings.json:**
```json
{
  "DatabaseMappings": {
    "Source": {
      "Provider": "MySQL",
      "ConnectionString": "Server=mysql-legacy;Database=sales_db;Uid=app;Pwd=pass123;",
      "Tables": {
        "Ventas": {
          "PhysicalName": "tbl_sales_transactions",
          "Schema": "",
          "Columns": {
            "Id": { "PhysicalName": "transaction_id", "DataType": "BINARY(16)" },
            "FechaVenta": { "PhysicalName": "transaction_date", "DataType": "DATETIME" },
            "Cliente": { "PhysicalName": "customer_name", "DataType": "VARCHAR(200)" },
            "Producto": { "PhysicalName": "product_desc", "DataType": "VARCHAR(200)" },
            "Monto": { "PhysicalName": "amount_cents", "DataType": "BIGINT", "Transformation": "CentavosToDecimal" },
            "Periodo": { "PhysicalName": "period_code", "DataType": "VARCHAR(7)" }
          }
        }
      }
    },
    "Destination": {
      "Provider": "PostgreSQL",
      "ConnectionString": "Host=my-db.rds.amazonaws.com;Database=blocksync;Username=admin;Password=***;",
      "Tables": {
        "Ventas": {
          "PhysicalName": "sales",
          "Schema": "public",
          "Columns": {
            "Id": { "PhysicalName": "id", "DataType": "UUID" },
            "FechaVenta": { "PhysicalName": "sale_date", "DataType": "TIMESTAMP" },
            "Cliente": { "PhysicalName": "customer", "DataType": "VARCHAR(200)" },
            "Producto": { "PhysicalName": "product", "DataType": "VARCHAR(200)" },
            "Monto": { "PhysicalName": "amount_cents", "DataType": "BIGINT", "Transformation": "CentavosToDecimal" },
            "Periodo": { "PhysicalName": "period", "DataType": "VARCHAR(7)" }
          }
        },
        "SyncLedger": {
          "PhysicalName": "sync_ledger",
          "Schema": "public",
          "Columns": {
            "PeriodoId": { "PhysicalName": "period", "DataType": "VARCHAR(7)", "IsPrimaryKey": true },
            "Hash": { "PhysicalName": "block_hash", "DataType": "CHAR(32)" },
            "Estado": { "PhysicalName": "status", "DataType": "VARCHAR(20)" },
            "UltimaSync": { "PhysicalName": "last_sync", "DataType": "TIMESTAMP" },
            "TotalRegistros": { "PhysicalName": "record_count", "DataType": "INTEGER" },
            "SumaMonto": { "PhysicalName": "total_cents", "DataType": "BIGINT", "Transformation": "CentavosToDecimal" },
            "UltimaAccion": { "PhysicalName": "action", "DataType": "VARCHAR(10)" },
            "CreatedAt": { "PhysicalName": "created_at", "DataType": "TIMESTAMP" },
            "UpdatedAt": { "PhysicalName": "updated_at", "DataType": "TIMESTAMP" }
          }
        }
      }
    }
  }
}
```

**NuGet Packages Requeridos:**
```bash
dotnet add package MySql.Data
dotnet add package Npgsql
```

---

### Caso 3: SQL Server → SQL Server (diferentes schemas)

**Escenario**: Ambos en SQL Server pero con diferentes esquemas y nombres de tablas.

**appsettings.json:**
```json
{
  "DatabaseMappings": {
    "Source": {
      "Provider": "SqlServer",
      "ConnectionString": "Server=prod-server;Database=ERP;Integrated Security=true;",
      "Tables": {
        "Ventas": {
          "PhysicalName": "FactVentas",
          "Schema": "Ventas",
          "Columns": {
            "Id": { "PhysicalName": "VentaKey", "DataType": "UNIQUEIDENTIFIER" },
            "FechaVenta": { "PhysicalName": "FechaTransaccion", "DataType": "DATETIME2" },
            "Cliente": { "PhysicalName": "NombreCliente", "DataType": "NVARCHAR(200)" },
            "Producto": { "PhysicalName": "NombreProducto", "DataType": "NVARCHAR(200)" },
            "Monto": { "PhysicalName": "MontoCentavos", "DataType": "BIGINT", "Transformation": "CentavosToDecimal" },
            "Periodo": { "PhysicalName": "PeriodoMensual", "DataType": "VARCHAR(7)" }
          }
        }
      }
    },
    "Destination": {
      "Provider": "SqlServer",
      "ConnectionString": "Server=localhost;Database=BlockSyncWarehouse;Integrated Security=true;",
      "Tables": {
        "Ventas": {
          "PhysicalName": "SalesData",
          "Schema": "Staging",
          "Columns": {
            "Id": { "PhysicalName": "TransactionId", "DataType": "UNIQUEIDENTIFIER" },
            "FechaVenta": { "PhysicalName": "TransactionDate", "DataType": "DATETIME2" },
            "Cliente": { "PhysicalName": "CustomerName", "DataType": "NVARCHAR(200)" },
            "Producto": { "PhysicalName": "ProductName", "DataType": "NVARCHAR(200)" },
            "Monto": { "PhysicalName": "AmountInCents", "DataType": "BIGINT", "Transformation": "CentavosToDecimal" },
            "Periodo": { "PhysicalName": "MonthPeriod", "DataType": "VARCHAR(7)" }
          }
        },
        "SyncLedger": {
          "PhysicalName": "BlockchainLedger",
          "Schema": "Sync",
          "Columns": {
            "PeriodoId": { "PhysicalName": "PeriodKey", "DataType": "VARCHAR(7)", "IsPrimaryKey": true },
            "Hash": { "PhysicalName": "BlockMD5Hash", "DataType": "CHAR(32)" },
            "Estado": { "PhysicalName": "BlockStatus", "DataType": "VARCHAR(20)" },
            "UltimaSync": { "PhysicalName": "LastSyncDate", "DataType": "DATETIME2" },
            "TotalRegistros": { "PhysicalName": "TotalRecords", "DataType": "INT" },
            "SumaMonto": { "PhysicalName": "TotalAmountCents", "DataType": "BIGINT", "Transformation": "CentavosToDecimal" },
            "UltimaAccion": { "PhysicalName": "SyncAction", "DataType": "VARCHAR(10)" },
            "CreatedAt": { "PhysicalName": "CreatedDate", "DataType": "DATETIME2" },
            "UpdatedAt": { "PhysicalName": "ModifiedDate", "DataType": "DATETIME2" }
          }
        }
      }
    }
  }
}
```

---

## Herramienta CLI para Generar Configuración Automática

Crea un comando que inspeccione una base de datos existente y genere la configuración JSON:

```bash
dotnet run -- generate-mapping \
  --provider Oracle \
  --connection "Data Source=legacy:1521/PROD;User Id=app;Password=***;" \
  --table TB_VENDAS \
  --schema LEGACY_SCHEMA \
  --output mappings/oracle-legacy.json
```

**Salida generada:**
```json
{
  "PhysicalName": "TB_VENDAS",
  "Schema": "LEGACY_SCHEMA",
  "Columns": {
    "Id": { "PhysicalName": "ID_VENDA", "DataType": "RAW(16)", "Detected": true },
    "FechaVenta": { "PhysicalName": "DT_VENDA", "DataType": "DATE", "Detected": true },
    "Cliente": { "PhysicalName": "NM_CLIENTE", "DataType": "VARCHAR2(200)", "Detected": true },
    "Producto": { "PhysicalName": "DS_PRODUTO", "DataType": "VARCHAR2(200)", "Detected": true },
    "Monto": { "PhysicalName": "VL_CENTAVOS", "DataType": "NUMBER(18,0)", "Transformation": "CentavosToDecimal", "Detected": true },
    "Periodo": { "PhysicalName": "CD_PERIODO", "DataType": "VARCHAR2(7)", "Detected": true }
  },
  "DetectedIndexes": [
    { "Name": "IDX_VENDAS_PERIODO", "Columns": ["CD_PERIODO"], "IsUnique": false },
    { "Name": "PK_VENDAS", "Columns": ["ID_VENDA"], "IsUnique": true }
  ]
}
```

---

## Validación de Configuración al Startup

```csharp
// En Program.cs
var mappingConfig = builder.Configuration
    .GetSection("DatabaseMappings")
    .Get<DatabaseMappingConfiguration>();

// Validar que la configuración es correcta
var validator = new MappingConfigValidator();
var validationResult = validator.Validate(mappingConfig);

if (!validationResult.IsValid)
{
    Console.WriteLine("❌ Errores en configuración de mapeo:");
    foreach (var error in validationResult.Errors)
    {
        Console.WriteLine($"   - {error}");
    }
    Environment.Exit(1);
}

Console.WriteLine("✅ Configuración de mapeo validada correctamente");
```

**Ejemplo de validaciones:**
- Todas las columnas requeridas están mapeadas (Id, Periodo, Monto, etc.)
- Los nombres físicos no están vacíos
- Los providers son soportados
- Las connection strings son válidas
- Las transformaciones referenciadas existen

---

## Testing con Configuración Mock

```csharp
public class GenericRepositoryTests
{
    [Fact]
    public async Task Should_Map_Oracle_To_Domain_Entity()
    {
        // Arrange
        var mockConfig = new DatabaseSystemConfig
        {
            Provider = "Oracle",
            Tables = new Dictionary<string, TableMapping>
            {
                ["Ventas"] = new TableMapping
                {
                    PhysicalName = "TB_VENDAS",
                    Columns = new Dictionary<string, ColumnMapping>
                    {
                        ["Id"] = new() { PhysicalName = "ID_VENDA", DataType = "RAW(16)" },
                        ["Periodo"] = new() { PhysicalName = "CD_PERIODO", DataType = "VARCHAR2(7)" },
                        ["Monto"] = new() { PhysicalName = "VL_CENTAVOS", DataType = "NUMBER(18,0)", Transformation = "CentavosToDecimal" }
                    }
                }
            }
        };

        var transformations = new Dictionary<string, TransformationConfig>
        {
            ["CentavosToDecimal"] = new() { Read = "value / 100m", Write = "(long)(value * 100)" }
        };

        var repository = new GenericSyncRepository<Venta>(
            mockConfig,
            "Ventas",
            transformations
        );

        // Act
        var blockHeaders = await repository.GetBlockHeadersAsync();

        // Assert
        Assert.NotEmpty(blockHeaders);
    }
}
```

---

## Migración Paso a Paso

### Paso 1: Identificar Esquema Source

```sql
-- Oracle
SELECT table_name, column_name, data_type, data_length
FROM all_tab_columns
WHERE owner = 'LEGACY_SCHEMA'
  AND table_name = 'TB_VENDAS'
ORDER BY column_id;
```

### Paso 2: Identificar Esquema Destination

```sql
-- SQL Server
SELECT
    c.TABLE_SCHEMA,
    c.TABLE_NAME,
    c.COLUMN_NAME,
    c.DATA_TYPE,
    c.CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE c.TABLE_NAME = 'Sales'
ORDER BY c.ORDINAL_POSITION;
```

### Paso 3: Crear Mapeo en appsettings.json

Usar la estructura del documento principal `ARCHITECTURE_FLEXIBLE_DB.md`.

### Paso 4: Configurar Program.cs

```csharp
var mappingConfig = builder.Configuration
    .GetSection("DatabaseMappings")
    .Get<DatabaseMappingConfiguration>();

builder.Services.AddScoped<ISyncSource>(sp =>
    new GenericSyncRepository<Venta>(
        mappingConfig.Source,
        "Ventas",
        mappingConfig.DataTransformations
    )
);

builder.Services.AddScoped<ISyncDestination>(sp =>
    new GenericSyncRepository<Venta>(
        mappingConfig.Destination,
        "Ventas",
        mappingConfig.DataTransformations
    )
);
```

### Paso 5: Ejecutar y Validar

```bash
dotnet run

# Verificar que la app inicia correctamente
curl http://localhost:5000/api/sync/status

# Ejecutar sincronización inicial
curl -X POST http://localhost:5000/api/sync
```

---

## Ventajas vs AutoMapper

| Aspecto | AutoMapper | Nuestro Sistema |
|---------|-----------|-----------------|
| **Licencia** | Comercial en algunos casos | Open source, gratis |
| **Performance** | Reflexión en runtime | Mapeo directo optimizado |
| **Configuración** | Code-based (C#) | JSON declarativo |
| **Flexibilidad** | Requiere profiles y configuración compleja | Simple key-value mapping |
| **Database-aware** | No entiende de bases de datos | Incluye query builders para cada provider |
| **Validación** | Manual | Automática al startup |
| **Curva de aprendizaje** | Alta (muchos features) | Baja (solo JSON) |

---

## Próximos Pasos Implementación

1. ✅ Diseño arquitectónico completo
2. ⬜ Implementar `DatabaseMappingConfiguration.cs`
3. ⬜ Implementar `EntityMapper<TEntity>.cs`
4. ⬜ Implementar `DynamicQueryBuilder.cs` con todos los providers
5. ⬜ Implementar `GenericSyncRepository<TEntity>.cs`
6. ⬜ Crear `MappingConfigValidator.cs`
7. ⬜ Crear herramienta CLI `generate-mapping`
8. ⬜ Testing completo con diferentes providers
9. ⬜ Documentación y ejemplos

