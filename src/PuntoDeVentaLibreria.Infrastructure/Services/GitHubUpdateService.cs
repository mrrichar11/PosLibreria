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
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var batchPath = Path.Combine(Path.GetTempPath(), "apply_update_libreria.bat");

        var script = $@"@echo off
timeout /t 2 /nobreak >nul
powershell -Command ""Expand-Archive -Path '{rutaZipDescargado}' -DestinationPath '{appDir}' -Force""
start """" ""{Path.Combine(appDir, "PuntoDeVentaLibreria.UI.exe")}""
del ""%~f0""
";
        File.WriteAllText(batchPath, script);

        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"{batchPath}\"",
            CreateNoWindow = true,
            UseShellExecute = false
        };
        Process.Start(psi);
        Environment.Exit(0);
    }
}
