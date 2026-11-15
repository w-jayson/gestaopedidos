# Plan — Mini Gestão de Pedidos

Plano técnico de implementação elaborado a partir da [spec.md](spec.md) e das regras permanentes da [constitution.md](constitution.md).

---

## 1. Estrutura de Pastas do Repositório

```text
gestaopedidos/
├── spec/
│   ├── constitution.md
│   ├── spec.md
│   ├── plan.md
│   └── task.md
├── database/
│   ├── 01_schema.sql
│   └── 02_seed.sql
├── backend/
│   ├── Dockerfile
│   ├── OrderManagement.sln
│   ├── src/
│   │   ├── OrderManagement.Domain/
│   │   ├── OrderManagement.Application/
│   │   ├── OrderManagement.Infrastructure/
│   │   └── OrderManagement.API/
│   └── tests/
│       ├── OrderManagement.UnitTests/
│       └── OrderManagement.IntegrationTests/
├── frontend/
│   ├── Dockerfile
│   ├── nginx.conf
│   ├── package.json
│   ├── tsconfig.json
│   ├── vite.config.ts
│   ├── index.html
│   └── src/
│       ├── api/
│       ├── components/
│       ├── types/
│       ├── App.tsx
│       ├── App.css
│       └── main.tsx
├── docker-compose.yml
└── README.md
```

---

## 2. Banco de Dados (`database/`)

### 2.1 `database/01_schema.sql`
Script idempotente (`IF OBJECT_ID(...) IS NULL`) criando:
- **Tabelas:** `Clientes`, `Produtos`, `Pedidos`, `ItensPedido`.
- **Restrições:** chaves primárias (`IDENTITY`), chaves estrangeiras (`FK_Pedidos_Clientes`, `FK_ItensPedido_Pedidos`, `FK_ItensPedido_Produtos`) e `CHECK` constraints (`Preco >= 0`, `Estoque >= 0`, `ValorTotal >= 0`, `Quantidade > 0`, `PrecoUnitario >= 0`).
- **Índices planejados:**
  - `UX_Clientes_Email` (`UNIQUE` em `Clientes(Email)`)
  - `IX_Pedidos_DataPedido_ClienteId` em `Pedidos(DataPedido DESC, ClienteId)` — acelera filtros por período/cliente na listagem paginada e na query sargável do ranking.
  - `IX_ItensPedido_PedidoId` em `ItensPedido(PedidoId) INCLUDE (Quantidade, PrecoUnitario)` — permite calcular `SUM(Quantidade * PrecoUnitario)` sem precisar voltar à tabela base (*Key Lookup*).
  - `IX_ItensPedido_ProdutoId` em `ItensPedido(ProdutoId)` — cobre a FK de produto.

### 2.2 `database/02_seed.sql`
Script idempotente (`IF NOT EXISTS`) inserindo dados realistas para testes manuais, demonstração na UI e validação da Parte 4:
- Clientes suficientes (mais de 5, permitindo validar o corte `TOP 5` e também clientes sem compras no período).
- Produtos variados (incluindo item com estoque baixo para testar erro `409` facilmente pela interface).
- Pedidos distribuídos em **2026** (para validar a query de ranking da Parte 4) e em anos anteriores (para comprovar que o filtro por período funciona corretamente).

---

## 3. Arquitetura do Backend (`.NET 10`)

Organização em 4 camadas simples, sem abstrações desnecessárias, com identificadores em inglês e comentários didáticos em português:

### 3.1 `OrderManagement.Domain`
- **Entidades:** `Customer` (`Clientes`), `Product` (`Produtos`), `Order` (`Pedidos`), `OrderItem` (`ItensPedido`).
- **Exceções de Domínio/Negócio:**
  - `ValidationException` → mapeada para `400 Bad Request`.
  - `ResourceNotFoundException` → mapeada para `404 Not Found`.
  - `InsufficientStockException` → mapeada para `409 Conflict`.
- **Regras encapsuladas:** consolidação de itens com `ProductId` repetido, validação de quantidade `> 0`, cálculo de subtotal/total e método `Product.DecreaseStock(int quantity)` garantindo estoque não negativo.

### 3.2 `OrderManagement.Application`
- **DTOs (`Contracts/`):**
  - Entrada: `CreateOrderRequest`, `CreateOrderItemRequest`, `OrderFilterQuery`, `TopCustomersQuery`.
  - Saída: `OrderResponse`, `OrderItemResponse`, `PagedResponse<T>`, `TopCustomerResponse`, `CustomerResponse`, `ProductResponse`.
- **Interfaces enxutas:**
  - `IOrderCommandRepository` (escrita transacional do pedido e baixa de estoque).
  - `IOrderReadRepository` (leitura paginada de pedidos e relatório Top 5).
  - `ICatalogReadRepository` (listagem simples de clientes e produtos para os selects).
- **Serviço (`OrderService`):** orquestra a validação de entrada, consolidação de itens repetidos e delega a criação transacional; valida filtros de período (`startDate > endDate`) antes das consultas.

### 3.3 `OrderManagement.Infrastructure`
- **EF Core (`AppDbContext`):**
  - Mapeia explicitamente as entidades para as tabelas `Clientes`, `Produtos`, `Pedidos`, `ItensPedido` e suas colunas em português.
  - Usado no fluxo de escrita (`POST /pedidos`) dentro de uma **transação explícita (`BeginTransactionAsync`)**:
    1. Verifica existência do cliente.
    2. Carrega os produtos solicitados e valida existência + estoque disponível.
    3. Executa baixa atômica no banco (`ExecuteSql` / `UPDATE Produtos SET Estoque = Estoque - @qty WHERE Id = @id AND Estoque >= @qty` ou atualização transacional verificada) para evitar condição de corrida e garantir rollback completo em caso de falha.
    4. Salva `Order` + `OrderItems` com `PrecoUnitario` copiado do banco e `ValorTotal` calculado no servidor.
- **Dapper (`SqlConnectionFactory` + repositórios de leitura):**
  - **`GET /pedidos`:** query SQL parametrizada e sargável (`DataPedido >= @StartDate AND DataPedido < @EndDateExclusive`), contagem total (`COUNT(*)`) + paginação (`OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY`), seguida de uma única consulta (`WHERE PedidoId IN @OrderIds`) para montar os itens sem problema N+1.
  - **`GET /relatorios/top-clientes`:** query sargável corrigida da Parte 4 (`SELECT TOP 5 c.Id, c.Nome, COUNT(DISTINCT p.Id), SUM(i.Quantidade * i.PrecoUnitario) ... INNER JOIN ... GROUP BY c.Id, c.Nome ORDER BY TotalAmount DESC`).
  - **`GET /clientes` e `GET /produtos`:** queries diretas e ordenadas por nome.

### 3.4 `OrderManagement.API`
- **Controllers tradicionais:**
  - `OrdersController` (`[Route("pedidos")]`): `POST`, `GET`.
  - `ReportsController` (`[Route("relatorios")]`): `GET top-clientes`.
  - `CustomersController` (`[Route("clientes")]`): `GET`.
  - `ProductsController` (`[Route("produtos")]`): `GET`.
- **OpenAPI + Scalar:** `AddOpenApi()` + `MapScalarApiReference()` (sem Swagger UI).
- **Erros padronizados:** `GlobalExceptionHandler` implementando `IExceptionHandler` com `AddProblemDetails()`, convertendo exceções de negócio e erros de binding em `ProblemDetails (RFC 7807)` (`400`, `404`, `409`).
- **Healthcheck:** endpoint `/health` para orquestração no Docker Compose.

---

## 4. Arquitetura do Frontend (`React + TypeScript`)

Aplicação criada com **Vite (`react-ts`)**, sem bibliotecas pesadas de estado, formulários ou componentes:

- **`src/types/api.ts`:** interfaces TypeScript espelhando os JSONs da API (`Order`, `PagedOrders`, `Customer`, `Product`, `TopCustomer`, `ProblemDetailsError`).
- **`src/api/client.ts`:** funções assíncronas com `fetch` nativo apontando para `/api/...`, tratando respostas `!response.ok` para extrair `detail`/`title` do `ProblemDetails`.
- **`src/components/TopCustomersCard.tsx`:** exibe o Top 5 clientes com filtro simples de `startDate` e `endDate`.
- **`src/components/OrdersFilterBar.tsx`:** selects/inputs simples para filtrar por cliente e período + botão limpar.
- **`src/components/OrdersList.tsx`:** tabela de pedidos, resumo de itens e botões *Anterior* / *Próxima*.
- **`src/components/CreateOrderModal.tsx`:** modal com seleção de cliente, adição/remoção dinâmica de produtos + quantidade, prévia do total e exibição clara de erro/sucesso.
- **`src/App.tsx`:** estado central da página que coordena o carregamento inicial e recarrega pedidos, produtos (estoque atualizado) e ranking após criar um pedido.
- **Comunicação com a API:**
  - Em desenvolvimento local: proxy `/api` no `vite.config.ts` redirecionando para a API `.NET`.
  - Em produção/Docker: **Nginx** servindo os arquivos estáticos e fazendo proxy reverso de `/api/` para `http://backend:8080/` (removendo o prefixo `/api`), evitando problemas de CORS e portas no navegador.

---

## 5. Plano de Testes (`backend/tests/`)

### 5.1 `OrderManagement.UnitTests` (`xUnit`)
Testes rápidos sem banco externo, cobrindo:
- Criação de pedido válida (preço unitário copiado do produto, `ValorTotal` calculado, estoque decrementado).
- Consolidação de produto repetido no mesmo pedido (soma quantidades antes de validar estoque).
- Falhas esperadas: cliente inexistente, produto inexistente, lista de itens vazia/nula, quantidade `<= 0`, estoque insuficiente (inclusive quando soma de produto repetido ultrapassa estoque) e período inválido (`startDate > endDate`).

### 5.2 `OrderManagement.IntegrationTests` (`xUnit + Testcontainers`)
- Fixture compartilhada (`SqlServerTestFixture`) iniciando um container real do **SQL Server** via `Testcontainers.MsSql`.
- Execução automática dos scripts reais `database/01_schema.sql` e `database/02_seed.sql` na inicialização da fixture.
- `WebApplicationFactory<Program>` apontando a connection string para o container de teste.
- Testes HTTP end-to-end na API validando:
  - `POST /pedidos` (`201` com baixa real de estoque no SQL Server; `409` sem alterar estoque em rollback; `404` e `400` retornando `ProblemDetails`).
  - `GET /pedidos` (filtros combinados por cliente e datas + paginação e ordenação decrescente).
  - `GET /relatorios/top-clientes` (retorna até 5 clientes, ignora quem não comprou no período, ordena `DESC`).
  - `GET /clientes` e `GET /produtos`.

---

## 6. Orquestração Docker (`docker-compose.yml`)

Serviços definidos na raiz:
1. `sqlserver`: imagem oficial do SQL Server 2022 com volume persistente e `healthcheck` via `sqlcmd`.
2. `db-init`: container temporário (`mssql-tools`) que aguarda `sqlserver` saudável e executa `01_schema.sql` e `02_seed.sql`.
3. `backend`: build `.NET 10` (`ASP.NET Core`), expõe porta `8080` e só inicia após `db-init` concluir com sucesso (`service_completed_successfully`).
4. `frontend`: build Vite + Nginx servindo na porta `3000` e encaminhando `/api/*` para `backend:8080`.

Comando único de execução: `docker compose up --build` (acessando o sistema em `http://localhost:3000` e Scalar em `http://localhost:8080/scalar/v1`).

---

## 7. Debug da Query (Parte 4)

Durante a implementação do banco e testes contra o SQL Server real:
1. Executar a query original do desafio com `SET STATISTICS IO, TIME ON` e inspecionar o plano de execução para observar o *Scan* causado por `YEAR(p.DataPedido) = 2026`, o comportamento do `LEFT JOIN` anulado pelo `WHERE` e a ordenação ascendente.
2. Aplicar os índices (`IX_Pedidos_DataPedido_ClienteId` e `IX_ItensPedido_PedidoId INCLUDE (Quantidade, PrecoUnitario)`) e reescrever a query com intervalo sargável (`>= '2026-01-01' AND < '2027-01-01'`), `INNER JOIN`, `GROUP BY c.Id, c.Nome`, `ORDER BY Total DESC` e `TOP 5`.
3. Registrar o comparativo e a explicação técnica no `README.md`.

---

## 8. Registro de Ajustes e Decisões em Relação às Propostas Iniciais

Em conformidade com o requisito de registrar ajustes e discordâncias críticas no planejamento assistido por IA:

1. **Troca do Swagger UI pelo Scalar:** Em vez de adotar o pacote tradicional de Swagger UI, optou-se por OpenAPI nativo do `.NET 10` com **Scalar**, reduzindo dependências e mantendo a documentação da API moderna e enxuta.
2. **Inclusão de `GET /clientes` e `GET /produtos` além dos 3 endpoints do enunciado:** Uma abordagem estrita limitaria a API a `POST /pedidos`, `GET /pedidos` e `GET /relatorios/top-clientes`, o que obrigaria o frontend React a usar listas fixas ou digitação manual de IDs. A inclusão desses dois endpoints de leitura torna o fluxo ponta a ponta realista.
3. **Adição de `Email` em `Clientes` e `ValorTotal` em `Pedidos`:** O enunciado não detalhou todas as colunas além das usadas na query da Parte 4. Incluir `Email` evita ambiguidade entre clientes homônimos e persistir `ValorTotal` simplifica e acelera a paginação de `GET /pedidos` sem recalcular somatórios a cada página, mantendo o cálculo por `ItensPedido` na query da Parte 4.
4. **Híbrido EF Core + Dapper em vez de ORM único:** O uso exclusivo de EF Core dificultaria o controle fino do SQL da Parte 4 e de consultas paginadas/agregadas; já o uso exclusivo de Dapper aumentaria código repetitivo no `POST /pedidos`. A divisão (EF Core para transação/escrita e Dapper para leitura/relatório) equilibra simplicidade e controle.
5. **Proxy `/api` via Nginx no Docker:** Em vez de configurar URLs fixas com `localhost:8080` e CORS aberto no navegador, o Nginx faz proxy interno `/api -> backend:8080`, permitindo que `docker compose up` funcione de forma transparente sem ajustes manuais.
6. **Frontend minimalista e comentários didáticos em português:** Optou-se por evitar bibliotecas como Redux, React Query ou UI kits pesados, priorizando `fetch`, `useState`/`useEffect` e comentários detalhados em português para garantir domínio total na revisão ao vivo.
