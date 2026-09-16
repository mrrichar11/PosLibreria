<#
.SYNOPSIS
    Empaqueta y genera el archivo ZIP de una nueva versión de MR SYS Librería para subir a GitHub Releases.

.DESCRIPTION
    1. Lee o solicita la versión semántica (ej. 1.0.1).
    2. Actualiza los metadatos en PuntoDeVentaLibreria.UI.csproj.
    3. Compila y publica la aplicación WPF en modo Release (win-x64).
    4. Excluye estrictamente archivos de datos locales (.db, backups, logs, etc.).
    5. Comprime los archivos en dist/Release_vX.Y.Z/MR_SYS_Libreria_vX.Y.Z.zip.
    6. Muestra las instrucciones exactas paso a paso para publicarlo en GitHub Releases.

.PARAMETER Version
    Número de versión semántica (ej. 1.0.1).
#>

[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$Version = "",

    [Parameter(Position = 1)]
    [string]$Notas = "",

    [switch]$SelfContained = $false
)

$ErrorActionPreference = "Stop"

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "  EMPAQUETADOR DE RELEASES - MR SYS LIBRERIA & REGALERIA" -ForegroundColor Yellow
Write-Host "========================================================" -ForegroundColor Cyan

$rootDir = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $rootDir "src\PuntoDeVentaLibreria.UI\PuntoDeVentaLibreria.UI.csproj"

if (-not (Test-Path $projectPath)) {
    Write-Error "No se encontró el proyecto UI en $projectPath"
    exit 1
}

# 1. Determinar versión si no se especificó
[xml]$projXml = Get-Content $projectPath
$currentVerNode = $projXml.SelectSingleNode("//Version")
$currentVersion = if ($currentVerNode) { $currentVerNode.InnerText } else { "1.0.0" }

if ([string]::IsNullOrWhiteSpace($Version)) {
    Write-Host "Versión actual en csproj: $currentVersion" -ForegroundColor Gray
    # Calcular sugerencia (+1 en patch)
    $parts = $currentVersion.Split('.')
    if ($parts.Length -ge 3) {
        $nextPatch = [int]$parts[2] + 1
        $suggestedVersion = "$($parts[0]).$($parts[1]).$nextPatch"
    } else {
        $suggestedVersion = "$currentVersion.1"
    }

    $inputVersion = Read-Host "Ingrese la versión a compilar [$suggestedVersion]"
    if ([string]::IsNullOrWhiteSpace($inputVersion)) {
        $Version = $suggestedVersion
    } else {
        $Version = $inputVersion.Trim()
    }
}

if (-not ($Version -match '^\d+\.\d+\.\d+(\.\d+)?$')) {
    Write-Error "El formato de versión '$Version' es inválido. Debe ser como 1.0.1 o 1.0.1.0."
    exit 1
}

Write-Host "Compilando versión: v$Version" -ForegroundColor Green

# 2. Actualizar versión en PuntoDeVentaLibreria.UI.csproj
$csprojContent = Get-Content $projectPath -Raw
$csprojContent = [regex]::Replace($csprojContent, '<Version>[^<]+</Version>', "<Version>$Version</Version>")
$csprojContent = [regex]::Replace($csprojContent, '<AssemblyVersion>[^<]+</AssemblyVersion>', "<AssemblyVersion>$Version.0</AssemblyVersion>")
$csprojContent = [regex]::Replace($csprojContent, '<FileVersion>[^<]+</FileVersion>', "<FileVersion>$Version.0</FileVersion>")
Set-Content -Path $projectPath -Value $csprojContent -Encoding UTF8
Write-Host "Metadatos de versión actualizados en PuntoDeVentaLibreria.UI.csproj" -ForegroundColor DarkGray

# 3. Preparar directorios de salida
$distDir = Join-Path $rootDir "dist\Release_v$Version"
$tempPublishDir = Join-Path $distDir "temp_publish"
$zipOutput = Join-Path $distDir "MR_SYS_Libreria_v$Version.zip"

if (Test-Path $distDir) {
    Write-Host "Limpiando directorio anterior $distDir..." -ForegroundColor DarkGray
    Remove-Item -Path $distDir -Recurse -Force
}
New-Item -ItemType Directory -Path $tempPublishDir -Force | Out-Null

# 4. Publicación de .NET
$selfContainedStr = if ($SelfContained) { "true" } else { "false" }
Write-Host "Compilando y publicando en Release win-x64 (SelfContained=$selfContainedStr)..." -ForegroundColor Cyan

$publishArgs = @(
    "publish",
    $projectPath,
    "-c", "Release",
    "-r", "win-x64",
    "--self-contained", $selfContainedStr,
    "-p:PublishSingleFile=false",
    "-p:Version=$Version",
    "-p:AssemblyVersion=$Version.0",
    "-p:FileVersion=$Version.0",
    "-o", $tempPublishDir
)

& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    Write-Error "Error durante la compilación/publicación de dotnet."
    exit $LASTEXITCODE
}

# 5. PURGA ESTRICTA ANTI-SOBRESCRITURA DE DATOS
Write-Host "Verificando y purgando archivos de datos de usuario de la compilación..." -ForegroundColor Cyan
$patternsToDelete = @(
    "*.db",
    "*.db-shm",
    "*.db-wal",
    "*.sqlite",
    "*.log"
)

foreach ($pattern in $patternsToDelete) {
    Get-ChildItem -Path $tempPublishDir -Filter $pattern -Recurse -File -ErrorAction SilentlyContinue | ForEach-Object {
        Write-Host "  [Seguridad] Eliminado archivo de BD o log: $($_.Name)" -ForegroundColor Yellow
        Remove-Item $_.FullName -Force
    }
}

$dirsToDelete = @("backups_db", "Backups", "Tickets", "Logos")
foreach ($dir in $dirsToDelete) {
    $targetDir = Join-Path $tempPublishDir $dir
    if (Test-Path $targetDir) {
        Write-Host "  [Seguridad] Eliminada carpeta local: $dir" -ForegroundColor Yellow
        Remove-Item $targetDir -Recurse -Force
    }
}

# 6. Comprimir a ZIP
Write-Host "Comprimiendo paquete de actualización a $zipOutput..." -ForegroundColor Cyan
Compress-Archive -Path "$tempPublishDir\*" -DestinationPath $zipOutput -CompressionLevel Optimal

# Eliminar carpeta temporal dejando sólo el ZIP
Remove-Item -Path $tempPublishDir -Recurse -Force

# 7. Calcular Hash SHA256 y tamaño
$hash = (Get-FileHash -Path $zipOutput -Algorithm SHA256).Hash
$sizeMb = [math]::Round(((Get-Item $zipOutput).Length / 1MB), 2)

Write-Host ""
Write-Host "========================================================" -ForegroundColor Green
Write-Host "  PAQUETE DE ACTUALIZACION GENERADO CON EXITO!          " -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Green
Write-Host "Archivo generado: $zipOutput" -ForegroundColor White
Write-Host "Tamaño:           $sizeMb MB" -ForegroundColor White
Write-Host "Hash SHA256:      $hash" -ForegroundColor DarkGray
Write-Host ""
Write-Host "PASOS PARA PUBLICAR EN GITHUB RELEASES:" -ForegroundColor Yellow
Write-Host "1. Abre tu navegador e ingresa a:" -ForegroundColor White
Write-Host "   https://github.com/mrrichar11/PosLibreria/releases/new" -ForegroundColor Cyan
Write-Host "2. En 'Choose a tag', escribe: v$Version (y selecciona 'Create new tag: v$Version on publish')" -ForegroundColor White
Write-Host "3. En 'Release title', escribe: MR SYS Libreria v$Version" -ForegroundColor White
Write-Host "4. En la descripción escribe las novedades que implementaste (ej: Cierre de caja, Arqueo, Stock, Combos)" -ForegroundColor White
Write-Host "5. Arrastra y suelta el archivo ZIP generado:" -ForegroundColor White
Write-Host "   $zipOutput" -ForegroundColor Green
Write-Host "6. Haz clic en 'Publish release'." -ForegroundColor White
Write-Host ""
Write-Host "A partir de ese momento, cualquier terminal con el sistema abierto" -ForegroundColor Cyan
Write-Host "detectará v$Version automáticamente y ofrecerá actualizar en 1 clic!" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan
