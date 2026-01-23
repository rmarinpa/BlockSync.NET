-- ============================================================================
-- BlockSync.NET - SQLite Schema
-- ============================================================================
-- Tabla de ventas para sincronización local con SQLite
-- Compatible con Mac (sin instalación de motor de base de datos)
-- ============================================================================

-- Eliminar tabla si existe (para desarrollo)
DROP TABLE IF EXISTS Ventas;

-- Crear tabla Ventas
CREATE TABLE Ventas (
    Id TEXT PRIMARY KEY,                    -- GUID almacenado como texto
    FechaVenta TEXT NOT NULL,               -- Fecha en formato ISO 8601 (yyyy-MM-dd)
    Cliente TEXT NOT NULL,                  -- Nombre del cliente
    Producto TEXT NOT NULL,                 -- Nombre del producto
    Monto REAL NOT NULL CHECK(Monto >= 0),  -- Monto de la venta (decimal)
    Periodo TEXT NOT NULL                   -- Período en formato yyyy-MM
        CHECK(Periodo GLOB '[0-9][0-9][0-9][0-9]-[0-1][0-9]')
);

-- Índice para consultas por periodo (usado en sincronización)
CREATE INDEX IX_Ventas_Periodo ON Ventas(Periodo, FechaVenta, Monto);

-- Índice para consultas por fecha
CREATE INDEX IX_Ventas_FechaVenta ON Ventas(FechaVenta);

-- ============================================================================
-- Notas de implementación:
-- ============================================================================
-- 1. SQLite no tiene tipo UNIQUEIDENTIFIER nativo, usamos TEXT para GUIDs
-- 2. SQLite no tiene tipo DATE/DATETIME2 nativo, usamos TEXT en formato ISO
-- 3. SQLite no tiene tipo DECIMAL nativo, usamos REAL (64-bit float)
-- 4. Los CHECK constraints validan formato de periodo y monto positivo
-- 5. GLOB es case-sensitive en SQLite (vs LIKE que es case-insensitive)
-- ============================================================================
