using System.Net;
using System.Text;
using FluentAssertions;
using Moq;
using Moq.Protected;
using PuntoDeVentaLibreria.Infrastructure.Services;
using Xunit;

namespace PuntoDeVentaLibreria.UnitTests;

public class UpdateServiceTests
{
    [Fact]
    public void ObtenerVersionActual_DebeRetornarCadenaValida()
    {
        var service = new GitHubUpdateService();
        var ver = service.ObtenerVersionActual();

        ver.Should().NotBeNullOrWhiteSpace();
        Version.TryParse(ver, out _).Should().BeTrue();
    }

    [Fact]
    public async Task VerificarActualizaciones_CuandoOwnerORepoSonVacios_DebeRetornarFalso()
    {
        var service = new GitHubUpdateService();
        var res = await service.VerificarActualizacionesAsync("", "");

        res.HayActualizacion.Should().BeFalse();
    }

    [Fact]
    public async Task VerificarActualizaciones_CuandoHayNuevaVersionEnGitHub_DebeRetornarVerdadero()
    {
        var json = @"{
            ""tag_name"": ""v99.0.0"",
            ""body"": ""Nuevas funciones de libreria"",
            ""published_at"": ""2026-09-16T12:00:00Z"",
            ""assets"": [
                {
                    ""name"": ""MR_SYS_Libreria_v99.0.0.zip"",
                    ""browser_download_url"": ""https://github.com/mrrichar11/PosLibreria/releases/download/v99.0.0/MR_SYS_Libreria_v99.0.0.zip""
                }
            ]
        }";

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        var client = new HttpClient(mockHandler.Object);
        var service = new GitHubUpdateService(client);

        var res = await service.VerificarActualizacionesAsync("mrrichar11", "PosLibreria");

        res.HayActualizacion.Should().BeTrue();
        res.VersionDisponible.Should().Be("99.0.0");
        res.UrlDescargaZip.Should().Contain(".zip");
        res.NotasLanzamiento.Should().Be("Nuevas funciones de libreria");
    }

    [Fact]
    public async Task VerificarActualizaciones_CuandoVersionEsMenorOIgual_DebeRetornarFalso()
    {
        var json = @"{
            ""tag_name"": ""v0.0.1"",
            ""body"": ""Version antigua"",
            ""published_at"": ""2020-01-01T12:00:00Z"",
            ""assets"": []
        }";

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        var client = new HttpClient(mockHandler.Object);
        var service = new GitHubUpdateService(client);

        var res = await service.VerificarActualizacionesAsync("mrrichar11", "PosLibreria");

        res.HayActualizacion.Should().BeFalse();
    }
}
