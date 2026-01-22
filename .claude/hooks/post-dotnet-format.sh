#!/bin/bash

# Hook PostToolUse: Ejecuta dotnet format después de Edit o Create en archivos .cs
# Este hook formatea automáticamente el código C# según las reglas del proyecto

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

# Verificar si el archivo modificado es un archivo .cs
if [[ "$file_path" =~ \.cs$ ]]; then
    echo "🎨 Formateando código C# con dotnet format..." >&2

    # Obtener el path absoluto del archivo
    abs_file_path="$file_path"
    if [[ ! "$file_path" = /* ]]; then
        abs_file_path="$(pwd)/$file_path"
    fi

    # Ejecutar dotnet format solo en el archivo modificado
    # Buscar el .csproj más cercano o el .sln en el directorio raíz
    if [ -f "BlockSync.sln" ]; then
        dotnet format "BlockSync.sln" --include "$abs_file_path" --verbosity quiet 2>&1 | grep -v "^$" >&2 || echo "✅ Formato aplicado" >&2
    else
        # Buscar el primer .sln o .csproj disponible
        solution_file=$(find . -maxdepth 2 -name "*.sln" | head -n 1)
        if [ -n "$solution_file" ]; then
            dotnet format "$solution_file" --include "$abs_file_path" --verbosity quiet 2>&1 | grep -v "^$" >&2 || echo "✅ Formato aplicado" >&2
        else
            # Si no hay solución, intentar con el primer .csproj
            csproj_file=$(find . -maxdepth 3 -name "*.csproj" | head -n 1)
            if [ -n "$csproj_file" ]; then
                dotnet format "$csproj_file" --include "$abs_file_path" --verbosity quiet 2>&1 | grep -v "^$" >&2 || echo "✅ Formato aplicado" >&2
            fi
        fi
    fi
fi

exit 0
