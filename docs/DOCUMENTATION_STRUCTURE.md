# 📁 Estructura de Documentación - BlockSync.NET

Este documento muestra la organización completa de toda la documentación del proyecto.

---

## 🌳 Árbol de Archivos

```
BlockSync.NET/
│
├── 📄 README.md                          ← Overview principal del proyecto
├── 📄 INSTRUCTIONS.md                    ← Instrucciones para el usuario final
├── 📄 LICENSE                            ← MIT License
├── 📄 BlockSync.sln                      ← Solución .NET
│
├── 📂 docs/                              ← TODA LA DOCUMENTACIÓN TÉCNICA
│   │
│   ├── 📄 README.md                      ← Índice de documentación
│   ├── 📄 CLAUDE.md                      ← ⭐ Guía principal del proyecto
│   │                                        (Build, arquitectura, convenciones)
│   │
│   ├── 📂 architecture/                  ← Diseños arquitectónicos
│   │   └── 📄 flexible-database-design.md
│   │                                        Sistema de BD configurable
│   │                                        (EntityMapper, QueryBuilder, GenericRepo)
│   │
│   ├── 📂 guides/                        ← Guías paso a paso
│   │   └── 📄 flexible-database-quickstart.md
│   │                                        Ejemplos prácticos
│   │                                        (Oracle→SQL Server, MySQL→PostgreSQL)
│   │
│   └── 📂 reference/                     ← Referencias técnicas
│       └── 📄 sqlite-queries-and-verification.md
│                                            Queries de verificación
│                                            (SELECT, stats, integridad)
│
├── 📂 database/                          ← Scripts de base de datos
│   └── 📂 sqlite/
│       └── 📄 schema.sql                 ← Schema optimizado SQLite
│
├── 📂 scripts/                           ← Scripts de utilidad
│   └── 📄 verify-databases.sh            ← Script de verificación completa
│
└── 📂 src/                               ← Código fuente .NET
    ├── BlockSync.API/
    ├── BlockSync.Application/
    ├── BlockSync.Domain/
    └── BlockSync.Infrastructure/
```

---

## 📚 Guía de Navegación por Tipo de Usuario

### 👨‍💻 Desarrollador que se une al proyecto

**Empieza aquí:**
1. [README.md](../README.md) - Overview del proyecto
2. [docs/CLAUDE.md](./CLAUDE.md) - Arquitectura y comandos de build
3. [docs/reference/sqlite-queries-and-verification.md](./reference/sqlite-queries-and-verification.md) - Verificar que todo funciona

### 🏗️ Arquitecto que quiere entender el diseño

**Empieza aquí:**
1. [docs/CLAUDE.md](./CLAUDE.md) - Clean Architecture y componentes
2. [docs/architecture/flexible-database-design.md](./architecture/flexible-database-design.md) - Sistema de mapeo configurable

### 🎯 Usuario que necesita configurar otra BD

**Empieza aquí:**
1. [docs/guides/flexible-database-quickstart.md](./guides/flexible-database-quickstart.md) - Ejemplos completos
2. [docs/architecture/flexible-database-design.md](./architecture/flexible-database-design.md) - Referencia técnica

### 🔍 DevOps que necesita verificar el sistema

**Empieza aquí:**
1. [docs/reference/sqlite-queries-and-verification.md](./reference/sqlite-queries-and-verification.md) - Queries de diagnóstico
2. [scripts/verify-databases.sh](../scripts/verify-databases.sh) - Script automatizado

---

## 📖 Descripción de Cada Documento

### Root Level (Nivel Superior)

| Archivo | Propósito | Audiencia |
|---------|-----------|-----------|
| **README.md** | Overview del proyecto, problema/solución, instalación | Todos |
| **INSTRUCTIONS.md** | Instrucciones para usuario final | Usuario |
| **LICENSE** | MIT License | Legal |

### docs/ (Documentación Técnica)

| Archivo | Descripción | Tamaño |
|---------|-------------|--------|
| **README.md** | Índice de toda la documentación | 3 KB |
| **CLAUDE.md** | Guía completa del proyecto para Claude Code<br>• Build commands<br>• Clean Architecture<br>• Sync algorithm<br>• API endpoints<br>• Blockchain Ledger | 16 KB |

### docs/architecture/ (Arquitectura)

| Archivo | Descripción | Tamaño |
|---------|-------------|--------|
| **flexible-database-design.md** | Diseño completo del sistema de BD flexible<br>• DatabaseMappingConfiguration<br>• EntityMapper<TEntity><br>• DynamicQueryBuilder<br>• GenericSyncRepository | 28 KB |

### docs/guides/ (Guías)

| Archivo | Descripción | Tamaño |
|---------|-------------|--------|
| **flexible-database-quickstart.md** | Guía práctica con ejemplos<br>• Oracle → SQL Server<br>• MySQL → PostgreSQL<br>• SQL Server → SQL Server<br>• Herramienta CLI generate-mapping | 16 KB |

### docs/reference/ (Referencias)

| Archivo | Descripción | Tamaño |
|---------|-------------|--------|
| **sqlite-queries-and-verification.md** | Queries completas para SQLite<br>• Verificación de tablas<br>• Stats del ledger<br>• Validación de integridad<br>• Resumen del plan flexible DB | 10 KB |

---

## 🎯 Quick Links por Tarea

| Necesito... | Ir a... |
|-------------|---------|
| **Compilar el proyecto** | [docs/CLAUDE.md](./CLAUDE.md#build--run-commands) |
| **Entender la arquitectura** | [docs/CLAUDE.md](./CLAUDE.md#clean-architecture-structure) |
| **Ver endpoints de la API** | [docs/CLAUDE.md](./CLAUDE.md#api-endpoints) |
| **Configurar Oracle o SQL Server** | [docs/guides/flexible-database-quickstart.md](./guides/flexible-database-quickstart.md) |
| **Entender el sistema de mapeo** | [docs/architecture/flexible-database-design.md](./architecture/flexible-database-design.md) |
| **Verificar base de datos SQLite** | [docs/reference/sqlite-queries-and-verification.md](./reference/sqlite-queries-and-verification.md) |
| **Ejecutar queries de diagnóstico** | [docs/reference/sqlite-queries-and-verification.md](./reference/sqlite-queries-and-verification.md#queries-de-verificación-sqlite) |
| **Ver el ledger blockchain** | [docs/CLAUDE.md](./CLAUDE.md#blockchain-ledger-syncledger) |

---

## 📝 Convenciones de Documentación

### Nombres de Archivos
- **kebab-case** para nombres: `flexible-database-design.md`
- **Descriptivos y específicos**
- **Sin abreviaturas** cuando sea posible

### Estructura de Documentos
- Headers jerárquicos (# ## ### ####)
- Emojis para visual scanning rápido
- Ejemplos de código con syntax highlighting
- Referencias cruzadas entre documentos
- Tabla de contenidos cuando sea largo (>5KB)

### Categorías
- **architecture/** - Diseños técnicos detallados
- **guides/** - Tutoriales paso a paso
- **reference/** - Referencias técnicas y queries

---

## 🔄 Historial de Reorganización

### Commit: `9f26884` - 2026-01-23
**refactor: Reorganizar documentación en estructura jerárquica**

**Antes (desordenado en root):**
```
BlockSync.NET/
├── CLAUDE.md
├── ARCHITECTURE_FLEXIBLE_DB.md
├── FLEXIBLE_DB_QUICKSTART.md
├── RESUMEN_QUERIES_Y_PLAN.md
└── README.md
```

**Después (organizado en docs/):**
```
BlockSync.NET/
├── README.md
└── docs/
    ├── CLAUDE.md
    ├── architecture/flexible-database-design.md
    ├── guides/flexible-database-quickstart.md
    └── reference/sqlite-queries-and-verification.md
```

**Beneficios:**
- ✅ Estructura escalable
- ✅ Documentos categorizados
- ✅ Fácil navegación
- ✅ Separación clara entre docs de usuario y técnicas

---

## 📊 Estadísticas de Documentación

| Categoría | Archivos | Tamaño Total | Líneas |
|-----------|----------|--------------|--------|
| Root (usuario) | 2 | ~17 KB | ~200 |
| docs/ | 6 | ~73 KB | ~2,800 |
| **TOTAL** | **8** | **~90 KB** | **~3,000** |

---

**Última actualización:** 2026-01-23
**Branch:** feature/local-sqlite
**Versión:** 1.0.0

