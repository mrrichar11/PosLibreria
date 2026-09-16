using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using PuntoDeVentaLibreria.Application.DTOs.Sistema;
using PuntoDeVentaLibreria.Application.Services;

namespace PuntoDeVentaLibreria.Infrastructure.Services;

public class GitHubUpdateService : IUpdateService
{
    private readonly HttpClient _httpClient;

    public GitHubUpdateService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    public string ObtenerVersionActual()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version 
                   ?? Assembly.GetExecutingAssembly().GetName().Version;
        return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
    }

    public async Task<ActualizacionDto> VerificarActualizacionesAsync(string owner, string repo, CancellationToken ct = default)
    {
        var versionActualStr = ObtenerVersionActual();
        var resultado = new ActualizacionDto
        {
            VersionActual = versionActualStr,
            HayActualizacion = false
        };

        if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo))
        {
            return resultado;
        }

        try
        {
            var url = $"https://api.github.com/repos/{owner.Trim()}/{repo.Trim()}/releases/latest";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("MR-SYS-Libreria", versionActualStr));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                return resultado;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tagName = root.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() ?? "" : "";
            var versionRemotaStr = tagName.TrimStart('v', 'V').Trim();
            var body = root.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() ?? "" : "";
            
            DateTime? fechaPublicacion = null;
            if (root.TryGetProperty("published_at", out var pubProp) && pubProp.TryGetDateTime(out var dt))
            {
                fechaPublicacion = dt;
            }

            string downloadUrl = string.Empty;
            if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        downloadUrl = asset.TryGetProperty("browser_download_url", out var d) ? d.GetString() ?? "" : "";
                        break;
                    }
                }
            }

            resultado.VersionDisponible = versionRemotaStr;
            resultado.NotasLanzamiento = body;
            resultado.UrlDescargaZip = downloadUrl;
            resultado.FechaPublicacion = fechaPublicacion;

            if (Version.TryParse(versionActualStr, out var vAct) && Version.TryParse(versionRemotaStr, out var vRem))
            {
                resultado.HayActualizacion = vRem > vAct;
            }
            else
            {
                resultado.HayActualizacion = !string.Equals(versionActualStr, versionRemotaStr, StringComparison.OrdinalIgnoreCase);
            }

            return resultado;
        }
        catch
        {
            return resultado;
        }
    }

    public async Task<string> DescargarActualizacionAsync(ActualizacionDto info, IProgress<ProgresoDescargaDto>? progreso = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(info.UrlDescargaZip))
            throw new InvalidOperationException("No hay URL de descarga disponible para esta actualización.");

        var tempDir = Path.Combine(Path.GetTempPath(), "MR_SYS_Libreria_Updates");
        Directory.CreateDirectory(tempDir);
        var destinoZip = Path.Combine(tempDir, $"update_v{info.VersionDisponible}.zip");

        using var response = await _httpClient.GetAsync(info.UrlDescargaZip, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;
        await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
        await using var fileStream = new FileStream(destinoZip, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

        var buffer = new byte[81920];
        long totalBytesLeidos = 0;
        int bytesLeidos;

        while ((bytesLeidos = await contentStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesLeidos, ct);
            totalBytesLeidos += bytesLeidos;

            if (totalBytes.HasValue && progreso != null)
            {
                var porcentaje = (double)totalBytesLeidos / totalBytes.Value * 100;
                progreso.Report(new ProgresoDescargaDto
                {
                    Porcentaje = Math.Round(porcentaje, 1),
                    BytesRecibidos = totalBytesLeidos,
                    TotalBytes = totalBytes,
                    Mensaje = $"Descargando v{info.VersionDisponible}: {porcentaje:N0}%"
                });
            }
        }

        return destinoZip;
    }

    public void IniciarInstalacion(string rutaZipDescargado)
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
        var tempPath = Path.GetTempPath();
        var ps1Path = Path.Combine(tempPath, "apply_update_libreria.ps1");
        var batPath = Path.Combine(tempPath, "launch_update_libreria.bat");

        var psScript = $@"# Script de actualizacion automatica segura para MR SYS Libreria
$ErrorActionPreference = 'Continue'
$logFile = Join-Path $env:TEMP 'mr_sys_update.log'
$zipPath = '{rutaZipDescargado.Replace("'", "''")}'
$appDir = '{appDir.Replace("'", "''")}'

function Log($msg) {{
    $ts = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    ""[$ts] $msg"" | Out-File -FilePath $logFile -Append -Encoding utf8
    Write-Host ""[$ts] $msg""
}}

Log '==================================================='
Log 'INICIANDO PROCESO DE ACTUALIZACION OFICIAL MR SYS'
Log ""Archivo ZIP: $zipPath""
Log ""Directorio Destino: $appDir""

# 1. Esperar cierre seguro del proceso principal
Log 'Esperando a que la aplicacion se cierre completamente...'
$timeoutSec = 15
$timer = [System.Diagnostics.Stopwatch]::StartNew()
while ($timer.Elapsed.TotalSeconds -lt $timeoutSec) {{
    $procs = Get-Process -Name 'PuntoDeVentaLibreria.UI' -ErrorAction SilentlyContinue
    if (-not $procs) {{ break }}
    Start-Sleep -Milliseconds 400
}}

# Cierre forzado si quedo algun hilo bloqueante
$remaining = Get-Process -Name 'PuntoDeVentaLibreria.UI' -ErrorAction SilentlyContinue
if ($remaining) {{
    Log 'Terminando procesos remanentes...'
    Stop-Process -Name 'PuntoDeVentaLibreria.UI' -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
}}

# 2. Respaldo preventivo de la base de datos de la libreria
$backupFolder = Join-Path $appDir 'backups_db\pre_update'
if (-not (Test-Path $backupFolder)) {{
    New-Item -ItemType Directory -Path $backupFolder -Force | Out-Null
}}

$dbFiles = Get-ChildItem -Path $appDir -Filter 'punto_venta_libreria*.db*' -ErrorAction SilentlyContinue
if ($dbFiles) {{
    $ts = Get-Date -Format 'yyyyMMdd_HHmmss'
    foreach ($f in $dbFiles) {{
        $backupDest = Join-Path $backupFolder ""$($f.BaseName)_$ts$($f.Extension)""
        Copy-Item -Path $f.FullName -Destination $backupDest -Force
        Log ""Base de datos respaldada preventivamente en: $backupDest""
    }}
}}

# 3. Descomprimir en carpeta temporal aislada
$extractTempDir = Join-Path $env:TEMP 'MR_SYS_Libreria_Extracted'
if (Test-Path $extractTempDir) {{
    Remove-Item -Path $extractTempDir -Recurse -Force -ErrorAction SilentlyContinue
}}
New-Item -ItemType Directory -Path $extractTempDir -Force | Out-Null

Log 'Descomprimiendo archivos de actualizacion...'
try {{
    Expand-Archive -Path $zipPath -DestinationPath $extractTempDir -Force
    Log 'Descompresion finalizada con exito.'
}} catch {{
    Log ""ERROR al descomprimir: $($_.Exception.Message)""
}}

# 4. PROTECCION ABSOLUTA: Eliminar cualquier archivo .db que venga dentro del ZIP
$extractedDbs = Get-ChildItem -Path $extractTempDir -Recurse -Include '*.db', '*.db-wal', '*.db-shm' -ErrorAction SilentlyContinue
foreach ($edb in $extractedDbs) {{
    Remove-Item -Path $edb.FullName -Force -ErrorAction SilentlyContinue
    Log ""Seguridad: descartado archivo $($edb.Name) del paquete para salvaguardar base de datos local.""
}}

# 5. Detectar directorio fuente (por si el ZIP empaqueto una subcarpeta raiz)
$sourceDir = $extractTempDir
$exeInRoot = Join-Path $sourceDir 'PuntoDeVentaLibreria.UI.exe'
if (-not (Test-Path $exeInRoot)) {{
    $foundExe = Get-ChildItem -Path $extractTempDir -Filter 'PuntoDeVentaLibreria.UI.exe' -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($foundExe) {{
        $sourceDir = $foundExe.DirectoryName
        Log ""Subcarpeta detectada en ZIP: $sourceDir""
    }}
}}

# 6. Copiar los archivos actualizados hacia el directorio de la aplicacion
Log ""Copiando archivos nuevos a $appDir...""
Get-ChildItem -Path $sourceDir -Recurse | ForEach-Object {{
    if ($_.Extension -match '^\.db.*$') {{ return }}
    $rel = $_.FullName.Substring($sourceDir.Length).TrimStart('\', '/')
    $dest = Join-Path $appDir $rel
    if ($_.PSIsContainer) {{
        if (-not (Test-Path $dest)) {{
            New-Item -ItemType Directory -Path $dest -Force | Out-Null
        }}
    }} else {{
        $parent = Split-Path $dest -Parent
        if (-not (Test-Path $parent)) {{
            New-Item -ItemType Directory -Path $parent -Force | Out-Null
        }}
        Copy-Item -Path $_.FullName -Destination $dest -Force
    }}
}}
Log 'Copia de archivos completada exitosamente.'

# 7. Limpieza de temporales
Remove-Item -Path $extractTempDir -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path $zipPath -Force -ErrorAction SilentlyContinue

# 8. Relanzar el sistema
$finalExe = Join-Path $appDir 'PuntoDeVentaLibreria.UI.exe'
if (Test-Path $finalExe) {{
    Log ""Reiniciando aplicacion: $finalExe""
    Start-Process -FilePath $finalExe
}} else {{
    Log ""AVISO: No se encontro el ejecutable exacto en $finalExe""
}}
Log 'ACTUALIZACION FINALIZADA SATISFACTORIAMENTE.'
";

        File.WriteAllText(ps1Path, psScript, System.Text.Encoding.UTF8);

        var batScript = $@"@echo off
timeout /t 1 /nobreak >nul
powershell -NoProfile -ExecutionPolicy Bypass -File ""{ps1Path}""
del ""%~f0""
";
        File.WriteAllText(batPath, batScript, System.Text.Encoding.ASCII);

        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"{batPath}\"",
            CreateNoWindow = true,
            UseShellExecute = false
        };
        Process.Start(psi);
        Environment.Exit(0);
    }
}
