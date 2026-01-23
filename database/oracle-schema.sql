-- =====================================================
-- BlockSync.NET - Oracle Source Database Schema
-- =====================================================
-- Este script crea la tabla de ventas en Oracle que
-- actúa como fuente de datos históricos.
-- =====================================================

-- Eliminar tabla si existe (solo para testing)
BEGIN
   EXECUTE IMMEDIATE 'DROP TABLE VENTAS';
EXCEPTION
   WHEN OTHERS THEN
      IF SQLCODE != -942 THEN
         RAISE;
      END IF;
END;
/

-- Crear tabla de ventas
CREATE TABLE VENTAS (
    ID RAW(16) DEFAULT SYS_GUID() PRIMARY KEY,
    FECHA_VENTA TIMESTAMP NOT NULL,
    CLIENTE NVARCHAR2(255) NOT NULL,
    PRODUCTO NVARCHAR2(255) NOT NULL,
    MONTO NUMBER(18,2) NOT NULL,
    PERIODO VARCHAR2(7) NOT NULL,
    CONSTRAINT CK_MONTO CHECK (MONTO >= 0)
);

-- Crear índice en PERIODO para optimizar queries de sincronización
CREATE INDEX IX_PERIODO ON VENTAS(PERIODO);

-- Crear índice en FECHA_VENTA para queries por rango de fechas
CREATE INDEX IX_FECHA_VENTA ON VENTAS(FECHA_VENTA);

-- Estadísticas para el optimizador
EXEC DBMS_STATS.GATHER_TABLE_STATS(USER, 'VENTAS');

-- =====================================================
-- Script de datos de prueba (OPCIONAL)
-- =====================================================
-- Descomentar para insertar datos de ejemplo
/*
INSERT INTO VENTAS (ID, FECHA_VENTA, CLIENTE, PRODUCTO, MONTO, PERIODO)
VALUES (
    SYS_GUID(),
    TO_TIMESTAMP('2024-01-15 10:30:00', 'YYYY-MM-DD HH24:MI:SS'),
    'Acme Corp',
    'Laptop Dell',
    1299.99,
    '2024-01'
);

INSERT INTO VENTAS (ID, FECHA_VENTA, CLIENTE, PRODUCTO, MONTO, PERIODO)
VALUES (
    SYS_GUID(),
    TO_TIMESTAMP('2024-01-20 14:15:00', 'YYYY-MM-DD HH24:MI:SS'),
    'Tech Solutions SA',
    'Monitor LG 27"',
    350.00,
    '2024-01'
);

INSERT INTO VENTAS (ID, FECHA_VENTA, CLIENTE, PRODUCTO, MONTO, PERIODO)
VALUES (
    SYS_GUID(),
    TO_TIMESTAMP('2024-02-05 09:00:00', 'YYYY-MM-DD HH24:MI:SS'),
    'Global Systems',
    'Teclado Mecánico',
    89.99,
    '2024-02'
);

COMMIT;
*/

-- Verificar creación
SELECT COUNT(*) AS TOTAL_REGISTROS FROM VENTAS;

-- =====================================================
-- Query de ejemplo para ver estructura de bloques
-- =====================================================
/*
SELECT
    PERIODO,
    COUNT(*) AS TOTAL_REGISTROS,
    SUM(MONTO) AS SUMA_MONTO,
    MIN(FECHA_VENTA) AS FECHA_INICIO,
    MAX(FECHA_VENTA) AS FECHA_FIN
FROM VENTAS
GROUP BY PERIODO
ORDER BY PERIODO;
*/
