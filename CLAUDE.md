# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**BlockSync.NET** (also known as **MerkleFlow Core**) is a blockchain-inspired data synchronization engine that replaces slow ETL processes with intelligent hash-based sync. It synchronizes only changed data blocks by comparing MD5 hashes (Merkle Tree concept), avoiding redundant full downloads.

**Core Concept:** Instead of downloading all data every time, the system compares block headers (periodo + hash) between source and destination. If hashes match → SKIP. If different → REPAIR. If missing → INSERT.

## Build & Run Commands

### Build Solution
```bash
# From repository root
dotnet build

# Clean build
dotnet clean && dotnet build

# Build specific project
cd src/BlockSync.API
dotnet build
```

### Run Application
```bash
# From API project directory
cd src/BlockSync.API
dotnet run

# Application will start on http://localhost:5000
# Swagger UI available at http://localhost:5000
```

### Restore Dependencies
```bash
dotnet restore
```

### Clean Build Artifacts
```bash
# Remove all bin/obj directories
find . -type d \( -name "bin" -o -name "obj" \) -exec rm -rf {} + 2>/dev/null
```

## Clean Architecture Structure

The project follows **strict Clean Architecture** with dependency flow: API → Infrastructure → Application → Domain

### Layer Dependency Rules

**Domain** (Core) - No dependencies
- Pure business entities and interfaces
- Contains: `Venta` entity, `BlockHeader` value object
- Interfaces: `ISyncSource`, `ISyncDestination`

**Application** - Depends on Domain only
- Business logic and use cases
- `SyncEngine`: Implements the SKIP/INSERT/REPAIR algorithm
- `HashCalculator`: Calculates MD5 hashes using formula: `MD5(SumaMonto + "|" + TotalRegistros)`
- DTOs for all API responses

**Infrastructure** - Depends on Application + Domain
- Concrete implementations of repositories
- `LegacyRepository`: In-memory source system (1M records generated with Bogus)
- `LocalRepository`: In-memory destination system
- `DataGenerator`: Uses Bogus library with **fixed seed 8675309** for reproducible data

**API** - Depends on Infrastructure + Application
- ASP.NET Core Web API with **Controllers** (not Minimal API)
- Single controller: `SyncController` with 6 endpoints
- Dependency injection configured in `Program.cs`

### Critical Architecture Note

**HashCalculator was moved from Infrastructure to Application layer** to avoid circular dependencies. Infrastructure references Application (correct direction for Clean Architecture when Infrastructure needs Application services).

## Synchronization Algorithm

The core sync logic in `SyncEngine.SynchronizeAsync()`:

1. **Fetch block headers** from both source and destination (lightweight metadata)
2. **Compare hashes** for each period:
   - No match in destination → **INSERT** (download full block)
   - Hashes match → **SKIP** (no action, ~99% of cases after initial sync)
   - Hashes differ → **REPAIR** (delete corrupted block + re-download from source)
3. **Generate detailed report** with timing and action counts

### Hash Calculation Formula
```csharp
// Per block (monthly period)
var blockData = $"{sumaMonto}|{totalRegistros}";
var hash = MD5(blockData);
```

This allows integrity verification without downloading actual records.

## Data Generation (Bogus)

The system generates **1,000,000 sales records** in memory using Bogus:
- **Seed:** 8675309 (fixed for reproducibility)
- **Date range:** 2022-01-01 to current date
- **Grouped by:** Monthly periods (49 blocks)
- **Spanish locale:** Company names and product names in Spanish

Located in: `BlockSync.Infrastructure/Services/DataGenerator.cs`

**IMPORTANT:** The constant `TOTAL_RECORDS = 1000000` controls the dataset size.

## API Endpoints

All endpoints are in `SyncController.cs` under route `/api/sync`:

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/status` | Show source/destination state and sync status |
| GET | `/diagnostics` | **Proof of 1M records** - detailed statistics, unique clients/products, random samples |
| POST | `/` | Execute full synchronization (SKIP/INSERT/REPAIR) |
| POST | `/hack/{anio}/{mes}` | Simulate data corruption for testing REPAIR logic |
| POST | `/reset` | Clear destination and restore source to initial state |
| GET | `/hashes` | View hash comparison map (SINCRONIZADO/CORRUPTO/FALTA_EN_DESTINO) |

### Testing the Sync Flow

**Initial Sync:**
```bash
curl http://localhost:5000/api/sync/status        # Verify empty destination
curl -X POST http://localhost:5000/api/sync       # INSERT all 49 blocks
curl http://localhost:5000/api/sync/status        # Verify synchronized
```

**Idempotent Sync (instant):**
```bash
curl -X POST http://localhost:5000/api/sync       # All blocks SKIP (~100-200ms)
```

**Corruption Detection:**
```bash
curl -X POST http://localhost:5000/api/sync/hack/2023/3  # Corrupt March 2023
curl -X POST http://localhost:5000/api/sync              # Auto-detects and REPAIR
```

## Code Conventions

- **Comments:** All XML documentation comments are in Spanish
- **Logging:** Uses emoji prefixes (🚀 ✅ ❌ 🔧 ⏭️) for visual clarity in console
- **Async/Await:** All repository and engine methods are async
- **Thread Safety:** `LocalRepository` uses lock for in-memory list operations
- **Singleton Services:** Repositories registered as `Singleton` in DI to maintain in-memory state

## Key Implementation Details

### Block Header Exchange
Both repositories implement `GetBlockHeadersAsync()` which returns lightweight metadata:
```csharp
List<BlockHeader> {
    Periodo: "2023-03",
    Hash: "abc123...",
    TotalRegistros: 20450,
    SumaMonto: 51234.56
}
```

### Corruption Simulation
`CorruptBlockAsync(periodo)` in `LocalRepository` randomly modifies 10-30% of record amounts in that period, causing hash mismatch.

### Diagnostics Endpoint
The `/diagnostics` endpoint proves the system handles real data by showing:
- Exact count: 1,000,000 records
- 559,372 unique clients (Bogus-generated Spanish company names)
- 2,880 unique products
- Random sample of 10 records (different each call)
- Memory usage metrics
- Per-period distribution stats

## Swagger Configuration

Swagger UI is configured to serve at root URL (`RoutePrefix = string.Empty`) so navigating to `http://localhost:5000` opens the Swagger interface directly.

Swashbuckle.AspNetCore version: 6.5.0 (avoid 10.x due to OpenAPI model compatibility issues with .NET 9).

## Common Patterns

**Adding a new DTO:**
1. Create in `BlockSync.Application/DTOs/`
2. No references needed (DTOs are data containers)
3. Add to controller action signature for Swagger documentation

**Adding a new repository method:**
1. Add to interface in `BlockSync.Domain/Interfaces/`
2. Implement in `BlockSync.Infrastructure/Repositories/`
3. Use in `SyncEngine` or expose via controller

**Modifying sync algorithm:**
- All sync logic is in `SyncEngine.SynchronizeAsync()`
- Action types defined in `SyncActionType` enum (SKIP, INSERT, REPAIR)
- Each block result captured in `BlockSyncResult`
- Final report generated as `SyncReport` with ASCII summary

## Performance Characteristics

- **Initial sync (1M records):** ~5-10 seconds
- **Subsequent sync (all SKIP):** ~100-200 ms (only hash comparison)
- **Memory usage:** ~685 MB for 1M in-memory records
- **49 monthly blocks** spanning 2022-01 to 2026-01

The system demonstrates **O(1) complexity for unchanged historical data** vs O(N) for traditional ETL.

## SQLite Local Edition (Branch: feature/local-sqlite)

Esta implementación permite probar BlockSync.NET localmente en Mac/Linux/Windows sin necesidad de instalar motores de base de datos. Usa SQLite como almacenamiento persistente.

### Diferencias vs PoC In-Memory

| Aspecto | In-Memory (main) | SQLite OPTIMIZADO (feature/local-sqlite) |
|---------|------------------|------------------------------------------|
| Storage | List<Venta> en RAM | 2 archivos: source.db + destination.db |
| Persistencia | No (se pierde al cerrar app) | Sí (datos persisten) |
| Inicialización | Automática al arrancar | Manual via /reset endpoint |
| Performance seed | Inmediato | **22 segundos** para 1M registros (OPTIMIZADO) |
| Performance sync | ~100ms | **126ms** para 1M registros (todos SKIP) |
| Uso de memoria | ~685 MB en RAM | Mínimo (datos en disco) |
| Motor DB requerido | Ninguno | Ninguno (SQLite embebido) |
| GUID storage | 36 bytes (string) | 16 bytes (BLOB) - ahorro 55% |
| Decimal precision | Exacto | Exacto (INTEGER centavos) |
| Hack/Corrupt | Funciona | **Funciona correctamente** (DBs separadas) |

### Arquitectura SQLite (Optimizada)

**Repositorios separados:**
- `SqliteSourceRepository`: Implementa `ISyncSource` - lee de source.db (sistema legacy)
- `SqliteDestinationRepository`: Implementa `ISyncDestination` - escribe en destination.db (sistema local)
- Bases de datos completamente independientes
- Permite probar hack/corrupt/repair correctamente

**Servicios:**
- `SqliteDataSeeder`: Genera e inserta 1M de registros usando `DataGenerator`
- Reutiliza el seed 8675309 para datos reproducibles
- **OPTIMIZADO** con PRAGMAs de alta velocidad

**Schema optimizado:**
- Ubicado en `/database/sqlite/schema.sql`
- **GUIDs como BLOB** (16 bytes vs 36 bytes)
- Fechas en formato ISO 8601 (TEXT)
- **Montos como INTEGER** (centavos para precisión exacta)
- CHECK constraints para validación
- PRAGMAs de optimización documentados

### Comandos SQLite Edition

**Inicializar base de datos:**
```bash
cd src/BlockSync.API
sqlite3 ./data/blocksync.db < ../../database/sqlite/schema.sql
```

**Ejecutar aplicación:**
```bash
cd src/BlockSync.API
dotnet run
```

**Poblar con datos de prueba:**
```bash
curl -X POST http://localhost:5000/api/sync/reset
# Genera 1,000,000 registros en ~2.5 minutos
```

**Verificar datos en SQLite:**
```bash
sqlite3 ./data/blocksync.db "SELECT COUNT(*) FROM Ventas;"
sqlite3 ./data/blocksync.db "SELECT Periodo, COUNT(*) FROM Ventas GROUP BY Periodo LIMIT 5;"
```

**Probar sincronización:**
```bash
# Verificar estado
curl http://localhost:5000/api/sync/status | jq

# Ejecutar sync (debe ser ~700ms con todos SKIP)
curl -X POST http://localhost:5000/api/sync | jq '.resumen'
```

### Tecnologías SQLite

- **Microsoft.Data.Sqlite** 10.0.2 - Driver oficial de Microsoft
- **Dapper** 2.1.66 - Micro-ORM para queries
- **SQLite** - Embebido en el paquete (no requiere instalación separada)
- **Transacciones** - Para integridad en bulk inserts

### Configuración (appsettings.json)

```json
{
  "ConnectionStrings": {
    "SqliteDatabase": "Data Source=./data/blocksync.db"
  },
  "DatabaseSettings": {
    "CommandTimeout": 300,
    "BulkInsertBatchSize": 5000
  }
}
```

### Mejoras Implementadas (Optimizado)

**1. Bases de datos separadas:**
- ✅ `source.db` para origen (sistema legacy read-only)
- ✅ `destination.db` para destino (sistema local sincronizado)
- ✅ **Hack/Corrupt/Repair funciona correctamente**

**2. Schema optimizado:**
- ✅ **BLOB para GUIDs**: 16 bytes vs 36 bytes TEXT (ahorro de 55%)
- ✅ **INTEGER para montos**: Almacena centavos para precisión exacta decimal
- ✅ Sin pérdida de precisión en operaciones monetarias

**3. Performance optimizada:**
- ✅ **Seed 7x más rápido**: 22 segundos vs 2.5 minutos
- ✅ **PRAGMAs de alta velocidad**: WAL mode, PRAGMA synchronous=OFF durante seed
- ✅ **Batch size aumentado**: 10,000 registros por transacción
- ✅ **Cache grande**: 256MB durante seed, 64MB en operación normal

### Ventajas SQLite Edition

✅ Datos persisten entre reinicios de la aplicación
✅ No requiere instalar ningún motor de base de datos
✅ Compatible con Mac, Linux y Windows out-of-the-box
✅ Útil para demos y pruebas sin infraestructura
✅ Sync súper rápido (~126ms para 1M registros con SKIP)
✅ Seed optimizado (~22 segundos para 1M registros)
✅ Schema optimizado con tipos eficientes
✅ Hack/Corrupt/Repair completamente funcional
