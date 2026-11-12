using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ClienteApp.Tests.Api;

public class ClientesApiContratoTests
{
    [Theory]
    [InlineData("{}")]
    [InlineData("{\"correo\":\"ana@example.com\"}")]
    [InlineData("{\"nombre\":\"Ana\"}")]
    [InlineData("{\"nombre\":null,\"correo\":\"ana@example.com\"}")]
    [InlineData("{\"nombre\":\"Ana\",\"correo\":null}")]
    [InlineData("null")]
    public async Task Registrar_CamposAusentesONulos_DebeRetornar400(string json)
    {
        using var factory = new ClienteApiFactory();
        using var client = factory.CreateClient();
        using var contenido = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("/api/Clientes", contenido);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/Clientes")]
    [InlineData("POST", "/api/Clientes/1")]
    [InlineData("PUT", "/api/Clientes/1")]
    [InlineData("PATCH", "/api/Clientes/1")]
    public async Task Solicitar_MetodoNoAdmitido_DebeRetornar405(string metodo, string ruta)
    {
        using var factory = new ClienteApiFactory();
        using var client = factory.CreateClient();
        using var solicitud = new HttpRequestMessage(new HttpMethod(metodo), ruta);
        using var response = await client.SendAsync(solicitud);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public async Task ObtenerPorId_IdSinCliente_DebeRetornar404ConMensaje(int id)
    {
        using var factory = new ClienteApiFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync($"/api/Clientes/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Cliente no encontrado.", error.GetProperty("mensaje").GetString());
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("1.5")]
    [InlineData("2147483648")]
    [InlineData("-2147483649")]
    public async Task ObtenerPorId_SegmentoFueraDelRangoInt_DebeRetornar404(string segmento)
    {
        using var factory = new ClienteApiFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync($"/api/Clientes/{segmento}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("   ", "ana@example.com", "El nombre es obligatorio.")]
    [InlineData("Ana", "", "El correo no es válido.")]
    [InlineData("Ana", "   ", "El correo no es válido.")]
    [InlineData("Ana", "ana.example.com", "El correo no es válido.")]
    public async Task Registrar_DatosInvalidos_DebeRetornar400ConMensaje(
        string nombre, string correo, string mensaje)
    {
        using var factory = new ClienteApiFactory();
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/Clientes", new { nombre, correo });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(mensaje, error.GetProperty("mensaje").GetString());
    }

    [Theory]
    [InlineData("{")]
    [InlineData("[]")]
    [InlineData("{\"nombre\":123,\"correo\":\"ana@example.com\"}")]
    public async Task Registrar_JsonInvalidoParaElModelo_DebeRetornar400(string json)
    {
        using var factory = new ClienteApiFactory();
        using var client = factory.CreateClient();
        using var contenido = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("/api/Clientes", contenido);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Registrar_ContenidoDeTexto_DebeRetornar415()
    {
        using var factory = new ClienteApiFactory();
        using var client = factory.CreateClient();
        using var contenido = new StringContent("Ana,ana@example.com", Encoding.UTF8, "text/plain");
        using var response = await client.PostAsync("/api/Clientes", contenido);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task Eliminar_EndpointSoloDeConsulta_DebeRetornar405()
    {
        using var factory = new ClienteApiFactory();
        using var client = factory.CreateClient();
        using var response = await client.DeleteAsync("/api/Clientes/1");

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task Registrar_CorreoDuplicadoEnMayusculas_DebeRetornar409ConMensaje()
    {
        using var factory = new ClienteApiFactory();
        using var client = factory.CreateClient();
        using var registro = await client.PostAsJsonAsync(
            "/api/Clientes", new { Nombre = "Ana", Correo = "ana@example.com" });
        Assert.Equal(HttpStatusCode.Created, registro.StatusCode);

        using var response = await client.PostAsJsonAsync(
            "/api/Clientes", new { Nombre = "Otra persona", Correo = "ANA@EXAMPLE.COM" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("El correo ya está registrado.", error.GetProperty("mensaje").GetString());
    }
}
