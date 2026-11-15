using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Persistence;

// AppDbContext (Contexto de banco da aplicação): mapeia as entidades de domínio em inglês
// para as tabelas e colunas em português no SQL Server usando Entity Framework Core.
public class AppDbContext : DbContext
{
    // Construtor que recebe as opções configuradas (como connection string do SQL Server).
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // Customers (Clientes): representa a tabela dbo.Clientes.
    public DbSet<Customer> Customers => Set<Customer>();

    // Products (Produtos): representa a tabela dbo.Produtos.
    public DbSet<Product> Products => Set<Product>();

    // Orders (Pedidos): representa a tabela dbo.Pedidos.
    public DbSet<Order> Orders => Set<Order>();

    // OrderItems (Itens do pedido): representa a tabela dbo.ItensPedido.
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    // OnModelCreating (Ao criar o modelo): configura explicitamente nomes de tabelas, colunas e relacionamentos.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Mapeamento da entidade Customer -> tabela Clientes.
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Clientes", "dbo");
            entity.HasKey(customer => customer.Id);
            entity.Property(customer => customer.Name).HasColumnName("Nome").HasMaxLength(150).IsRequired();
            entity.Property(customer => customer.Email).HasColumnName("Email").HasMaxLength(200).IsRequired();
            entity.HasIndex(customer => customer.Email).IsUnique().HasDatabaseName("UX_Clientes_Email");
        });

        // Mapeamento da entidade Product -> tabela Produtos.
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Produtos", "dbo");
            entity.HasKey(product => product.Id);
            entity.Property(product => product.Name).HasColumnName("Nome").HasMaxLength(150).IsRequired();
            entity.Property(product => product.Price).HasColumnName("Preco").HasPrecision(18, 2).IsRequired();
            entity.Property(product => product.Stock).HasColumnName("Estoque").IsRequired();
        });

        // Mapeamento da entidade Order -> tabela Pedidos.
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Pedidos", "dbo");
            entity.HasKey(order => order.Id);
            entity.Property(order => order.CustomerId).HasColumnName("ClienteId").IsRequired();
            entity.Property(order => order.OrderDate).HasColumnName("DataPedido").IsRequired();
            entity.Property(order => order.TotalAmount).HasColumnName("ValorTotal").HasPrecision(18, 2).IsRequired();

            entity.HasOne(order => order.Customer)
                .WithMany()
                .HasForeignKey(order => order.CustomerId);
        });

        // Mapeamento da entidade OrderItem -> tabela ItensPedido.
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("ItensPedido", "dbo");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.OrderId).HasColumnName("PedidoId").IsRequired();
            entity.Property(item => item.ProductId).HasColumnName("ProdutoId").IsRequired();
            entity.Property(item => item.Quantity).HasColumnName("Quantidade").IsRequired();
            entity.Property(item => item.UnitPrice).HasColumnName("PrecoUnitario").HasPrecision(18, 2).IsRequired();

            entity.HasOne(item => item.Order)
                .WithMany(order => order.Items)
                .HasForeignKey(item => item.OrderId);

            entity.HasOne(item => item.Product)
                .WithMany()
                .HasForeignKey(item => item.ProductId);
        });
    }
}
