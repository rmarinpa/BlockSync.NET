# BlockSync.NET Frontend - Guía de Instalación

## Prerequisitos

- Node.js 18+ (https://nodejs.org/)
- npm o yarn
- BlockSync.NET API corriendo en http://localhost:5000

## Instalación Rápida

```bash
# Navegar al directorio frontend
cd /Users/ricardomarin/Documents/GitHub/BlockSync.NET/frontend

# Instalar dependencias
npm install

# Iniciar servidor de desarrollo
npm run dev
```

El frontend estará disponible en: **http://localhost:3000**

## Verificación de la API

Antes de ejecutar el frontend, asegúrate que la API de BlockSync.NET esté funcionando:

```bash
# Terminal 1: Iniciar API (.NET)
cd /Users/ricardomarin/Documents/GitHub/BlockSync.NET/src/BlockSync.API
dotnet run

# Terminal 2: Iniciar Frontend (React)
cd /Users/ricardomarin/Documents/GitHub/BlockSync.NET/frontend
npm run dev
```

Verifica que la API responda en: http://localhost:5000/api/Sync/status

## Scripts Disponibles

```bash
npm run dev      # Servidor de desarrollo (port 3000)
npm run build    # Compilar para producción
npm run preview  # Preview de build de producción
```

## Troubleshooting

### Error: "Cannot connect to API"
- Verifica que la API .NET esté corriendo en http://localhost:5000
- Revisa la consola del navegador para errores CORS
- Asegúrate que la API tenga CORS habilitado para http://localhost:3000

### Error: "Module not found"
```bash
rm -rf node_modules package-lock.json
npm install
```

### Puerto 3000 en uso
Edita `vite.config.ts` para cambiar el puerto:
```typescript
server: {
  port: 4000,  // Cambia a cualquier puerto disponible
}
```

## Estructura del Proyecto

```
frontend/
├── src/
│   ├── components/     # Componentes reutilizables
│   │   └── Layout.tsx  # Layout principal con navegación
│   ├── pages/          # Páginas de la aplicación
│   │   ├── Dashboard.tsx
│   │   ├── Ledger.tsx
│   │   ├── Hashes.tsx
│   │   ├── Actions.tsx
│   │   └── Diagnostics.tsx
│   ├── lib/
│   │   └── api.ts      # Cliente API centralizado
│   ├── types/
│   │   └── api.ts      # Tipos TypeScript del OpenAPI
│   ├── App.tsx         # Router principal
│   ├── main.tsx        # Entry point
│   └── index.css       # Estilos globales + TailwindCSS
├── public/             # Assets estáticos
├── package.json
├── vite.config.ts
├── tailwind.config.js
└── tsconfig.json
```

## Funcionalidades

### Auto-Refresh
- Dashboard: Actualiza cada 5 segundos
- Ledger: Actualiza cada 10 segundos
- Hashes: Actualiza cada 10 segundos

### Temas Visuales
- Dark mode por defecto (terminal aesthetic)
- Efectos CRT con scanlines
- Tipografía monospace (JetBrains Mono)
- Animaciones y transiciones suaves

### Interactividad
- Filtros en tiempo real en Ledger
- Expand/collapse de filas para detalles
- Botones de acción con feedback visual
- Estados de loading y error

## Configuración CORS en la API

Si experimentas errores CORS, agrega esto en `Program.cs` de la API:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy
            .WithOrigins("http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader());
});

// ...

app.UseCors("AllowFrontend");
```

## Producción

Para compilar para producción:

```bash
npm run build
```

Los archivos optimizados estarán en `dist/`. Puedes servirlos con cualquier servidor estático:

```bash
# Opción 1: Preview con Vite
npm run preview

# Opción 2: Servidor HTTP simple
npx serve dist

# Opción 3: Nginx, Apache, IIS, etc.
```

---

**¡Listo para sincronizar con estilo terminal! 🟢**
