#!/bin/bash

# Hook PostToolUse: Ejecuta dotnet build después de Edit o Create
# Este hook compila el proyecto después de modificaciones para detectar errores rápidamente

# Leer el input JSON desde stdin
input=$(cat)

# Extraer el nombre de la herramienta
tool_name=$(echo "$input" | jq -r '.tool_name // empty')

# Solo proceder si la herramienta es Edit o Write (Create)
if [[ "$tool_name" != "Edit" && "$tool_name" != "Write" ]]; then
    exit 0
fi

# Extraer los parámetros de la herramienta
tool_input=$(echo "$input" | jq -r '.tool_input // {}')
file_path=$(echo "$tool_input" | jq -r '.file_path // empty')

# Verificar si el archivo modificado es un archivo de código .NET (cs, csproj, sln)
if [[ "$file_path" =~ \.(cs|csproj|sln|fsproj|vbproj)$ ]]; then
    echo "🔨 Ejecutando dotnet build para validar cambios..." >&2

    # Encontrar el directorio raíz del proyecto (donde está el .sln o el primer .csproj)
    project_dir="$(pwd)"

    # Intentar encontrar un archivo .sln primero
    if [ -f "BlockSync.sln" ] || [ -f "*.sln" ]; then
        dotnet build --no-restore --verbosity quiet 2>&1 | grep -E "error|warning" >&2 || echo "✅ Build exitoso" >&2
    else
        # Si no hay .sln, buscar el primer .csproj
        csproj_file=$(find . -maxdepth 3 -name "*.csproj" | head -n 1)
        if [ -n "$csproj_file" ]; then
            dotnet build "$csproj_file" --no-restore --verbosity quiet 2>&1 | grep -E "error|warning" >&2 || echo "✅ Build exitoso" >&2
        fi
    fi
fi

exit 0
