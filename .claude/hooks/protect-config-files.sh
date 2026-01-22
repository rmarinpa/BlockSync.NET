#!/bin/bash

# Hook PreToolUse: Bloquea acceso a archivos de configuración sensibles
# Este hook impide que Claude lea archivos con connection strings, API keys, etc.

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

    # Combinar todas las posibles rutas en una sola variable
    all_paths="$file_path $path $pattern"

    # Lista de archivos sensibles a proteger
    sensitive_files=(
        "appsettings.Development.json"
        "appsettings.Local.json"
        "appsettings.Production.json"
        "secrets.json"
        "user-secrets"
        "launchSettings.json"
        "connectionstrings.json"
    )

    # Verificar si alguna ruta contiene archivos sensibles
    for sensitive_file in "${sensitive_files[@]}"; do
        if echo "$all_paths" | grep -qiF "$sensitive_file"; then
            # Bloquear la operación con exit code 2
            echo "🔒 ACCESO BLOQUEADO: No se permite leer '$sensitive_file' por seguridad." >&2
            echo "Este archivo puede contener connection strings, API keys u otros secretos sensibles." >&2
            echo "Si necesitas información de configuración, consulta con el usuario directamente." >&2
            exit 2
        fi
    done

    # También bloquear accesos a directorios de secretos de usuario de .NET
    if echo "$all_paths" | grep -qE "(Microsoft/UserSecrets|\.microsoft/usersecrets)"; then
        echo "🔒 ACCESO BLOQUEADO: No se permite acceder a User Secrets de .NET por seguridad." >&2
        echo "Los User Secrets contienen información sensible del entorno de desarrollo." >&2
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
