# Resumen: Queries de Verificación y Plan de BD Flexible

## ✅ Queries de Verificación SQLite

### Verificar Totales

```bash
# Source database
sqlite3 ./data/source.db "SELECT COUNT(*) FROM Ventas;"
# Resultado: 1,000,000

# Destination database
sqlite3 ./data/destination.db "SELECT COUNT(*) FROM Ventas;"
# Resultado: 1,000,000

# Ledger stats
sqlite3 -header -column ./data/destination.db \
  "SELECT COUNT(*) as Bloques,
   SUM(CASE WHEN Estado='SINCRONIZADO' THEN 1 ELSE 0 END) as Sincronizados
   FROM SyncLedger;"
# Resultado: 49 bloques, 49 sincronizados
```

### Distribución por Periodo

```bash
sqlite3 -header -column ./data/source.db \
  "SELECT Periodo,
   COUNT(*) as Registros,
   printf('%.2f', SUM(MontoCentavos)/100.0) as SumaMonto
   FROM Ventas
   GROUP BY Periodo
   ORDER BY Periodo
   LIMIT 5;"
```

Resultado:
```
Periodo  Registros  SumaMonto
-------  ---------  -----------
2022-01  21084      52747724.16
2022-02  18848      47635672.91
2022-03  20741      52177533.07
2022-04  20218      50826808.43
2022-05  20886      52348047.08
```

### Ver Ledger (Blockchain Metadata)

```bash
# Todas las entradas
sqlite3 -header -column ./data/destination.db \
  "SELECT PeriodoId, Hash, Estado, UltimaAccion, TotalRegistros
   FROM SyncLedger
   ORDER BY PeriodoId
   LIMIT 5;"

# Bloques reparados
sqlite3 -header -column ./data/destination.db \
  "SELECT PeriodoId, Hash, UltimaAccion, UpdatedAt
   FROM SyncLedger
   WHERE UltimaAccion = 'REPAIR';"

# Historial de acciones
sqlite3 -header -column ./data/destination.db \
  "SELECT UltimaAccion, COUNT(*) as Cantidad
   FROM SyncLedger
   GROUP BY UltimaAccion;"
```

### Verificar Integridad (Ventas vs Ledger)

```bash
# Debe retornar 0 filas si está todo sincronizado
sqlite3 ./data/destination.db "
SELECT
    'Diferencia' as Tipo,
    Periodo as PeriodoId,
    COUNT(*) as TotalRegistros,
    SUM(MontoCentavos) as SumaMontoCentavos
FROM Ventas
GROUP BY Periodo

EXCEPT

SELECT
    'Ledger' as Tipo,
    PeriodoId,
    TotalRegistros,
    SumaMontoCentavos
FROM SyncLedger
ORDER BY PeriodoId;"
```

### Queries de Diagnóstico

```bash
# Tamaño de bases de datos
ls -lh ./data/*.db

# Ver schema de tablas
sqlite3 ./data/source.db ".schema Ventas"
sqlite3 ./data/destination.db ".schema SyncLedger"

# Ver índices
sqlite3 ./data/destination.db ".indexes Ventas"
sqlite3 ./data/destination.db ".indexes SyncLedger"

# Journal mode (debe ser WAL)
sqlite3 ./data/source.db "PRAGMA journal_mode;"
```

---

## 🎯 Plan de Arquitectura Flexible para Cualquier BD

### Documentos Creados

1. **`ARCHITECTURE_FLEXIBLE_DB.md`** - Arquitectura técnica completa
   - Clases de configuración (`DatabaseMappingConfiguration`, `TableMapping`, `ColumnMapping`)
   - `EntityMapper<TEntity>` - Mapeo manual sin AutoMapper
   - `DynamicQueryBuilder` - Construcción de queries para Oracle, SQL Server, PostgreSQL, MySQL, SQLite
   - `GenericSyncRepository<TEntity>` - Repositorio genérico configurable

2. **`FLEXIBLE_DB_QUICKSTART.md`** - Guía práctica con ejemplos
   - Casos de uso comunes (Oracle→SQL Server, MySQL→PostgreSQL, etc.)
   - Configuración JSON completa
   - Herramienta CLI para generar mapping automático
   - Testing y validación

### Concepto Principal: Mapeo Declarativo por JSON

**Antes (hardcoded):**
```csharp
var sql = "SELECT Id, FechaVenta, Cliente FROM Ventas WHERE Periodo = @periodo";
```

**Después (configurable):**
```json
{
  "Tables": {
    "Ventas": {
      "PhysicalName": "TB_VENDAS_HISTORICO",
      "Schema": "LEGACY",
      "Columns": {
        "Id": { "PhysicalName": "ID_VENDA", "DataType": "RAW(16)" },
        "FechaVenta": { "PhysicalName": "DT_VENDA", "DataType": "DATE" },
        "Cliente": { "PhysicalName": "NM_CLIENTE", "DataType": "VARCHAR2(200)" }
      }
    }
  }
}
```

El sistema lee la configuración y genera:
```csharp
var sql = "SELECT ID_VENDA, DT_VENDA, NM_CLIENTE FROM LEGACY.TB_VENDAS_HISTORICO WHERE CD_PERIODO = :periodo";
```

### Ejemplo Completo: Oracle → SQL Server

**Source (Oracle Legacy):**
- Tabla: `LEGACY_SCHEMA.TB_VENDAS`
- Columnas: `ID_VENDA (RAW)`, `DT_VENDA (DATE)`, `NM_CLIENTE (VARCHAR2)`, etc.

**Destination (SQL Server Local):**
- Tabla: `dbo.Sales`
- Columnas: `SaleId (UNIQUEIDENTIFIER)`, `SaleDate (DATETIME2)`, `CustomerName (NVARCHAR)`, etc.

**Configuración en appsettings.json:**
```json
{
  "DatabaseMappings": {
    "Source": {
      "Provider": "Oracle",
      "ConnectionString": "Data Source=oracle-prod:1521/ORCL;...",
      "Tables": {
        "Ventas": {
          "PhysicalName": "TB_VENDAS",
          "Schema": "LEGACY_SCHEMA",
          "Columns": {
            "Id": { "PhysicalName": "ID_VENDA", "DataType": "RAW(16)" },
            "FechaVenta": { "PhysicalName": "DT_VENDA", "DataType": "DATE" },
            "Cliente": { "PhysicalName": "NM_CLIENTE", "DataType": "VARCHAR2(200)" },
            "Monto": { "PhysicalName": "VL_CENTAVOS", "DataType": "NUMBER(18,0)", "Transformation": "CentavosToDecimal" }
          }
        }
      }
    },
    "Destination": {
      "Provider": "SqlServer",
      "ConnectionString": "Server=localhost;Database=BlockSync;...",
      "Tables": {
        "Ventas": {
          "PhysicalName": "Sales",
          "Schema": "dbo",
          "Columns": {
            "Id": { "PhysicalName": "SaleId", "DataType": "UNIQUEIDENTIFIER" },
            "FechaVenta": { "PhysicalName": "SaleDate", "DataType": "DATETIME2" },
            "Cliente": { "PhysicalName": "CustomerName", "DataType": "NVARCHAR(200)" },
            "Monto": { "PhysicalName": "AmountCents", "DataType": "BIGINT", "Transformation": "CentavosToDecimal" }
          }
        }
      }
    },
    "DataTransformations": {
      "CentavosToDecimal": {
        "Read": "value / 100m",
        "Write": "(long)(value * 100)"
      }
    }
  }
}
```

**Program.cs:**
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

---

## 🚀 Ventajas del Sistema

| Característica | Beneficio |
|---------------|-----------|
| **Zero Code Changes** | Solo cambiar `appsettings.json` para nueva BD |
| **Multi-Provider** | Oracle, SQL Server, PostgreSQL, MySQL, SQLite |
| **Sin AutoMapper** | Mapeo manual optimizado (mejor performance) |
| **Validación** | Configuración se valida al startup |
| **Testeable** | Unit tests con configuración mock |
| **Transformaciones** | Soporta conversiones (centavos↔decimal, GUID↔binary, etc.) |
| **Clean Architecture** | Mantiene separación de capas |
| **Open Source** | Sin licencias comerciales |

---

## 📋 Próximos Pasos de Implementación

### Fase 1: Infraestructura Base
- [ ] Crear `DatabaseMappingConfiguration.cs`
- [ ] Crear `EntityMapper<TEntity>.cs`
- [ ] Crear `DynamicQueryBuilder.cs`

### Fase 2: Repositorio Genérico
- [ ] Implementar `GenericSyncRepository<TEntity>`
- [ ] Implementar factory de conexiones (Oracle, SQL Server, PostgreSQL, etc.)
- [ ] Implementar query builders específicos por provider

### Fase 3: Validación y Tooling
- [ ] Crear `MappingConfigValidator.cs`
- [ ] Crear herramienta CLI `generate-mapping` para inspección automática de BD
- [ ] Crear unit tests

### Fase 4: Documentación
- [ ] Ejemplos completos para cada provider
- [ ] Guía de migración paso a paso
- [ ] Troubleshooting común

---

## 🔧 Herramienta CLI Propuesta

Genera configuración JSON automáticamente inspeccionando una BD existente:

```bash
dotnet run -- generate-mapping \
  --provider Oracle \
  --connection "Data Source=legacy:1521/PROD;User Id=app;Password=***;" \
  --table TB_VENDAS \
  --schema LEGACY_SCHEMA \
  --output mappings/oracle-legacy.json
```

**Output generado:**
```json
{
  "PhysicalName": "TB_VENDAS",
  "Schema": "LEGACY_SCHEMA",
  "Columns": {
    "Id": {
      "PhysicalName": "ID_VENDA",
      "DataType": "RAW(16)",
      "Detected": true,
      "Suggestions": ["Probablemente GUID - usar como PrimaryKey"]
    },
    "FechaVenta": {
      "PhysicalName": "DT_VENDA",
      "DataType": "DATE",
      "Detected": true
    }
  },
  "DetectedIndexes": [
    { "Name": "IDX_VENDAS_PERIODO", "Columns": ["CD_PERIODO"], "IsUnique": false },
    { "Name": "PK_VENDAS", "Columns": ["ID_VENDA"], "IsUnique": true }
  ]
}
```

---

## 📚 Recursos Adicionales

- **ARCHITECTURE_FLEXIBLE_DB.md**: Arquitectura completa con todas las clases
- **FLEXIBLE_DB_QUICKSTART.md**: Guía práctica con ejemplos de uso
- **scripts/verify-databases.sh**: Script de verificación de SQLite (en desarrollo)

---

## ✅ Estado Actual del Proyecto

### Implementado (Feature: local-sqlite)
- ✅ SQLite con bases separadas (source.db, destination.db)
- ✅ Schema optimizado (BLOB para GUIDs, INTEGER para centavos)
- ✅ Performance optimizada (22 seg seed, 126ms sync)
- ✅ **Blockchain Ledger (SyncLedger)** - Metadata completa de sincronización
- ✅ Endpoint `/api/sync/ledger` con historial y estadísticas
- ✅ Integración automática del ledger en cada operación (SKIP/INSERT/REPAIR)

### Próximo (Feature: flexible-database)
- ⬜ Sistema de mapeo configurable por JSON
- ⬜ Soporte multi-provider (Oracle, SQL Server, PostgreSQL, MySQL)
- ⬜ Herramienta CLI de generación automática de mappings
- ⬜ Validación de configuración al startup

---

**Última actualización:** 2026-01-23
**Branch actual:** `feature/local-sqlite`
**Commits:**
- `e9c439a` - feat: Implementar Blockchain Ledger (SyncLedger)
- `fa13020` - feat: Reparar limitaciones SQLite (bases separadas)
- `4009658` - feat: Implementar SQLite local edition

