# Database Setup Scripts

Este directorio contiene los scripts SQL para configurar las bases de datos de **BlockSync.NET**.

## Arquitectura

- **Oracle** → Fuente de datos históricos (`ISyncSource`)
- **SQL Server** → Destino de sincronización (`ISyncDestination`)

## Scripts Disponibles

### 1. `oracle-schema.sql`
Crea la tabla `VENTAS` en Oracle con:
- Columnas: `ID`, `FECHA_VENTA`, `CLIENTE`, `PRODUCTO`, `MONTO`, `PERIODO`
- Índices en `PERIODO` y `FECHA_VENTA`
- Constraints de validación

**Ejecución:**
```sql
-- En SQL*Plus o SQL Developer
@oracle-schema.sql

-- O usando sqlplus desde terminal
sqlplus username/password@host:port/service @oracle-schema.sql
```

### 2. `sqlserver-schema.sql`
Crea la tabla `Ventas` en SQL Server con:
- Columnas: `Id`, `FechaVenta`, `Cliente`, `Producto`, `Monto`, `Periodo`
- Índices en `Periodo` y `FechaVenta`
- Constraints de validación de formato

**Ejecución:**
```bash
# Usando sqlcmd
sqlcmd -S server_name -d database_name -U username -P password -i sqlserver-schema.sql

# O usando Azure Data Studio / SQL Server Management Studio
# Abrir el archivo y ejecutar
```

## Configuración de Connection Strings

Después de crear las tablas, configura los connection strings en `secrets.json`:

```bash
cd src/BlockSync.API
dotnet user-secrets init
```

### Oracle Connection String
```bash
dotnet user-secrets set "ConnectionStrings:OracleSource" "User Id=your_user;Password=your_password;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=your_host)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=your_service)))"
```

### SQL Server Connection String
```bash
dotnet user-secrets set "ConnectionStrings:SqlServerDestination" "Server=your_server;Database=your_database;User Id=your_user;Password=your_password;TrustServerCertificate=True;Encrypt=True;"
```

## Mapeo de Columnas

| Oracle (Source)     | SQL Server (Destination) | Tipo C#          | Descripción                |
|---------------------|--------------------------|------------------|----------------------------|
| `ID` (RAW(16))      | `Id` (UNIQUEIDENTIFIER)  | `Guid`           | Primary key                |
| `FECHA_VENTA`       | `FechaVenta`             | `DateTime`       | Fecha de venta             |
| `CLIENTE`           | `Cliente`                | `string`         | Nombre del cliente         |
| `PRODUCTO`          | `Producto`               | `string`         | Nombre del producto        |
| `MONTO`             | `Monto`                  | `decimal`        | Monto de la venta          |
| `PERIODO`           | `Periodo`                | `string`         | Periodo "yyyy-MM"          |

## Queries de Sincronización

### Query de Block Headers (Oracle)
```sql
SELECT
    TO_CHAR(FECHA_VENTA, 'YYYY-MM') AS Periodo,
    COUNT(*) AS TotalRegistros,
    SUM(MONTO) AS SumaMonto
FROM VENTAS
GROUP BY TO_CHAR(FECHA_VENTA, 'YYYY-MM')
ORDER BY Periodo;
```

### Query de Block Headers (SQL Server)
```sql
SELECT
    Periodo,
    COUNT(*) AS TotalRegistros,
    SUM(Monto) AS SumaMonto
FROM Ventas
GROUP BY Periodo
ORDER BY Periodo;
```

### Query de Block Data (Oracle)
```sql
SELECT
    ID, FECHA_VENTA, CLIENTE, PRODUCTO, MONTO, PERIODO
FROM VENTAS
WHERE TO_CHAR(FECHA_VENTA, 'YYYY-MM') = :periodo;
```

### Query de Block Data (SQL Server)
```sql
SELECT
    Id, FechaVenta, Cliente, Producto, Monto, Periodo
FROM Ventas
WHERE Periodo = @periodo;
```

## Índices y Performance

Ambas tablas tienen índices optimizados para:
1. **Periodo**: Usado en queries de sincronización (GROUP BY, WHERE)
2. **FechaVenta**: Usado para filtros por rango de fechas

### Recomendaciones de Performance

**Oracle:**
- Ejecutar `EXEC DBMS_STATS.GATHER_TABLE_STATS` periódicamente
- Considerar particionamiento por PERIODO para tablas grandes (>10M registros)

**SQL Server:**
- Actualizar estadísticas: `UPDATE STATISTICS dbo.Ventas`
- Considerar particionamiento por Periodo para tablas grandes (>10M registros)

## Datos de Prueba

Los scripts incluyen comentarios con INSERTs de ejemplo. Para probar:

1. Descomentar la sección de datos de prueba en cada script
2. Ejecutar los scripts
3. Verificar con: `SELECT COUNT(*) FROM VENTAS/Ventas`

## Troubleshooting

### Oracle
- **Error ORA-00942**: La tabla no existe → Verificar permisos de usuario
- **Error ORA-01031**: Privilegios insuficientes → Necesitas `CREATE TABLE` privilege

### SQL Server
- **Error 229**: Permiso denegado → Necesitas permisos `db_ddladmin` o `db_owner`
- **Error 2714**: Ya existe tabla → Ejecutar `DROP TABLE` primero

## Backup y Restore

### Oracle
```sql
-- Export
expdp username/password@service schemas=your_schema tables=VENTAS directory=DATA_PUMP_DIR dumpfile=ventas.dmp

-- Import
impdp username/password@service schemas=your_schema tables=VENTAS directory=DATA_PUMP_DIR dumpfile=ventas.dmp
```

### SQL Server
```sql
-- Backup
BACKUP DATABASE YourDB TO DISK = 'C:\backup\ventas.bak'

-- Restore
RESTORE DATABASE YourDB FROM DISK = 'C:\backup\ventas.bak'
```
