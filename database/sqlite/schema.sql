-- ============================================================================
-- BlockSync.NET - SQLite Schema (Optimizado)
-- ============================================================================
-- Tabla de ventas para sincronización local con SQLite
-- Compatible con Mac (sin instalación de motor de base de datos)
-- ============================================================================

-- Eliminar tabla si existe (para desarrollo)
DROP TABLE IF EXISTS Ventas;

-- Crear tabla Ventas con tipos optimizados
CREATE TABLE Ventas (
    Id BLOB PRIMARY KEY,                    -- GUID como 16 bytes (más eficiente que TEXT)
    FechaVenta TEXT NOT NULL,               -- Fecha en formato ISO 8601 (yyyy-MM-dd)
    Cliente TEXT NOT NULL,                  -- Nombre del cliente
    Producto TEXT NOT NULL,                 -- Nombre del producto
    MontoCentavos INTEGER NOT NULL CHECK(MontoCentavos >= 0),  -- Monto en centavos (exacto)
    Periodo TEXT NOT NULL                   -- Período en formato yyyy-MM
        CHECK(Periodo GLOB '[0-9][0-9][0-9][0-9]-[0-1][0-9]')
);

-- Índice para consultas por periodo (usado en sincronización)
CREATE INDEX IX_Ventas_Periodo ON Ventas(Periodo, FechaVenta, MontoCentavos);

-- Índice para consultas por fecha
CREATE INDEX IX_Ventas_FechaVenta ON Ventas(FechaVenta);

-- ============================================================================
-- Tabla SyncLedger - Metadata Blockchain para tracking de sincronización
-- ============================================================================
-- Esta tabla actúa como un "blockchain ledger" registrando el estado de cada
-- periodo sincronizado. Permite auditoría completa y detección de anomalías.
-- ============================================================================

DROP TABLE IF EXISTS SyncLedger;

CREATE TABLE SyncLedger (
    PeriodoId TEXT PRIMARY KEY,             -- Periodo en formato yyyy-MM
    Hash TEXT NOT NULL,                      -- Hash MD5 del bloque sincronizado
    Estado TEXT NOT NULL                     -- SINCRONIZADO, CORRUPTO, PENDIENTE, ERROR
        CHECK(Estado IN ('SINCRONIZADO', 'CORRUPTO', 'PENDIENTE', 'ERROR')),
    UltimaSync TEXT NOT NULL,                -- Timestamp ISO 8601 de última sincronización
    TotalRegistros INTEGER NOT NULL,         -- Cantidad de registros en el bloque
    SumaMontoCentavos INTEGER NOT NULL,      -- Suma de montos en centavos
    UltimaAccion TEXT NOT NULL               -- SKIP, INSERT, REPAIR
        CHECK(UltimaAccion IN ('SKIP', 'INSERT', 'REPAIR')),
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),  -- Timestamp de creación
    UpdatedAt TEXT NOT NULL DEFAULT (datetime('now'))   -- Timestamp de actualización
);

-- Índice para consultas por estado
CREATE INDEX IX_SyncLedger_Estado ON SyncLedger(Estado);

-- Índice para consultas por fecha de sincronización
CREATE INDEX IX_SyncLedger_UltimaSync ON SyncLedger(UltimaSync DESC);

-- ============================================================================
-- Notas de implementación (OPTIMIZADO):
-- ============================================================================
-- 1. BLOB para GUIDs: 16 bytes vs ~36 bytes en TEXT (ahorro de 55%)
-- 2. INTEGER para montos: Almacena centavos (monto * 100) para precisión exacta
--    Ej: $123.45 se almacena como 12345 centavos
-- 3. TEXT para fechas: ISO 8601 permite sorting directo
-- 4. CHECK constraints validan integridad de datos
-- 5. GLOB es case-sensitive para validación de formato de periodo
-- ============================================================================

-- ============================================================================
-- PRAGMAs para optimización (ejecutar al conectar)
-- ============================================================================
-- PRAGMA journal_mode=WAL;          -- Write-Ahead Logging para mejor concurrencia
-- PRAGMA synchronous=NORMAL;         -- Balance entre velocidad y seguridad
-- PRAGMA cache_size=-64000;          -- 64MB de cache
-- PRAGMA temp_store=MEMORY;          -- Temporales en memoria
-- PRAGMA mmap_size=30000000000;      -- Memory-mapped I/O
-- ============================================================================
