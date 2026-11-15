using Microsoft.EntityFrameworkCore;
using OrderManagement.API.Middleware;
using OrderManagement.Application.Interfaces;
using OrderManagement.Application.Services;
using OrderManagement.Infrastructure.Persistence;
using OrderManagement.Infrastructure.Repositories;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// connectionString (string de conexão): lê o endereço do SQL Server configurado no appsettings ou variáveis de ambiente.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not configured.");

// Registro do EF Core (escrita transacional) e da fábrica de conexões Dapper (leitura/relatórios).
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddSingleton(_ => new SqlConnectionFactory(connectionString));

// Registro das dependências das camadas Infrastructure e Application.
builder.Services.AddScoped<IOrderCommandRepository, OrderCommandRepository>();
builder.Services.AddScoped<IOrderReadRepository, OrderReadRepository>();
builder.Services.AddScoped<ICatalogReadRepository, CatalogReadRepository>();
builder.Services.AddScoped<OrderService>();

// Padronização de erros HTTP via ProblemDetails (RFC 7807) e tratador global de exceções.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddControllers();

// Documentação da API com OpenAPI nativo do .NET 10 + interface Scalar (sem Swagger).
builder.Services.AddOpenApi();

var app = builder.Build();

// Middleware de exceções deve vir no início do pipeline para capturar falhas de qualquer endpoint.
app.UseExceptionHandler();

// Expõe o documento OpenAPI (/openapi/v1.json) e a UI interativa Scalar (/scalar/v1).
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title = "Mini Gestão de Pedidos API";
});

// Endpoint simples de saúde usado pelo Docker Compose para saber quando a API está pronta.
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
   .WithTags("Health");

app.MapControllers();

app.Run();

// Classe parcial pública necessária para os testes de integração usarem WebApplicationFactory<Program>.
public partial class Program
{
}
