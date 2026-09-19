<#
.SYNOPSIS
    Generador de Claves de Activación y Licencias para MR SYS Librería & Regalería.

.DESCRIPTION
    Genera claves de suscripción válidas para activar el sistema por:
    - 30 días (Plan Mensual)
    - 180 días (Plan Semestral)
    - 365 días (Plan Anual)

.PARAMETER Tipo
    Tipo de licencia: 'Mensual', 'Semestral' o 'Anual'. Por defecto 'Mensual'.

.PARAMETER Cliente
    Nombre del comercio o cliente (opcional, para registro/bitácora).
#>

[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet("Mensual", "Semestral", "Anual")]
    [string]$Tipo = "Mensual",

    [Parameter(Position = 1)]
    [string]$Cliente = "Comercio"
)

function GenerarSegmentoHex([int]$longitud = 4) {
    $bytes = New-Object byte[] ($longitud / 2)
    (New-Object System.Security.Cryptography.RNGCryptoServiceProvider).GetBytes($bytes)
    return [System.BitConverter]::ToString($bytes) -replace '-', ''
}

$seg1 = GenerarSegmentoHex 4
$seg2 = GenerarSegmentoHex 4
$seg3 = GenerarSegmentoHex 4

$clave = ""
$dias = 0
$planNombre = ""

switch ($Tipo) {
    "Mensual" {
        $clave = "MES-$seg1-$seg2-$seg3"
        $dias = 30
        $planNombre = "Plan Mensual (30 días)"
    }
    "Semestral" {
        $clave = "SEMESTRE-$seg1-$seg2-$seg3"
        $dias = 180
        $planNombre = "Plan Semestral (180 días)"
    }
    "Anual" {
        $clave = "ANUAL-$seg1-$seg2-$seg3"
        $dias = 365
        $planNombre = "Plan Anual (365 días)"
    }
}

$fechaVigencia = (Get-Date).AddDays($dias).ToString("dd/MM/yyyy")

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  GENERADOR DE LICENCIAS OFICIALES - MR SYS LIBRERIA        " -ForegroundColor Yellow
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "Cliente / Comercio:  $Cliente" -ForegroundColor White
Write-Host "Plan Seleccionado:   $planNombre" -ForegroundColor Green
Write-Host "Días de Cobertura:   $dias días (Vigente aprox hasta $fechaVigencia)" -ForegroundColor DarkGray
Write-Host ""
Write-Host "CLAVE DE ACTIVACION:" -ForegroundColor Yellow
Write-Host "  $clave" -ForegroundColor Green
Write-Host ""
Write-Host "Instrucciones para el cliente:" -ForegroundColor DarkGray
Write-Host "1. Abrir MR SYS Librería -> Configuración -> Pestaña 'Plan Mensual & Licencia'." -ForegroundColor DarkGray
Write-Host "2. Pegar la clave '$clave' en el campo 'Clave de Activación' y hacer clic en Activar." -ForegroundColor DarkGray
Write-Host "============================================================" -ForegroundColor Cyan

# Copiar al portapapeles si está disponible
try {
    Set-Clipboard -Value $clave
    Write-Host " (¡Clave copiada automáticamente al portapapeles!)" -ForegroundColor Cyan
} catch {}
