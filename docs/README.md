# BlockSync.NET - Documentación

Documentación técnica completa del proyecto BlockSync.NET.

## 📁 Estructura de Documentación

```
docs/
├── CLAUDE.md                                          ← Instrucciones para Claude Code (AI)
├── architecture/
│   └── flexible-database-design.md                    ← Arquitectura de BD flexible
├── guides/
│   └── flexible-database-quickstart.md                ← Guía rápida con ejemplos
└── reference/
    └── sqlite-queries-and-verification.md             ← Queries y verificación
```

---

## 🎯 Documentos Principales

### Para Desarrolladores

**[CLAUDE.md](./CLAUDE.md)**
- Instrucciones para Claude Code (AI assistant)
- Información del proyecto y arquitectura
- Comandos de build y ejecución
- Convenciones de código

### Arquitectura

**[Diseño de Base de Datos Flexible](./architecture/flexible-database-design.md)**
- Sistema de mapeo configurable por JSON
- Soporte multi-database (Oracle, SQL Server, PostgreSQL, MySQL, SQLite)
- EntityMapper sin AutoMapper
- DynamicQueryBuilder para cada provider
- GenericSyncRepository configurable

### Guías y Tutoriales

**[Flexible Database - Quick Start](./guides/flexible-database-quickstart.md)**
- Casos de uso comunes con ejemplos
- Oracle Legacy → SQL Server Local
- MySQL → PostgreSQL Cloud
- Configuración paso a paso
- Herramienta CLI para generar mappings

### Referencias

**[SQLite Queries y Verificación](./reference/sqlite-queries-and-verification.md)**
- Queries de verificación completas
- Validación de integridad (Ventas vs SyncLedger)
- Resumen del plan de arquitectura flexible
- Estado del proyecto

---

## 🚀 Quick Links

| Necesito... | Documento |
|-------------|-----------|
| Entender el proyecto | [CLAUDE.md](./CLAUDE.md) |
| Configurar otra BD | [Flexible DB Design](./architecture/flexible-database-design.md) |
| Ejemplos prácticos | [Flexible DB QuickStart](./guides/flexible-database-quickstart.md) |
| Verificar SQLite | [Queries Reference](./reference/sqlite-queries-and-verification.md) |
| Información general | [README principal](../README.md) |

---

## 📚 Recursos Externos

- **[README.md](../README.md)** - Overview del proyecto
- **[INSTRUCTIONS.md](../INSTRUCTIONS.md)** - Instrucciones para el usuario
- **[database/sqlite/schema.sql](../database/sqlite/schema.sql)** - Schema optimizado
- **[scripts/verify-databases.sh](../scripts/verify-databases.sh)** - Script de verificación

---

## 🔧 Convenciones de Documentación

- Todos los archivos en formato Markdown (.md)
- Usar headers jerárquicos (# ## ### ####)
- Ejemplos de código con syntax highlighting
- Emojis para visual scanning rápido
- Referencias cruzadas entre documentos

---

**Última actualización:** 2026-01-23
**Branch:** feature/local-sqlite
**Versión:** 1.0.0
