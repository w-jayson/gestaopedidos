# Tasks — Mini Gestão de Pedidos

Checklist sequencial e verificável para implementar a aplicação seguindo a [constitution.md](constitution.md), a [spec.md](spec.md) e o [plan.md](plan.md).

> **Convenção de execução:** Ao concluir cada fase, validar build/testes aplicáveis e registrar commits pequenos e claros no padrão **Conventional Commits** (sem `squash` ou reescrita de histórico).

---

## Fase 0 — Preparação do Repositório

- [x] **0.1** Configurar `.gitignore` na raiz para `.NET` (`bin/`, `obj/`), Node/React (`node_modules/`, `dist/`) e arquivos de IDE/SO.
- [x] **0.2** Revisar `spec/constitution.md`, `spec/spec.md`, `spec/plan.md` e `spec/task.md` e realizar commit inicial da fase SDD (`docs: add spec-driven development artifacts`).

---

## Fase 1 — Banco de Dados SQL Server (`database/`)

- [x] **1.1** Criar `database/01_schema.sql` de forma idempotente com as tabelas `Clientes`, `Produtos`, `Pedidos` e `ItensPedido`, PKs, FKs e `CHECK` constraints.
- [x] **1.2** Adicionar no `01_schema.sql` os índices planejados (`UX_Clientes_Email`, `IX_Pedidos_DataPedido_ClienteId`, `IX_ItensPedido_PedidoId INCLUDE (Quantidade, PrecoUnitario)`, `IX_ItensPedido_ProdutoId`).
- [x] **1.3** Criar `database/02_seed.sql` de forma idempotente com clientes (mais de 5, incluindo cliente sem pedidos), produtos (incluindo item com estoque baixo para testes) e pedidos distribuídos em **2026** e em anos anteriores.
- [x] **1.4** Commit da fase (`feat(database): add sql server schema, indexes and seed scripts`).

---

## Fase 2 — Solution, Domain e Application (`backend/`)

- [x] **2.1** Criar `backend/OrderManagement.sln` e os 6 projetos `.NET 10` (`Domain`, `Application`, `Infrastructure`, `API`, `UnitTests` e `IntegrationTests` com xUnit).
- [x] **2.2** Criar as entidades de domínio em `OrderManagement.Domain` (`Customer`, `Product`, `Order`, `OrderItem`) com identificadores em inglês e comentários de tradução/explicação em português.
- [x] **2.3** Criar as exceções de negócio em `OrderManagement.Domain` (`ValidationException`, `ResourceNotFoundException`, `InsufficientStockException`).
- [x] **2.4** Criar os DTOs de entrada/saída em `OrderManagement.Application` (`CreateOrderRequest`, `OrderResponse`, `PagedResponse<T>`, `TopCustomerResponse`, `CustomerResponse`, `ProductResponse`, filtros).
- [x] **2.5** Criar as interfaces de repositório (`IOrderCommandRepository`, `IOrderReadRepository`, `ICatalogReadRepository`) em `OrderManagement.Application`.
- [x] **2.6** Implementar `OrderService` em `OrderManagement.Application` (validação de pedido sem itens, quantidade `<= 0`, consolidação de produto repetido e validação de período `startDate > endDate`).
- [x] **2.7** Executar `dotnet build` e realizar commit (`feat(backend): create solution structure, domain entities and application layer`).

---

## Fase 3 — Camada Infrastructure (`EF Core` + `Dapper`)

- [x] **3.1** Configurar `AppDbContext` (EF Core) mapeando explicitamente entidades e propriedades em inglês para as tabelas e colunas em português (`Clientes`, `Produtos`, `Pedidos`, `ItensPedido`).
- [x] **3.2** Implementar `OrderCommandRepository` com transação explícita: validar cliente, buscar produtos, consolidar/checar estoque, decrementar estoque atomicamente (`Estoque >= Quantidade`), gravar `PrecoUnitario` do catálogo e `ValorTotal` calculado no servidor.
- [x] **3.3** Implementar `SqlConnectionFactory` para criação de conexões SQL Server usadas pelo Dapper.
- [x] **3.4** Implementar `OrderReadRepository` (Dapper):
  - `GET /pedidos`: filtros sargáveis (`DataPedido >= @StartDate AND DataPedido < @EndDateExclusive`), `OFFSET/FETCH` + `COUNT(*)` e carga de itens sem N+1.
  - `GET /relatorios/top-clientes`: query corrigida (`TOP 5`, `INNER JOIN`, filtro sargável, `GROUP BY c.Id, c.Nome`, `ORDER BY TotalAmount DESC`).
- [x] **3.5** Implementar `CatalogReadRepository` (Dapper) para listar clientes (`GET /clientes`) e produtos (`GET /produtos`) ordenados por nome.
- [x] **3.6** Executar `dotnet build` e realizar commit (`feat(infrastructure): implement ef core transactional writes and dapper queries`).

---

## Fase 4 — Camada API (`.NET 10`)

- [x] **4.1** Implementar `GlobalExceptionHandler` (`IExceptionHandler`) convertendo exceções de negócio (`400`, `404`, `409`) para `ProblemDetails (RFC 7807)`.
- [x] **4.2** Configurar `Program.cs` com injeção de dependência, `AddProblemDetails()`, OpenAPI + **Scalar**, endpoint `/health` e `public partial class Program`.
- [x] **4.3** Implementar `OrdersController` (`POST /pedidos`, `GET /pedidos`).
- [x] **4.4** Implementar `ReportsController` (`GET /relatorios/top-clientes`), `CustomersController` (`GET /clientes`) e `ProductsController` (`GET /produtos`).
- [x] **4.5** Validar `dotnet build` e verificar o contrato OpenAPI/Scalar.
- [x] **4.6** Realizar commit (`feat(api): add controllers, problem details handler and scalar docs`).

---

## Fase 5 — Testes Automatizados (`UnitTests` e `IntegrationTests`)

- [x] **5.1** Criar testes unitários de criação válida e consolidação de produto duplicado em `OrderManagement.UnitTests`.
- [x] **5.2** Criar testes unitários de validação/erro (cliente inexistente, produto inexistente, lista vazia/nula, quantidade `<= 0`, estoque insuficiente e período inválido).
- [x] **5.3** Configurar `SqlServerTestFixture` em `OrderManagement.IntegrationTests` com `Testcontainers.MsSql` executando `01_schema.sql` e `02_seed.sql`, integrado ao `WebApplicationFactory<Program>`.
- [x] **5.4** Criar testes de integração para `POST /pedidos` (`201`, `400`, `404`, `409` garantindo rollback de estoque).
- [x] **5.5** Criar testes de integração para `GET /pedidos`, `GET /relatorios/top-clientes`, `GET /clientes` e `GET /produtos`.
- [x] **5.6** Executar `dotnet test` e confirmar aprovação de 100% dos testes unitários e de integração.
- [x] **5.7** Realizar commit (`test(backend): add unit and testcontainers integration tests`).

---

## Fase 6 — Frontend Minimalista (`frontend/`)

- [ ] **6.1** Inicializar projeto `Vite + React + TypeScript` em `frontend/` sem bibliotecas pesadas extras e configurar proxy `/api` em `vite.config.ts`.
- [ ] **6.2** Criar `src/types/api.ts` com as interfaces TypeScript alinhadas aos contratos da API e comentários em português.
- [ ] **6.3** Criar `src/api/client.ts` com funções `fetch` nativas e tratamento de `ProblemDetails` explicado linha a linha.
- [ ] **6.4** Criar `src/components/TopCustomersCard.tsx` (card de Top 5 clientes com filtro de datas e comentários didáticos).
- [ ] **6.5** Criar `src/components/OrdersFilterBar.tsx` (filtros por cliente e período + botão limpar, com comentários didáticos).
- [ ] **6.6** Criar `src/components/OrdersList.tsx` (tabela de pedidos com resumo dos itens e paginação Anterior/Próxima, com comentários didáticos).
- [ ] **6.7** Criar `src/components/CreateOrderModal.tsx` (modal de criação de pedido com seleção de cliente, itens dinâmicos, prévia do total e exibição de erros, com comentários didáticos).
- [ ] **6.8** Montar `src/App.tsx` e `src/App.css` integrando todos os componentes e recarregamento automático após criar pedido.
- [ ] **6.9** Executar `npm run build` em `frontend/` para validar tipagem TypeScript e bundle de produção.
- [ ] **6.10** Realizar commit (`feat(frontend): add minimal react typescript interface with didactic comments`).

---

## Fase 7 — Containerização Docker (`docker-compose.yml`)

- [ ] **7.1** Criar `backend/Dockerfile` (build multi-stage `.NET 10`).
- [ ] **7.2** Criar `frontend/Dockerfile` e `frontend/nginx.conf` (build Vite + Nginx com proxy `/api` → `http://backend:8080/`).
- [ ] **7.3** Criar `docker-compose.yml` na raiz (`sqlserver`, `db-init`, `backend` e `frontend`).
- [ ] **7.4** Testar subida completa com `docker compose up --build` e validar criação de pedido, listagem filtrada e Top 5 clientes pelo navegador.
- [ ] **7.5** Realizar commit (`chore(docker): configure sqlserver, backend and frontend containers`).

---

## Fase 8 — Debug da Query (Parte 4) e `README.md`

- [ ] **8.1** Validar no SQL Server a query original vs query corrigida da Parte 4 (coletando `STATISTICS IO, TIME ON` e detalhes do plano de execução / índices utilizados).
- [ ] **8.2** Escrever `README.md` na raiz contendo obrigatoriamente:
  1. Como rodar o projeto com Docker (`docker compose up`).
  2. Como rodar os testes unitários e de integração (`dotnet test`).
  3. Principais decisões técnicas.
  4. Como a IA foi usada (o que funcionou bem e o que precisou ser ajustado).
  5. Explicação completa da **Parte 4 (debug e otimização da query)**.
- [ ] **8.3** Verificação final de todos os critérios de aceite da `spec.md` (`dotnet test` + `docker compose up --build`).
- [ ] **8.4** Realizar commit final (`docs: add comprehensive readme and query optimization analysis`).
