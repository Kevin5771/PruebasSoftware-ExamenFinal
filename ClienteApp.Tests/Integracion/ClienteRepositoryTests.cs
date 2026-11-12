
using ClienteApp.Core.Data;
using ClienteApp.Core.Repositories;
using ClienteApp.Core.Services;

namespace ClienteApp.Tests.Integracion;

public class ClienteRepositoryTests : IDisposable
{
    private readonly string _dbPath;
    private readonly string _connectionString;
    private readonly ClienteRepository _repository;

    public ClienteRepositoryTests()
    {
        // Crear una base de datos exclusiva para esta prueba.
        _dbPath = Path.Combine(
            Path.GetTempPath(),
            $"clientes_test_{Guid.NewGuid():N}.db");

        _connectionString = $"Data Source={_dbPath};Pooling=False";

        // Crear la tabla en la base de datos de prueba.
        Database.Inicializar(_connectionString);

        // Utilizar el repositorio real.
        _repository = new ClienteRepository(_connectionString);
    }

    [Fact]
    public void Registrar_ClienteValido_DebePersistirEnSQLite()
    {
        // Arrange: preparar el servicio y los datos.
        var service = new ClienteService(_repository);

        // Act: registrar un cliente.
        var cliente = service.Registrar(
            "Ana López",
            "ana@example.com");

        // Assert: recuperar el cliente desde SQLite.
        var clienteGuardado =
            _repository.ObtenerPorId(cliente.Id);

        Assert.NotNull(clienteGuardado);
        Assert.Equal(cliente.Id, clienteGuardado.Id);
        Assert.Equal("Ana López", clienteGuardado.Nombre);
        Assert.Equal(
            "ana@example.com",
            clienteGuardado.Correo);
    }

    [Fact]
    public void Registrar_CorreoDuplicado_DebeRechazarSegundoCliente()
    {
        // Arrange: preparar el servicio y registrar el primer cliente.
        var service = new ClienteService(_repository);

        service.Registrar(
            "Ana López",
            "ana@example.com");

        // Act + Assert: el segundo registro debe ser rechazado.
        var excepcion = Assert.Throws<InvalidOperationException>(
            () => service.Registrar(
                "Carlos Pérez",
                "ana@example.com"));

        Assert.Equal(
            "El correo ya está registrado.",
            excepcion.Message);
    }


    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void ObtenerPorId_BaseVacia_DebeRetornarNull(int id)
    {
        var service = new ClienteService(_repository);

        Assert.Null(service.ObtenerPorId(id));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void ObtenerPorId_IdInexistenteConClientesRegistrados_DebeRetornarNull(int id)
    {
        var service = new ClienteService(_repository);
        service.Registrar("Ana López", "ana@example.com");
        service.Registrar("Carlos Pérez", "carlos@example.com");

        Assert.Null(service.ObtenerPorId(id));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 2)]
    [InlineData(1, 100)]
    [InlineData(2, 1)]
    [InlineData(2, 2)]
    [InlineData(2, 100)]
    [InlineData(5, 1)]
    [InlineData(5, 2)]
    [InlineData(5, 100)]
    [InlineData(10, 1)]
    [InlineData(10, 2)]
    [InlineData(10, 100)]
    [InlineData(20, 1)]
    [InlineData(20, 2)]
    [InlineData(20, 100)]
    public void ObtenerPorId_IdPosteriorAlUltimoRegistrado_DebeRetornarNull(
        int cantidad, int distancia)
    {
        var service = new ClienteService(_repository);
        var ultimo = service.Registrar("Cliente 1", "cliente1@example.com");
        for (var numero = 2; numero <= cantidad; numero++)
        {
            ultimo = service.Registrar($"Cliente {numero}", $"cliente{numero}@example.com");
        }

        Assert.Null(service.ObtenerPorId(ultimo.Id + distancia));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 1)]
    [InlineData(3, 2)]
    [InlineData(3, 3)]
    [InlineData(5, 1)]
    [InlineData(5, 2)]
    [InlineData(5, 3)]
    [InlineData(5, 4)]
    [InlineData(5, 5)]
    [InlineData(10, 1)]
    [InlineData(10, 3)]
    [InlineData(10, 5)]
    [InlineData(10, 8)]
    [InlineData(10, 10)]
    public void ObtenerPorId_IdEliminado_DebeRetornarNull(int cantidad, int posicion)
    {
        var service = new ClienteService(_repository);
        var ids = Enumerable.Range(1, cantidad)
            .Select(numero => service.Registrar($"Cliente {numero}", $"cliente{numero}@example.com").Id)
            .ToArray();
        var idEliminado = ids[posicion - 1];

        Assert.NotNull(service.ObtenerPorId(idEliminado));

        using var conexion = Database.ObtenerConexion(_connectionString);
        using var comando = conexion.CreateCommand();
        comando.CommandText = "DELETE FROM Clientes WHERE Id = $id";
        comando.Parameters.AddWithValue("$id", idEliminado);
        Assert.Equal(1, comando.ExecuteNonQuery());

        Assert.Null(service.ObtenerPorId(idEliminado));
        foreach (var id in ids.Where(id => id != idEliminado))
        {
            Assert.Equal(id, Assert.IsType<ClienteApp.Core.Models.Cliente>(service.ObtenerPorId(id)).Id);
        }
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
}
