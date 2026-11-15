using System.Data;
using Microsoft.Data.SqlClient;

namespace OrderManagement.Infrastructure.Persistence;

// SqlConnectionFactory (Fábrica de conexões SQL): centraliza a criação de conexões SqlConnection para o Dapper.
public class SqlConnectionFactory
{
    // _connectionString (string de conexão): endereço e credenciais do SQL Server.
    private readonly string _connectionString;

    // Construtor que recebe a string de conexão configurada na API ou nos testes.
    public SqlConnectionFactory(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("A connection string do SQL Server não pode ser vazia.", nameof(connectionString));
        }

        _connectionString = connectionString;
    }

    // CreateConnection (Criar conexão): devolve uma nova conexão pronta para uso pelo Dapper.
    public IDbConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}
