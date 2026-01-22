# BlockSync.NET - Instrucciones de Uso

## 📋 Descripción General

**BlockSync.NET** es una Prueba de Concepto (PoC) de un motor de sincronización de datos basado en integridad de bloques, inspirado en Blockchain y Merkle Trees. Reemplaza ETLs lentos con sincronización inteligente mediante comparación de hashes.

### Características Principales

- ✅ **Clean Architecture**: Separación clara de capas (Domain, Application, Infrastructure, API)
- ✅ **Sincronización Inteligente**: Solo sincroniza bloques con cambios detectados
- ✅ **Detección de Corrupción**: Identifica automáticamente datos corruptos mediante hashes
- ✅ **Generación de Datos**: Usa Bogus con seed fija para datos reproducibles
- ✅ **Minimal API**: Endpoints REST modernos con .NET 8
- ✅ **Swagger**: Documentación interactiva de API

---

## 🏗️ Arquitectura del Proyecto

```
BlockSync.NET/
│   ├── BlockSync.Domain/           # Capa de Dominio (sin dependencias externas)
│   │   ├── Entities/
│   │   │   └── Venta.cs           # Entidad principal del dominio
│   │   ├── ValueObjects/
│   │   │   └── BlockHeader.cs     # Header de bloque con hash
│   │   └── Interfaces/
│   │       ├── ISyncSource.cs     # Contrato para sistema origen
│   │       └── ISyncDestination.cs # Contrato para sistema destino
│   │
│   ├── BlockSync.Application/      # Capa de Aplicación (lógica de negocio)
│   │   ├── Services/
│   │   │   └── SyncEngine.cs      # Motor principal de sincronización
│   │   └── DTOs/
│   │       ├── SyncActionType.cs  # Enum: SKIP, INSERT, REPAIR
│   │       ├── BlockSyncResult.cs # Resultado por bloque
│   │       └── SyncReport.cs      # Reporte completo
│   │
│   ├── BlockSync.Infrastructure/   # Capa de Infraestructura (implementaciones)
│   │   ├── Repositories/
│   │   │   ├── LegacyRepository.cs # Sistema legacy (origen)
│   │   │   └── LocalRepository.cs  # Sistema local (destino)
│   │   └── Services/
│   │       └── HashCalculator.cs   # Calculador de hashes MD5/SHA256
│   │
│   └── BlockSync.API/              # Capa de API (Minimal API)
│       ├── Program.cs              # Configuración y endpoints
│       ├── BlockSync.API.csproj    # Archivo de proyecto
│       ├── appsettings.json        # Configuración
│       └── appsettings.Development.json
│
└── INSTRUCTIONS.md                 # Este archivo
```

---

## 🚀 Cómo Ejecutar el Proyecto

### Requisitos Previos

- **.NET 8 SDK** instalado ([Descargar aquí](https://dotnet.microsoft.com/download/dotnet/8.0))
- Terminal o línea de comandos

### Paso 1: Navegar al Proyecto

```bash
cd /Users/ricardomarin/Documents/GitHub/BlockSync.NET/src/BlockSync.API
```

### Paso 2: Restaurar Dependencias

```bash
dotnet restore
```

### Paso 3: Ejecutar la Aplicación

```bash
dotnet run
```

La API estará disponible en: **http://localhost:5000**

---

## 📡 Endpoints de la API

### 1. **GET /status** - Estado del Sistema

Muestra el estado actual de origen y destino.

```bash
curl -X GET http://localhost:5000/status
```

**Respuesta:**
```json
{
  "timestamp": "2024-01-22T10:00:00Z",
  "sistema": "BlockSync.NET",
  "version": "1.0.0",
  "origen": {
    "registros": 5000,
    "bloques": 25,
    "periodos": ["2022-01", "2022-02", "..."]
  },
  "destino": {
    "registros": 0,
    "bloques": 0,
    "periodos": []
  },
  "estadoSincronizacion": "DESINCRONIZADO"
}
```

---

### 2. **POST /sync** - Ejecutar Sincronización

Ejecuta el proceso completo de sincronización.

```bash
curl -X POST http://localhost:5000/sync
```

**Respuesta:**
```json
{
  "exitoso": true,
  "resumen": {
    "duracionMs": 1234,
    "totalBloques": 25,
    "bloquesOmitidos": 0,
    "bloquesInsertados": 25,
    "bloquesReparados": 0,
    "totalRegistrosProcesados": 5000,
    "totalRegistrosInsertados": 5000
  },
  "reporte": { /* Detalles completos */ },
  "resumenTexto": "/* ASCII report */"
}
```

**Tipos de Acciones:**
- **SKIP**: Hashes coinciden, sin cambios
- **INSERT**: Bloque no existe en destino, se descarga completo
- **REPAIR**: Hashes difieren, se elimina y re-descarga

---

### 3. **POST /hack/{anio}/{mes}** - Simular Corrupción

Corrompe intencionalmente datos de un periodo para probar la detección.

```bash
# Corromper datos de Marzo 2023
curl -X POST http://localhost:5000/hack/2023/3
```

**Respuesta:**
```json
{
  "exitoso": true,
  "mensaje": "Datos del periodo 2023-03 han sido corrompidos intencionalmente",
  "periodoAfectado": "2023-03",
  "instrucciones": "Ejecuta POST /sync para detectar y reparar la corrupción"
}
```

---

### 4. **POST /reset** - Reiniciar Sistema

Limpia el destino y restaura el origen al estado inicial.

```bash
curl -X POST http://localhost:5000/reset
```

**Respuesta:**
```json
{
  "exitoso": true,
  "mensaje": "Sistema reiniciado completamente",
  "accionesRealizadas": [
    "✓ Almacenamiento local limpiado",
    "✓ Datos de origen restaurados al estado original",
    "✓ Todas las corrupciones eliminadas"
  ],
  "siguientePaso": "Ejecuta POST /sync para sincronizar desde cero"
}
```

---

### 5. **GET /hashes** - Ver Mapa de Hashes

Muestra la comparación detallada de hashes entre origen y destino.

```bash
curl -X GET http://localhost:5000/hashes
```

**Respuesta:**
```json
{
  "timestamp": "2024-01-22T10:00:00Z",
  "totalPeriodos": 25,
  "sincronizados": 20,
  "faltanEnDestino": 3,
  "corruptos": 2,
  "bloques": [
    {
      "periodo": "2022-01",
      "estado": "SINCRONIZADO",
      "origen": {
        "hash": "a1b2c3d4...",
        "registros": 200,
        "monto": 45000.50
      },
      "destino": {
        "hash": "a1b2c3d4...",
        "registros": 200,
        "monto": 45000.50
      }
    }
  ]
}
```

---

## 🧪 Casos de Prueba Recomendados

### Prueba 1: Sincronización Inicial

```bash
# 1. Verificar estado inicial (destino vacío)
curl -X GET http://localhost:5000/status

# 2. Ejecutar primera sincronización (INSERT de todos los bloques)
curl -X POST http://localhost:5000/sync

# 3. Verificar que origen y destino están sincronizados
curl -X GET http://localhost:5000/status
```

**Resultado Esperado:** Todos los bloques se insertan (INSERT), origen y destino quedan sincronizados.

---

### Prueba 2: Sincronización Sin Cambios

```bash
# 1. Ejecutar sincronización nuevamente sin cambios
curl -X POST http://localhost:5000/sync
```

**Resultado Esperado:** Todos los bloques se omiten (SKIP), sincronización instantánea.

---

### Prueba 3: Detección y Reparación de Corrupción

```bash
# 1. Corromper un periodo específico
curl -X POST http://localhost:5000/hack/2023/3

# 2. Ver que el hash cambió
curl -X GET http://localhost:5000/hashes

# 3. Ejecutar sincronización (detectará y reparará)
curl -X POST http://localhost:5000/sync

# 4. Verificar que se reparó
curl -X GET http://localhost:5000/hashes
```

**Resultado Esperado:** El bloque corrupto se detecta (REPAIR), se elimina y re-descarga.

---

### Prueba 4: Reset Completo

```bash
# 1. Limpiar y restaurar todo
curl -X POST http://localhost:5000/reset

# 2. Verificar que destino está vacío
curl -X GET http://localhost:5000/status

# 3. Sincronizar desde cero
curl -X POST http://localhost:5000/sync
```

**Resultado Esperado:** Sistema vuelve al estado inicial, listo para re-sincronizar.

---

## 🎯 Algoritmo de Sincronización (Pseudocódigo)

```
PARA cada periodo en ORIGEN:

    SI periodo NO existe en DESTINO:
        → INSERT: Descargar bloque completo e insertar

    SI Hash(ORIGEN) == Hash(DESTINO):
        → SKIP: Sin cambios, omitir

    SI Hash(ORIGEN) != Hash(DESTINO):
        → REPAIR:
            1. Eliminar bloque corrupto del destino
            2. Descargar bloque correcto del origen
            3. Insertar bloque reparado
```

---

## 🔍 Detalles de Implementación

### Generación de Datos (Bogus)

- **Seed Fija:** 8675309 (para reproducibilidad)
- **Total Ventas:** 5,000 registros
- **Rango de Fechas:** 2022-01-01 hasta la fecha actual
- **Distribución:** Aleatoria pero consistente entre ejecuciones

### Cálculo de Hashes

```csharp
// Fórmula: MD5(SUM(Monto) + "|" + COUNT(*))
var blockData = $"{totalMonto}|{totalRegistros}";
var hash = CalcularMD5(blockData);
```

**Ventajas:**
- Detecta cambios en montos
- Detecta cambios en cantidad de registros
- Rápido de calcular
- No requiere descargar datos completos

---

## 📊 Swagger / OpenAPI

La documentación interactiva está disponible en:

**http://localhost:5000**

Desde allí puedes:
- Ver todos los endpoints
- Probar las APIs directamente
- Ver los schemas de request/response

---

## 🛠️ Tecnologías Utilizadas

| Tecnología | Propósito |
|------------|-----------|
| **.NET 8** | Framework principal |
| **C# 12** | Lenguaje de programación |
| **Minimal API** | Endpoints REST modernos |
| **Bogus** | Generación de datos falsos |
| **System.Security.Cryptography** | Cálculo de hashes MD5 |
| **Swagger/OpenAPI** | Documentación de API |

---

## 🎓 Conceptos Educativos

### Clean Architecture

Este proyecto demuestra:

1. **Separación de Capas**: Domain → Application → Infrastructure → API
2. **Inversión de Dependencias**: Las capas externas dependen de las internas
3. **Interfaces como Contratos**: ISyncSource, ISyncDestination
4. **Domain-Driven Design**: Entidades ricas, Value Objects

### Blockchain-Inspired Sync

Similar a cómo blockchain valida bloques:

1. **Block Header**: Contiene hash e información del periodo
2. **Merkle-like Comparison**: Comparación rápida sin descargar datos completos
3. **Integrity Verification**: Detección automática de corrupción
4. **Repair Mechanism**: Auto-reparación descargando desde fuente de verdad

---

## 🚀 Extensiones Futuras

Para convertir esta PoC en producción, considera:

1. **Base de Datos Real**: SQL Server, PostgreSQL en lugar de List<T>
2. **Paginación**: Para manejar bloques grandes
3. **Concurrencia**: Locks distribuidos para evitar sincronizaciones simultáneas
4. **Logging Estructurado**: Serilog, NLog
5. **Monitoreo**: Health checks, métricas
6. **Seguridad**: Autenticación, autorización
7. **Hashes Más Seguros**: SHA256 en lugar de MD5
8. **Async Batch Processing**: Procesar múltiples bloques en paralelo
9. **Delta Sync**: Sincronizar solo registros individuales en lugar de bloques completos
10. **Event Sourcing**: Registrar todos los cambios como eventos

---

## 📞 Soporte

Para preguntas sobre este PoC:

1. Revisa los comentarios en el código (están en español y son muy detallados)
2. Usa Swagger para explorar la API interactivamente
3. Ejecuta los casos de prueba recomendados
4. Inspecciona los logs de consola durante la sincronización

---

## ✅ Checklist de Verificación

- [ ] .NET 8 SDK instalado
- [ ] Proyecto restaurado (`dotnet restore`)
- [ ] Aplicación ejecutándose en puerto 5000
- [ ] Swagger accesible en navegador
- [ ] Endpoint `/status` respondiendo
- [ ] Primera sincronización completada (INSERT de todos los bloques)
- [ ] Segunda sincronización sin cambios (SKIP de todos los bloques)
- [ ] Corrupción simulada y reparada correctamente
- [ ] Reset completado y re-sincronización exitosa

---

## 🎉 ¡Listo!

Tu sistema BlockSync.NET está funcionando. Explora los endpoints, experimenta con corrupciones, y observa cómo el sistema detecta y repara automáticamente las inconsistencias.

**¡Disfruta sincronizando datos con integridad blockchain-style!** 🔗🚀
