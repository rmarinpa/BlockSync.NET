# 🔗 MerkleFlow Core

> **Sincronización de Datos Inteligente basada en Integridad Criptográfica.**
> *Arquitectura de alto rendimiento para migración de datos Legacy.*

![Build Status](https://img.shields.io/badge/Build-Passing-success) ![Platform](https://img.shields.io/badge/Platform-.NET%208-purple) ![License](https://img.shields.io/badge/License-MIT-blue)

## 📋 Resumen Ejecutivo

**MerkleFlow** es una prueba de concepto (PoC) y framework arquitectónico diseñado para resolver el problema de los procesos ETL monolíticos ("Full Load").

Inspirado en los principios de **Blockchain** y **Merkle Trees**, este motor no transfiere datos ciegamente. En su lugar, verifica la "huella digital" (Hash) de bloques de datos históricos. Si el hash en el origen (ej. Oracle Legacy) coincide con el destino (ej. SQL Server), el bloque se ignora, eliminando el consumo de red y CPU. Si difieren, el sistema activa una **auto-reparación quirúrgica** solo para ese periodo.

## 🚀 Problema vs. Solución

| Enfoque Tradicional (Legacy) | Enfoque MerkleFlow (Blockchain Architecture) |
| :--- | :--- |
| **Fuerza Bruta:** Descarga toda la historia (GBs/TBs) cada día. | **Quirúrgico:** Solo descarga lo que ha cambiado (Deltas). |
| **Lento:** Complejidad O(N). Crece con el tiempo. | **Rápido:** Complejidad O(1) para datos históricos. |
| **Frágil:** Si falla un registro, se cae todo el proceso. | **Resiliente:** Si falla un bloque, el resto se procesa. |
| **Ciego:** No detecta si cambiaron datos de años anteriores. | **Auditable:** Detecta "corrupción" histórica y se auto-repara. |

## ⚙️ Arquitectura Técnica

El sistema utiliza **Clean Architecture** y sigue el patrón de **Particionamiento por Tiempo**:

1.  **Block Header Exchange:** El orquestador descarga un mapa ligero `[Periodo, Hash]` del origen y del destino.
2.  **Delta Calculation:** Compara los mapas en memoria usando LINQ (operación de microsegundos).
3.  **Sync Execution:**
    * **SKIP:** Integridad validada (99% de los casos).
    * **INSERT:** Bloque nuevo detectado.
    * **REPAIR:** Hash mismatch detectado (Datos modificados retroactivamente).

### Stack Tecnológico
* **.NET 8 Web API**
* **Entity Framework Core** (con optimización `AsNoTracking`)
* **In-Memory Database** (Para simulación de escenarios en la PoC)
* **Algoritmos de Hashing:** MD5 / SHA256 sobre agregaciones (`SUM` + `COUNT`).

## 🛠️ Instalación y Ejecución

1.  **Clonar el repositorio:**
    ```bash
    git clone [https://github.com/tu-usuario/merkleflow.git](https://github.com/tu-usuario/merkleflow.git)
    ```
2.  **Ejecutar la API:**
    Abrir la solución en Visual Studio o VS Code y ejecutar el perfil `https`.
3.  **Probar con Swagger:**
    Navegar a `https://localhost:xxxx/swagger`.

## 🧪 Guía de Pruebas (Demo)

Para demostrar la potencia del motor, utiliza los endpoints de simulación incluidos:

### Escenario 1: Carga Inicial
1.  Ejecuta `GET /api/sync/status` -> Verás que el Destino está vacío.
2.  Ejecuta `POST /api/sync/run` -> El sistema detectará bloques faltantes e insertará todo.
3.  **Resultado:** Sincronización completa.

### Escenario 2: Eficiencia (Idempotencia)
1.  Ejecuta nuevamente `POST /api/sync/run`.
2.  **Resultado:** El sistema reportará `SKIPPED` en todos los meses. Tiempo de proceso cercano a 0.

### Escenario 3: Detección de Fraude / Corrección Histórica (Auto-Healing)
1.  Ejecuta `POST /api/simulation/hack-legacy`.
    * *Esto modifica "por debajo de la mesa" el monto de una venta de hace 2 años en el Origen.*
2.  Ejecuta `POST /api/sync/run`.
3.  **Resultado:** El sistema detectará que el hash de ese mes específico no coincide.
    * Acción: **REPAIR**. Borrará el mes local corrupto y traerá la nueva versión oficial.

## 📊 Benchmarks (Estimados)

| Métrica | ETL Legacy (Actual) | MerkleFlow | Mejora |
| :--- | :--- | :--- | :--- |
| **Tiempo Proc.** | 13 Horas | < 20 Minutos | **~97%** |
| **Uso de Red** | 50 GB / día | 50 MB / día | **Reducción masiva** |
| **RAM Server** | Alta (Picos de OOM) | Baja (Estable) | **Estabilidad** |

## ⚖️ Licencia

Distribuido bajo la licencia MIT. Ver `LICENSE` para más información.

---
*Desarrollado como solución arquitectónica para optimización de Big Data en entornos Enterprise.*
