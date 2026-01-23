-- =====================================================
-- BlockSync.NET - SQL Server Destination Database Schema
-- =====================================================
-- Este script crea la tabla de ventas en SQL Server que
-- actúa como destino de sincronización.
-- =====================================================

-- Eliminar tabla si existe (solo para testing)
IF OBJECT_ID('dbo.Ventas', 'U') IS NOT NULL
    DROP TABLE dbo.Ventas;
GO

-- Crear tabla de ventas
CREATE TABLE dbo.Ventas (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    FechaVenta DATETIME2(7) NOT NULL,
    Cliente NVARCHAR(255) NOT NULL,
    Producto NVARCHAR(255) NOT NULL,
    Monto DECIMAL(18,2) NOT NULL,
    Periodo VARCHAR(7) NOT NULL,

    -- Constraint para validar monto no negativo
    CONSTRAINT CK_Ventas_Monto CHECK (Monto >= 0),

    -- Constraint para validar formato de periodo (yyyy-MM)
    CONSTRAINT CK_Ventas_Periodo CHECK (Periodo LIKE '[0-9][0-9][0-9][0-9]-[0-1][0-9]')
);
GO

-- Crear índice en Periodo para optimizar queries de sincronización
CREATE NONCLUSTERED INDEX IX_Ventas_Periodo
ON dbo.Ventas(Periodo)
INCLUDE (FechaVenta, Monto);
GO

-- Crear índice en FechaVenta para queries por rango de fechas
CREATE NONCLUSTERED INDEX IX_Ventas_FechaVenta
ON dbo.Ventas(FechaVenta);
GO

-- =====================================================
-- Script de datos de prueba (OPCIONAL)
-- =====================================================
-- Descomentar para insertar datos de ejemplo
/*
INSERT INTO dbo.Ventas (Id, FechaVenta, Cliente, Producto, Monto, Periodo)
VALUES
    (NEWID(), '2024-01-15 10:30:00', 'Acme Corp', 'Laptop Dell', 1299.99, '2024-01'),
    (NEWID(), '2024-01-20 14:15:00', 'Tech Solutions SA', 'Monitor LG 27"', 350.00, '2024-01'),
    (NEWID(), '2024-02-05 09:00:00', 'Global Systems', 'Teclado Mecánico', 89.99, '2024-02');
GO
*/

-- Verificar creación
SELECT COUNT(*) AS TotalRegistros FROM dbo.Ventas;
GO

-- =====================================================
-- Query de ejemplo para ver estructura de bloques
-- =====================================================
/*
SELECT
    Periodo,
    COUNT(*) AS TotalRegistros,
    SUM(Monto) AS SumaMonto,
    MIN(FechaVenta) AS FechaInicio,
    MAX(FechaVenta) AS FechaFin
FROM dbo.Ventas
GROUP BY Periodo
ORDER BY Periodo;
GO
*/

-- =====================================================
-- Stored Procedure para bulk insert optimizado (OPCIONAL)
-- =====================================================
/*
CREATE OR ALTER PROCEDURE dbo.sp_BulkInsertVentas
    @Ventas dbo.VentasTableType READONLY
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRANSACTION;
    BEGIN TRY
        INSERT INTO dbo.Ventas (Id, FechaVenta, Cliente, Producto, Monto, Periodo)
        SELECT Id, FechaVenta, Cliente, Producto, Monto, Periodo
        FROM @Ventas;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
*/
