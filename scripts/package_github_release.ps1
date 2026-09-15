<#
.SYNOPSIS
    Empaqueta y genera el archivo ZIP de una nueva versin de Punto de Venta para subir a GitHub Releases.

.DESCRIPTION
    1. Compila y publica la aplicacin WPF en modo Release (win-x64, autocontenida).
    2. Excluye estrictamente archivos de datos de usuario (.db, gemini_key.txt, Backups, Logos, Tickets).
    3. Comprime los archivos en dist/Release_vX.Y.Z/PuntoDeVenta_vX.Y.Z.zip.
    4. Muestra las instrucciones exactas paso a paso para publicarlo en GitHub.

.PARAMETER Version
    Nmero de versin semntica (ej. 1.0.1).
#>

[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$Version = "1.0.1",

    [Parameter(Position = 1)]
    [string]$Notas = ""
)

$ErrorActionPreference = "Stop"

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "  EMPAQUETADOR DE RELEASES - PUNTO DE VENTA (MR SYS)  " -ForegroundColor Yellow
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "Versin a compilar: v$Version" -ForegroundColor Green

# 1. Validar formato de versin
if (-not ($Version -match '^\d+\.\d+\.\d+(\.\d+)?$')) {
    Write-Error "El formato de versin '$Version' es invlido. Debe ser como 1.0.1 o 1.0.1.0."
    exit 1
}

$rootDir = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $rootDir "src\PuntoDeVenta.UI\PuntoDeVenta.UI.csproj"
$distDir = Join-Path $rootDir "dist\Release_v$Version"
$tempPublishDir = Join-Path $distDir "temp_publish"
$zipOutput = Join-Path $distDir "PuntoDeVenta_v$Version.zip"

# Limpieza previa
if (Test-Path $distDir) {
    Write-Host "Limpiando directorio anterior $distDir..." -ForegroundColor DarkGray
    Remove-Item -Path $distDir -Recurse -Force
}

New-Item -ItemType Directory -Path $tempPublishDir -Force | Out-Null

# 2. Publicacin de .NET
Write-Host "Compilando y publicando en Release win-x64 autocontenido..." -ForegroundColor Cyan
$publishArgs = @(
    "publish",
    $projectPath,
    "-c", "Release",
    "-r", "win-x64",
    "--self-contained", "true",
    "-p:PublishSingleFile=false",
    "-p:Version=$Version",
    "-p:AssemblyVersion=$Version.0",
    "-p:FileVersion=$Version.0",
    "-o", $tempPublishDir
)

& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    Write-Error "Error durante la compilacin/publicacin de dotnet."
    exit $LASTEXITCODE
}

# 3. Limpieza de archivos de datos sensibles o carpetas locales que nunca deben distribuirse
Write-Host "Verificando y purgando archivos de datos de usuario..." -ForegroundColor Cyan
$patternsToDelete = @(
    "*.db",
    "*.db-shm",
    "*.db-wal",
    "*.sqlite",
    "gemini_key.txt",
    "*.log"
)

foreach ($pattern in $patternsToDelete) {
    Get-ChildItem -Path $tempPublishDir -Filter $pattern -Recurse -File | ForEach-Object {
        Write-Host "  Eliminando archivo temporal: $($_.Name)" -ForegroundColor Yellow
        Remove-Item $_.FullName -Force
    }
}

$dirsToDelete = @("Backups", "Tickets", "Logos")
foreach ($dir in $dirsToDelete) {
    $targetDir = Join-Path $tempPublishDir $dir
    if (Test-Path $targetDir) {
        Write-Host "  Eliminando carpeta local: $dir" -ForegroundColor Yellow
        Remove-Item $targetDir -Recurse -Force
    }
}

# 4. Comprimir a ZIP
Write-Host "Comprimiendo paquete de actualizacin a $zipOutput..." -ForegroundColor Cyan
Compress-Archive -Path "$tempPublishDir\*" -DestinationPath $zipOutput -CompressionLevel Optimal

# Eliminar carpeta temporal dejando slo el ZIP
Remove-Item -Path $tempPublishDir -Recurse -Force

# Calcular Hash SHA256 y tamao
$hash = (Get-FileHash -Path $zipOutput -Algorithm SHA256).Hash
$sizeMb = [math]::Round(((Get-Item $zipOutput).Length / 1MB), 2)

Write-Host ""
Write-Host "========================================================" -ForegroundColor Green
Write-Host "  PAQUETE DE ACTUALIZACIN GENERADO CON XITO!        " -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Green
Write-Host "Archivo generado: $zipOutput" -ForegroundColor White
Write-Host "Tamao:           $sizeMb MB" -ForegroundColor White
Write-Host "Hash SHA256:      $hash" -ForegroundColor DarkGray
Write-Host ""
Write-Host "PASOS PARA PUBLICAR EN GITHUB RELEASES:" -ForegroundColor Yellow
Write-Host "1. Abre tu navegador y ve a tu repositorio:" -ForegroundColor White
Write-Host "   https://github.com/mrrichar11/puntoVentaMR/releases/new" -ForegroundColor Cyan
Write-Host "2. En 'Choose a tag', escribe: v$Version (crea la etiqueta nueva)" -ForegroundColor White
Write-Host "3. En 'Release title', escribe: MR SYS v$Version" -ForegroundColor White
Write-Host "4. En la descripcin escribe las novedades (ej: 'Mejoras en el cierre de caja y ventas')" -ForegroundColor White
Write-Host "5. Arrastra y suelta el archivo ZIP generado:" -ForegroundColor White
Write-Host "   $zipOutput" -ForegroundColor Green
Write-Host "6. Haz clic en 'Publish release'." -ForegroundColor White
Write-Host ""
Write-Host "Listo! A partir de ese momento, cualquier terminal con el sistema abierto" -ForegroundColor Cyan
Write-Host "detectara la nueva version v$Version automaticamente y ofrecera actualizar con 1 clic." -ForegroundColor Cyan
