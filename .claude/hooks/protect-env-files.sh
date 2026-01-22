#!/bin/bash

# Hook para proteger archivos .env de ser leídos por Claude Code
# Este hook intercepta todas las operaciones de herramientas y bloquea
# cualquier intento de acceder a archivos .env

# Leer el input JSON desde stdin
input=$(cat)

# Extraer el nombre de la herramienta
tool_name=$(echo "$input" | jq -r '.tool_name // empty')

# Extraer los parámetros de la herramienta
tool_input=$(echo "$input" | jq -r '.tool_input // {}')

# Lista de herramientas que pueden acceder a archivos
file_access_tools=("Read" "Edit" "Write" "Grep" "Glob")

# Verificar si la herramienta actual accede a archivos
if [[ " ${file_access_tools[@]} " =~ " ${tool_name} " ]]; then
    # Extraer todos los valores del tool_input que podrían contener rutas
    file_path=$(echo "$tool_input" | jq -r '.file_path // empty')
    path=$(echo "$tool_input" | jq -r '.path // empty')
    pattern=$(echo "$tool_input" | jq -r '.pattern // empty')
    command=$(echo "$tool_input" | jq -r '.command // empty')

    # Combinar todas las posibles rutas en una sola variable
    all_paths="$file_path $path $pattern $command"

    # Verificar si alguna ruta contiene .env
    if echo "$all_paths" | grep -qiE '\.(env|environment)(\.|$|/)'; then
        # Bloquear la operación con exit code 2
        echo "🚫 OPERACIÓN BLOQUEADA: No se permite acceder a archivos .env o .environment por seguridad." >&2
        echo "Los archivos .env contienen información sensible y están protegidos por el hook de seguridad." >&2
        exit 2
    fi
fi

# Permitir la operación retornando JSON con decisión "allow"
cat <<EOF
{
  "hookSpecificOutput": {
    "hookEventName": "PreToolUse",
    "permissionDecision": "allow"
  }
}
EOF

exit 0
