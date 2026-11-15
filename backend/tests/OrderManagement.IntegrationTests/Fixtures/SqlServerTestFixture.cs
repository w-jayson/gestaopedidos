using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace OrderManagement.IntegrationTests.Fixtures;

// SqlServerTestFixture (Fixture de testes com SQL Server real):
// sobe um container SQL Server isolado via Testcontainers e aplica os scripts da pasta database/.
public class SqlServerTestFixture : IAsyncLifetime
{
    // _container (container Docker): instância real do SQL Server 2022 usada durante os testes de integração.
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    // ConnectionString (string de conexão): aponta para o banco OrderManagementDb dentro do container.
    public string ConnectionString { get; private set; } = string.Empty;

    // _repositoryRoot (raiz do repositório): caminho usado para localizar database/01_schema.sql e 02_seed.sql.
    private readonly string _repositoryRoot = FindRepositoryRoot();

    // InitializeAsync (Inicializar): executado automaticamente pelo xUnit antes da bateria de testes.
    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Cria o banco OrderManagementDb conectando inicialmente no master.
        await using (var masterConnection = new SqlConnection(_container.GetConnectionString()))
        {
            await masterConnection.OpenAsync();
            await using var createDbCommand = masterConnection.CreateCommand();
            createDbCommand.CommandText = "IF DB_ID(N'OrderManagementDb') IS NULL CREATE DATABASE OrderManagementDb;";
            await createDbCommand.ExecuteNonQueryAsync();
        }

        // Constrói a connection string final já apontando para OrderManagementDb.
        var builder = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = "OrderManagementDb"
        };
        ConnectionString = builder.ConnectionString;

        // Aplica schema e massa inicial diretamente a partir dos scripts oficiais do projeto.
        await ExecuteSqlScriptAsync(Path.Combine(_repositoryRoot, "database", "01_schema.sql"));
        await ExecuteSqlScriptAsync(Path.Combine(_repositoryRoot, "database", "02_seed.sql"));
    }

    // CreateFactory (Criar fábrica da API): inicializa a API real em memória apontando para o SQL Server de teste.
    public WebApplicationFactory<Program> CreateFactory()
    {
        return new CustomWebApplicationFactory(ConnectionString);
    }

    // ResetSeedDataAsync (Restaurar dados iniciais): limpa e recarrega o seed para garantir testes independentes.
    public async Task ResetSeedDataAsync()
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var cleanupCommand = connection.CreateCommand();
        cleanupCommand.CommandText = """
            DELETE FROM dbo.ItensPedido;
            DELETE FROM dbo.Pedidos;
            DELETE FROM dbo.Produtos;
            DELETE FROM dbo.Clientes;
            """;
        await cleanupCommand.ExecuteNonQueryAsync();

        await ExecuteSqlScriptAsync(Path.Combine(_repositoryRoot, "database", "02_seed.sql"));
    }

    // DisposeAsync (Finalizar): derruba e remove o container Docker ao término dos testes.
    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    // ExecuteSqlScriptAsync (Executar script SQL): lê um arquivo .sql do disco e executa no SQL Server de teste.
    private async Task ExecuteSqlScriptAsync(string scriptPath)
    {
        var sql = await File.ReadAllTextAsync(scriptPath);

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    // FindRepositoryRoot (Encontrar raiz do repositório): sobe diretórios a partir da pasta bin/ até achar database/01_schema.sql.
    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var schemaPath = Path.Combine(current.FullName, "database", "01_schema.sql");
            if (File.Exists(schemaPath))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Não foi possível localizar a pasta 'database/01_schema.sql' a partir do diretório de execução.");
    }

    // CustomWebApplicationFactory (Fábrica customizada): injeta a connection string do Testcontainers na API.
    private sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _connectionString;

        public CustomWebApplicationFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
        }
    }
}
