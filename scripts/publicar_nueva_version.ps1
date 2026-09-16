<#
.SYNOPSIS
    Script directo para publicar nueva versión de MR SYS Librería.
#>

param(
    [Parameter(Position = 0)]
    [string]$Version = "",

    [Parameter(Position = 1)]
    [string]$Notas = "",

    [switch]$SelfContained = $false
)

$script = Join-Path $PSScriptRoot "package_github_release.ps1"
& $script -Version $Version -Notas $Notas -SelfContained:$SelfContained
