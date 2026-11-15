-- ============================================================================
-- Script 02: Carga inicial idempotente de dados de exemplo (SQL Server)
-- Projeto: Mini Gestão de Pedidos
-- ============================================================================

-- 1. Carga de Clientes (somente se a tabela estiver vazia)
-- Inclui dois clientes homônimos ("Ana Silva"), um cliente com pedido apenas
-- em 2025 e um cliente sem pedidos, para exercitar todos os cenários da Parte 4.
IF NOT EXISTS (SELECT 1 FROM dbo.Clientes)
BEGIN
    SET IDENTITY_INSERT dbo.Clientes ON;

    INSERT INTO dbo.Clientes (Id, Nome, Email) VALUES
        (1, N'Ana Silva',      N'ana.silva@email.com'),
        (2, N'Bruno Costa',    N'bruno.costa@email.com'),
        (3, N'Carla Mendes',   N'carla.mendes@email.com'),
        (4, N'Daniel Souza',   N'daniel.souza@email.com'),
        (5, N'Eduarda Lima',   N'eduarda.lima@email.com'),
        (6, N'Ana Silva',      N'ana.silva.sp@email.com'),
        (7, N'Felipe Rocha',   N'felipe.rocha@email.com'),
        (8, N'Gabriela Alves', N'gabriela.alves@email.com');

    SET IDENTITY_INSERT dbo.Clientes OFF;
END;

-- 2. Carga de Produtos (somente se a tabela estiver vazia)
-- O produto 6 ("Webcam Full HD") inicia com estoque baixo (2) para facilitar
-- testes manuais de validação de estoque insuficiente (HTTP 409).
IF NOT EXISTS (SELECT 1 FROM dbo.Produtos)
BEGIN
    SET IDENTITY_INSERT dbo.Produtos ON;

    INSERT INTO dbo.Produtos (Id, Nome, Preco, Estoque) VALUES
        (1, N'Notebook Pro 15',  4500.00, 10),
        (2, N'Monitor 27 4K',    1800.00, 15),
        (3, N'Teclado Mecânico',  450.00, 25),
        (4, N'Mouse Sem Fio',     150.00, 40),
        (5, N'Headset USB',       320.00, 12),
        (6, N'Webcam Full HD',    280.00,  2),
        (7, N'Mousepad Gamer',     80.00, 50);

    SET IDENTITY_INSERT dbo.Produtos OFF;
END;

-- 3. Carga de Pedidos e ItensPedido (somente se a tabela Pedidos estiver vazia)
-- Pedidos 1..8 ocorrem em 2026; Pedidos 9..10 ocorrem em 2025 para validar filtros de período.
IF NOT EXISTS (SELECT 1 FROM dbo.Pedidos)
BEGIN
    SET IDENTITY_INSERT dbo.Pedidos ON;

    INSERT INTO dbo.Pedidos (Id, ClienteId, DataPedido, ValorTotal) VALUES
        (1,  1, '2026-02-10T14:30:00', 4800.00),
        (2,  1, '2026-05-18T10:15:00', 1800.00),
        (3,  2, '2026-03-05T16:00:00', 4500.00),
        (4,  3, '2026-04-12T09:45:00', 4820.00),
        (5,  3, '2026-08-22T11:20:00',  240.00),
        (6,  4, '2026-06-14T15:10:00', 2440.00),
        (7,  5, '2026-07-03T13:05:00', 1200.00),
        (8,  6, '2026-09-19T17:40:00',  720.00),
        (9,  2, '2025-11-20T12:00:00', 4500.00),
        (10, 7, '2025-12-05T18:30:00', 1800.00);

    SET IDENTITY_INSERT dbo.Pedidos OFF;

    INSERT INTO dbo.ItensPedido (PedidoId, ProdutoId, Quantidade, PrecoUnitario) VALUES
        -- Pedido 1 (Cliente 1 = 4800.00)
        (1, 1, 1, 4500.00),
        (1, 4, 2,  150.00),
        -- Pedido 2 (Cliente 1 = 1800.00)
        (2, 2, 1, 1800.00),
        -- Pedido 3 (Cliente 2 = 4500.00)
        (3, 2, 2, 1800.00),
        (3, 3, 2,  450.00),
        -- Pedido 4 (Cliente 3 = 4820.00)
        (4, 1, 1, 4500.00),
        (4, 5, 1,  320.00),
        -- Pedido 5 (Cliente 3 = 240.00)
        (5, 7, 3,   80.00),
        -- Pedido 6 (Cliente 4 = 2440.00)
        (6, 2, 1, 1800.00),
        (6, 5, 2,  320.00),
        -- Pedido 7 (Cliente 5 = 1200.00)
        (7, 3, 2,  450.00),
        (7, 4, 2,  150.00),
        -- Pedido 8 (Cliente 6 = 720.00)
        (8, 6, 2,  280.00),
        (8, 7, 2,   80.00),
        -- Pedido 9 (Cliente 2 em 2025 = 4500.00)
        (9, 1, 1, 4500.00),
        -- Pedido 10 (Cliente 7 em 2025 = 1800.00)
        (10, 2, 1, 1800.00);
END;
