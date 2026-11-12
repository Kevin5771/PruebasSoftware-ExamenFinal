using ClienteApp.Core.Data;
using ClienteApp.Core.Models;
using ClienteApp.Core.Repositories;
using ClienteApp.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// Registrar servicios de ASP.NET Core
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Registrar dependencias de la aplicación
builder.Services.AddScoped<IClienteRepository>(
    services => new ClienteRepository(
        ObtenerCadenaConexion(services.GetRequiredService<IConfiguration>())));

builder.Services.AddScoped<ClienteService>();

var app = builder.Build();

// Inicializar la base de datos con la conexión configurada
Database.Inicializar(ObtenerCadenaConexion(app.Configuration));

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

static string ObtenerCadenaConexion(IConfiguration configuration) =>
    configuration.GetConnectionString("ClientesDb")
    ?? throw new InvalidOperationException(
        "No se configuró la cadena de conexión ClientesDb.");

public partial class Program { }
