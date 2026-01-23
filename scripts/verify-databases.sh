#!/bin/bash

# Script de verificación completa para BlockSync SQLite
# Ejecutar desde: cd src/BlockSync.API && ../../scripts/verify-databases.sh

set -e

# Colores para output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

SOURCE_DB="./data/source.db"
DEST_DB="./data/destination.db"

echo -e "${BLUE}╔════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║     VERIFICACIÓN BLOCKSYNC SQLITE - DATABASES         ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════╝${NC}"
echo ""

# Verificar que las bases de datos existen
if [ ! -f "$SOURCE_DB" ]; then
    echo -e "${RED}❌ ERROR: No existe $SOURCE_DB${NC}"
    exit 1
fi

if [ ! -f "$DEST_DB" ]; then
    echo -e "${RED}❌ ERROR: No existe $DEST_DB${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Bases de datos encontradas${NC}"
echo ""

# ========== SOURCE DATABASE ==========
echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${YELLOW}SOURCE DATABASE (source.db)${NC}"
echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"

echo -e "${BLUE}📊 Total de registros:${NC}"
TOTAL_SOURCE=$(sqlite3 "$SOURCE_DB" "SELECT COUNT(*) FROM Ventas;")
echo -e "   ${GREEN}${TOTAL_SOURCE}${NC} registros"
echo ""

echo -e "${BLUE}📅 Distribución por periodo (primeros 5):${NC}"
sqlite3 -header -column "$SOURCE_DB" "
SELECT
    Periodo,
    COUNT(*) as Registros,
    printf('%.2f', SUM(MontoCentavos) / 100.0) as SumaMonto
FROM Ventas
GROUP BY Periodo
ORDER BY Periodo
LIMIT 5;"
echo ""

echo -e "${BLUE}🔍 Muestra de registros (primeros 3):${NC}"
sqlite3 -header -column "$SOURCE_DB" "
SELECT
    hex(substr(Id, 1, 4)) || '...' as IdHex,
    FechaVenta,
    substr(Cliente, 1, 30) as Cliente,
    substr(Producto, 1, 25) as Producto,
    printf('%.2f', MontoCentavos / 100.0) as Monto,
    Periodo
FROM Ventas
LIMIT 3;"
echo ""

# ========== DESTINATION DATABASE ==========
echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${YELLOW}DESTINATION DATABASE (destination.db)${NC}"
echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"

echo -e "${BLUE}📊 Total de registros:${NC}"
TOTAL_DEST=$(sqlite3 "$DEST_DB" "SELECT COUNT(*) FROM Ventas;")
echo -e "   ${GREEN}${TOTAL_DEST}${NC} registros"
echo ""

if [ "$TOTAL_SOURCE" -eq "$TOTAL_DEST" ]; then
    echo -e "${GREEN}✅ Source y Destination tienen la misma cantidad de registros${NC}"
else
    echo -e "${RED}⚠️  ADVERTENCIA: Source ($TOTAL_SOURCE) ≠ Destination ($TOTAL_DEST)${NC}"
fi
echo ""

# ========== SYNC LEDGER ==========
echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${YELLOW}BLOCKCHAIN LEDGER (SyncLedger)${NC}"
echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"

echo -e "${BLUE}📒 Estadísticas del Ledger:${NC}"
sqlite3 -header -column "$DEST_DB" "
SELECT
    COUNT(*) as TotalBloques,
    SUM(CASE WHEN Estado = 'SINCRONIZADO' THEN 1 ELSE 0 END) as Sincronizados,
    SUM(CASE WHEN Estado = 'CORRUPTO' THEN 1 ELSE 0 END) as Corruptos,
    SUM(CASE WHEN Estado = 'PENDIENTE' THEN 1 ELSE 0 END) as Pendientes,
    SUM(CASE WHEN Estado = 'ERROR' THEN 1 ELSE 0 END) as Errores
FROM SyncLedger;"
echo ""

echo -e "${BLUE}📋 Historial de acciones:${NC}"
sqlite3 -header -column "$DEST_DB" "
SELECT
    UltimaAccion as Accion,
    COUNT(*) as Cantidad
FROM SyncLedger
GROUP BY UltimaAccion
ORDER BY Cantidad DESC;"
echo ""

echo -e "${BLUE}🔧 Bloques reparados (si existen):${NC}"
REPAIRS=$(sqlite3 "$DEST_DB" "SELECT COUNT(*) FROM SyncLedger WHERE UltimaAccion = 'REPAIR';")
if [ "$REPAIRS" -gt 0 ]; then
    sqlite3 -header -column "$DEST_DB" "
    SELECT
        PeriodoId,
        substr(Hash, 1, 12) || '...' as Hash,
        TotalRegistros,
        UltimaSync,
        UltimaAccion
    FROM SyncLedger
    WHERE UltimaAccion = 'REPAIR'
    ORDER BY UpdatedAt DESC
    LIMIT 5;"
else
    echo -e "   ${GREEN}No hay bloques reparados${NC}"
fi
echo ""

echo -e "${BLUE}📊 Últimos 5 bloques sincronizados:${NC}"
sqlite3 -header -column "$DEST_DB" "
SELECT
    PeriodoId,
    Estado,
    UltimaAccion as Accion,
    TotalRegistros as Regs,
    printf('%.2f', SumaMontoCentavos / 100.0) as SumaMonto,
    substr(UltimaSync, 1, 19) as UltimaSync
FROM SyncLedger
ORDER BY PeriodoId DESC
LIMIT 5;"
echo ""

# ========== VERIFICACIÓN DE INTEGRIDAD ==========
echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${YELLOW}VERIFICACIÓN DE INTEGRIDAD${NC}"
echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"

echo -e "${BLUE}🔍 Comparando Ventas vs Ledger...${NC}"

DIFF_COUNT=$(sqlite3 "$DEST_DB" "
SELECT COUNT(*) FROM (
    SELECT
        Periodo as PeriodoId,
        COUNT(*) as TotalRegistros,
        SUM(MontoCentavos) as SumaMontoCentavos
    FROM Ventas
    GROUP BY Periodo

    EXCEPT

    SELECT
        PeriodoId,
        TotalRegistros,
        SumaMontoCentavos
    FROM SyncLedger
);")

if [ "$DIFF_COUNT" -eq 0 ]; then
    echo -e "${GREEN}✅ Integridad perfecta: Ventas y Ledger coinciden 100%${NC}"
else
    echo -e "${RED}⚠️  ADVERTENCIA: Hay $DIFF_COUNT bloques con diferencias${NC}"
    echo ""
    echo -e "${BLUE}Diferencias detectadas:${NC}"
    sqlite3 -header -column "$DEST_DB" "
    SELECT
        'Ventas' as Origen,
        Periodo as PeriodoId,
        COUNT(*) as TotalRegistros,
        SUM(MontoCentavos) as SumaMontoCentavos
    FROM Ventas
    GROUP BY Periodo

    EXCEPT

    SELECT
        'Ledger' as Origen,
        PeriodoId,
        TotalRegistros,
        SumaMontoCentavos
    FROM SyncLedger
    ORDER BY PeriodoId
    LIMIT 10;"
fi
echo ""

# ========== INFORMACIÓN DEL SISTEMA ==========
echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${YELLOW}INFORMACIÓN DEL SISTEMA${NC}"
echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"

echo -e "${BLUE}💾 Tamaño de bases de datos:${NC}"
ls -lh "$SOURCE_DB" "$DEST_DB" | awk '{print "   " $9 ": " $5}'
echo ""

echo -e "${BLUE}🔧 Journal Mode:${NC}"
echo -e "   Source: $(sqlite3 "$SOURCE_DB" 'PRAGMA journal_mode;')"
echo -e "   Destination: $(sqlite3 "$DEST_DB" 'PRAGMA journal_mode;')"
echo ""

echo -e "${BLUE}📇 Índices en Ventas (destination):${NC}"
sqlite3 "$DEST_DB" ".indexes Ventas" | sed 's/^/   /'
echo ""

echo -e "${BLUE}📇 Índices en SyncLedger:${NC}"
sqlite3 "$DEST_DB" ".indexes SyncLedger" | sed 's/^/   /'
echo ""

# ========== RESUMEN FINAL ==========
echo -e "${BLUE}╔════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║                    RESUMEN FINAL                       ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════╝${NC}"
echo ""

PERCENT_SYNC=$(sqlite3 "$DEST_DB" "
SELECT
    CAST((SUM(CASE WHEN Estado = 'SINCRONIZADO' THEN 1 ELSE 0 END) * 100.0 / COUNT(*)) AS INTEGER)
FROM SyncLedger;")

echo -e "${GREEN}✅ Source: ${TOTAL_SOURCE} registros${NC}"
echo -e "${GREEN}✅ Destination: ${TOTAL_DEST} registros${NC}"
echo -e "${GREEN}✅ Ledger: $(sqlite3 "$DEST_DB" 'SELECT COUNT(*) FROM SyncLedger;') bloques registrados${NC}"
echo -e "${GREEN}✅ Sincronización: ${PERCENT_SYNC}%${NC}"
echo ""

if [ "$TOTAL_SOURCE" -eq "$TOTAL_DEST" ] && [ "$DIFF_COUNT" -eq 0 ]; then
    echo -e "${GREEN}🎉 Sistema en estado óptimo - Todo sincronizado correctamente${NC}"
else
    echo -e "${YELLOW}⚠️  Sistema requiere atención - Ejecutar sincronización${NC}"
fi

echo ""
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
