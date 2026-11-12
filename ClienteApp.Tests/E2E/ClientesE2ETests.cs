using System.Net;
using System.Net.Http.Json;
using ClienteApp.Tests.Api;

namespace ClienteApp.Tests.E2E;

public class ClientesE2ETests
{
    [Theory]
    [InlineData(" Ana López ", " ana@example.com ", "Ana López", "ana@example.com")]
    [InlineData("\tJosé Núñez\t", "jose@example.com", "José Núñez", "jose@example.com")]
    public async Task RegistrarYConsultar_ConEspacios_DebePersistirDatosRecortados(
        string nombre, string correo, string nombreEsperado, string correoEsperado)
    {
        using var factory = new ClienteApiFactory();
        using var client = factory.CreateClient();
        using var registro = await client.PostAsJsonAsync("/api/Clientes", new { nombre, correo });

        Assert.Equal(HttpStatusCode.Created, registro.StatusCode);
        var location = registro.Headers.Location;
        Assert.NotNull(location);
        var creado = await registro.Content.ReadFromJsonAsync<ClienteRespuesta>();
        Assert.NotNull(creado);
        var esperado = new ClienteRespuesta(creado.Id, nombreEsperado, correoEsperado);
        Assert.Equal(esperado, creado);

        using var consulta = await client.GetAsync(location);

        Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);
        Assert.Equal(esperado, await consulta.Content.ReadFromJsonAsync<ClienteRespuesta>());
    }

    [Theory]
    [InlineData("", "ana@example.com")]
    [InlineData("Ana", "correo-invalido")]
    public async Task Registrar_SolicitudRechazada_DebePermitirRegistroCorregido(
        string nombre, string correo)
    {
        using var factory = new ClienteApiFactory();
        using var client = factory.CreateClient();
        using var invalido = await client.PostAsJsonAsync("/api/Clientes", new { nombre, correo });
        Assert.Equal(HttpStatusCode.BadRequest, invalido.StatusCode);

        using var consultaInicial = await client.GetAsync("/api/Clientes/1");
        Assert.Equal(HttpStatusCode.NotFound, consultaInicial.StatusCode);

        var corregido = new { Nombre = "Ana", Correo = "ana@example.com" };
        using var registro = await client.PostAsJsonAsync("/api/Clientes", corregido);
        Assert.Equal(HttpStatusCode.Created, registro.StatusCode);
        var location = registro.Headers.Location;
        Assert.NotNull(location);
        var creado = await registro.Content.ReadFromJsonAsync<ClienteRespuesta>();
        Assert.NotNull(creado);

        using var consulta = await client.GetAsync(location);

        Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);
        Assert.Equal(
            new ClienteRespuesta(creado.Id, corregido.Nombre, corregido.Correo),
            await consulta.Content.ReadFromJsonAsync<ClienteRespuesta>());
    }

    [Fact]
    public async Task RegistrarYConsultarCliente_UrlDeLocation_DebeDevolverDatosGuardados()
    {
        using var factory = new ClienteApiFactory();
        using var client = factory.CreateClient();
        var solicitud = new { Nombre = "Ana López", Correo = "ana@example.com" };

        using var registro = await client.PostAsJsonAsync("/api/Clientes", solicitud);

        Assert.Equal(HttpStatusCode.Created, registro.StatusCode);
        var location = registro.Headers.Location;
        Assert.NotNull(location);
        var creado = await registro.Content.ReadFromJsonAsync<ClienteRespuesta>();
        Assert.NotNull(creado);
        Assert.True(creado.Id > 0);

        using var consulta = await client.GetAsync(location);

        Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);
        var recuperado = await consulta.Content.ReadFromJsonAsync<ClienteRespuesta>();
        Assert.Equal(new ClienteRespuesta(creado.Id, solicitud.Nombre, solicitud.Correo), recuperado);
    }

    [Fact]
    public async Task RegistrarClienteDuplicado_DebeRetornar409YConservarElOriginal()
    {
        using var factory = new ClienteApiFactory();
        using var client = factory.CreateClient();
        var original = new { Nombre = "Ana López", Correo = "ana@example.com" };

        using var registro = await client.PostAsJsonAsync("/api/Clientes", original);
        Assert.Equal(HttpStatusCode.Created, registro.StatusCode);
        var location = registro.Headers.Location;
        Assert.NotNull(location);
        var creado = await registro.Content.ReadFromJsonAsync<ClienteRespuesta>();
        Assert.NotNull(creado);

        using var duplicado = await client.PostAsJsonAsync(
            "/api/Clientes", new { Nombre = "Otro cliente", original.Correo });
        Assert.Equal(HttpStatusCode.Conflict, duplicado.StatusCode);

        using var consulta = await client.GetAsync(location);
        Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);
        var recuperado = await consulta.Content.ReadFromJsonAsync<ClienteRespuesta>();
        Assert.Equal(new ClienteRespuesta(creado.Id, original.Nombre, original.Correo), recuperado);
    }

    private sealed record ClienteRespuesta(int Id, string Nombre, string Correo);
}
