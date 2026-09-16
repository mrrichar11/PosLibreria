$WshShell = New-Object -ComObject WScript.Shell
$desktop = [System.Environment]::GetFolderPath('Desktop')
$lnkPath = Join-Path $desktop "MR SYS Libreria.lnk"
$shortcut = $WshShell.CreateShortcut($lnkPath)
$shortcut.TargetPath = "C:\MR_SYS_Libreria\PuntoDeVentaLibreria.UI.exe"
$shortcut.WorkingDirectory = "C:\MR_SYS_Libreria"
$shortcut.Description = "Sistema de Punto de Venta para Libreria & Regaleria"
$shortcut.Save()
Write-Host "Acceso directo creado exitosamente en el Escritorio: $lnkPath" -ForegroundColor Green
