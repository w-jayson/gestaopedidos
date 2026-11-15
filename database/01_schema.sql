-- ============================================================================
-- Script 01: Criação idempotente do schema e índices (SQL Server)
-- Projeto: Mini Gestão de Pedidos
-- ============================================================================

-- 1. Tabela Clientes: armazena os dados cadastrais básicos dos clientes.
IF OBJECT_ID(N'dbo.Clientes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Clientes
    (
        Id INT IDENTITY(1, 1) NOT NULL, -- Identificador único autoincremental
        Nome NVARCHAR(150) NOT NULL,    -- Nome completo do cliente
        Email NVARCHAR(200) NOT NULL,   -- E-mail único para evitar ambiguidade entre homônimos

        CONSTRAINT PK_Clientes PRIMARY KEY CLUSTERED (Id)
    );
END;

-- Índice único em Email: impede cadastro duplicado e acelera buscas por e-mail.
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_Clientes_Email'
      AND object_id = OBJECT_ID(N'dbo.Clientes')
)
BEGIN
    EXEC(N'CREATE UNIQUE NONCLUSTERED INDEX UX_Clientes_Email ON dbo.Clientes (Email);');
END;

-- 2. Tabela Produtos: catálogo de produtos com preço unitário atual e estoque.
IF OBJECT_ID(N'dbo.Produtos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Produtos
    (
        Id INT IDENTITY(1, 1) NOT NULL, -- Identificador único do produto
        Nome NVARCHAR(150) NOT NULL,    -- Nome do produto
        Preco DECIMAL(18, 2) NOT NULL,  -- Preço unitário vigente no catálogo
        Estoque INT NOT NULL,           -- Quantidade disponível em estoque

        CONSTRAINT PK_Produtos PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_Produtos_Preco CHECK (Preco >= 0),
        CONSTRAINT CK_Produtos_Estoque CHECK (Estoque >= 0)
    );
END;

-- 3. Tabela Pedidos: cabeçalho de cada pedido associado a um cliente.
IF OBJECT_ID(N'dbo.Pedidos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Pedidos
    (
        Id INT IDENTITY(1, 1) NOT NULL,    -- Número do pedido
        ClienteId INT NOT NULL,            -- Chave estrangeira para Clientes
        DataPedido DATETIME2 NOT NULL,     -- Data/hora do pedido em UTC
        ValorTotal DECIMAL(18, 2) NOT NULL,-- Valor total calculado no servidor

        CONSTRAINT PK_Pedidos PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Pedidos_Clientes FOREIGN KEY (ClienteId)
            REFERENCES dbo.Clientes (Id),
        CONSTRAINT CK_Pedidos_ValorTotal CHECK (ValorTotal >= 0)
    );
END;

-- Índice composto em Pedidos(DataPedido DESC, ClienteId):
-- acelera filtros sargáveis por período/cliente na listagem paginada e no relatório Top 5.
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Pedidos_DataPedido_ClienteId'
      AND object_id = OBJECT_ID(N'dbo.Pedidos')
)
BEGIN
    EXEC(N'CREATE NONCLUSTERED INDEX IX_Pedidos_DataPedido_ClienteId ON dbo.Pedidos (DataPedido DESC, ClienteId);');
END;

-- 4. Tabela ItensPedido: itens de cada pedido com quantidade e preço congelado na compra.
IF OBJECT_ID(N'dbo.ItensPedido', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ItensPedido
    (
        Id INT IDENTITY(1, 1) NOT NULL,       -- Identificador único do item
        PedidoId INT NOT NULL,                -- Chave estrangeira para Pedidos
        ProdutoId INT NOT NULL,               -- Chave estrangeira para Produtos
        Quantidade INT NOT NULL,              -- Quantidade comprada (sempre > 0)
        PrecoUnitario DECIMAL(18, 2) NOT NULL,-- Preço unitário copiado de Produtos no momento da compra

        CONSTRAINT PK_ItensPedido PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_ItensPedido_Pedidos FOREIGN KEY (PedidoId)
            REFERENCES dbo.Pedidos (Id),
        CONSTRAINT FK_ItensPedido_Produtos FOREIGN KEY (ProdutoId)
            REFERENCES dbo.Produtos (Id),
        CONSTRAINT CK_ItensPedido_Quantidade CHECK (Quantidade > 0),
        CONSTRAINT CK_ItensPedido_PrecoUnitario CHECK (PrecoUnitario >= 0)
    );
END;

-- Índice em ItensPedido(PedidoId) incluindo Quantidade e PrecoUnitario:
-- cobre a agregação SUM(Quantidade * PrecoUnitario) no relatório Top 5 sem Key Lookup na tabela base.
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ItensPedido_PedidoId'
      AND object_id = OBJECT_ID(N'dbo.ItensPedido')
)
BEGIN
    EXEC(N'CREATE NONCLUSTERED INDEX IX_ItensPedido_PedidoId ON dbo.ItensPedido (PedidoId) INCLUDE (Quantidade, PrecoUnitario);');
END;

-- Índice em ItensPedido(ProdutoId): otimiza junções e verificações por produto.
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ItensPedido_ProdutoId'
      AND object_id = OBJECT_ID(N'dbo.ItensPedido')
)
BEGIN
    EXEC(N'CREATE NONCLUSTERED INDEX IX_ItensPedido_ProdutoId ON dbo.ItensPedido (ProdutoId);');
END;
