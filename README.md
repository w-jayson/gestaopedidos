# Mini Gestão de Pedidos

Aplicação completa para cadastro transacional e consulta de **Pedidos de Clientes**, desenvolvida com foco em **Spec-Driven Development (SDD)**, engenharia assistida por IA, modelagem e otimização de consultas no **SQL Server**, arquitetura em camadas no **.NET 10**, interface minimalista em **React + TypeScript** e testes automatizados (unitários e de integração com **Testcontainers**).

---

## Sumário

1. [Visão Geral e Arquitetura](#1-visão-geral-e-arquitetura)
2. [Como Rodar o Projeto com Docker](#2-como-rodar-o-projeto-com-docker)
3. [Como Rodar os Testes (Unitários e de Integração)](#3-como-rodar-os-testes-unitários-e-de-integração)
4. [Principais Decisões Técnicas](#4-principais-decisões-técnicas)
5. [Uso Estruturado da IA: O que Funcionou Bem e o que Foi Corrigido](#5-uso-estruturado-da-ia-o-que-funcionou-bem-e-o-que-foi-corrigido)
6. [Parte 4 — Debug e Otimização da Query SQL](#6-parte-4--debug-e-otimização-da-query-sql)
7. [Endpoints da API](#7-endpoints-da-api)

---

## 1. Visão Geral e Arquitetura

### Stack Tecnológica

| Camada | Tecnologia |
|---|---|
| **Banco de Dados** | SQL Server 2022 (`Clientes`, `Produtos`, `Pedidos`, `ItensPedido`) |
| **Backend** | .NET 10 (`ASP.NET Core Web API`), C# |
| **Acesso a Dados** | Híbrido: **Entity Framework Core 10** (escrita transacional) + **Dapper** (consultas otimizadas e relatórios) |
| **Documentação e Erros da API** | OpenAPI nativo + **Scalar UI** (`/scalar/v1`) e **ProblemDetails (RFC 7807)** |
| **Frontend** | React 19 + TypeScript + Vite (arquitetura minimalista com `fetch` nativo) |
| **Testes Automatizados** | **xUnit**, **Testcontainers (`Testcontainers.MsSql`)** e `WebApplicationFactory` |
| **Containerização** | Docker & Docker Compose (orquestração *turnkey* com inicialização automática do banco) |

### Estrutura do Repositório

```text
gestaopedidos/
├── spec/                                   # Artefatos de Spec-Driven Development (SDD)
│   ├── constitution.md                     # Princípios, stack, convenções e regras inegociáveis
│   ├── spec.md                             # Especificação funcional, regras de negócio e critérios de aceite
│   ├── plan.md                             # Plano técnico de implementação e registro de decisões críticas
│   └── task.md                             # Checklist incremental de tarefas e commits por fase
├── database/
│   ├── 01_schema.sql                       # Criação idempotente de tabelas, PKs, FKs, CHECKs e índices
│   └── 02_seed.sql                         # Carga inicial idempotente (clientes, produtos, pedidos 2025/2026)
├── backend/
│   ├── OrderManagement.sln
│   ├── src/
│   │   ├── OrderManagement.Domain/         # Entidades (Customer, Product, Order, OrderItem) e exceções de negócio
│   │   ├── OrderManagement.Application/    # DTOs, interfaces de repositório e OrderService (regras de negócio)
│   │   ├── OrderManagement.Infrastructure/ # EF Core (AppDbContext, escrita transacional) e Dapper (leituras SQL)
│   │   └── OrderManagement.API/            # Controllers REST, GlobalExceptionHandler (ProblemDetails) e Scalar
│   └── tests/
│       ├── OrderManagement.UnitTests/        # Testes unitários em memória das regras de negócio e validações
│       └── OrderManagement.IntegrationTests/ # Testes HTTP ponta a ponta com SQL Server real via Testcontainers
├── frontend/
│   └── src/
│       ├── api/client.ts                   # Cliente HTTP com fetch nativo e leitura de ProblemDetails
│       ├── components/                     # TopCustomersCard, OrdersFilterBar, OrdersList e CreateOrderModal
│       ├── types/api.ts                    # Contratos TypeScript espelhando os DTOs da API
│       └── App.tsx                         # Coordenação de estado e atualização reativa de pedidos/estoque/ranking
├── docker-compose.yml                      # Orquestração completa (sqlserver, db-init, backend e frontend)
└── README.md
```

---

## 2. Como Rodar o Projeto com Docker

### Pré-requisitos
- [Docker](https://www.docker.com/) e **Docker Compose** instalados e em execução.

### Subindo toda a aplicação com um único comando

Na raiz do repositório, execute:

```bash
docker compose up --build
```

O `docker-compose.yml` orquestra automaticamente a ordem de inicialização sem necessidade de passos manuais:
1. **`sqlserver`**: sobe o SQL Server 2022 e aguarda o `healthcheck` responder.
2. **`db-init`**: conecta ao SQL Server assim que saudável, cria o banco `OrderManagementDb` e executa automaticamente os scripts idempotentes `database/01_schema.sql` e `database/02_seed.sql`.
3. **`backend`**: inicia a API `.NET 10` somente após o `db-init` concluir com sucesso (`service_completed_successfully`).
4. **`frontend`**: serve a aplicação React compilada via Nginx e realiza proxy reverso de `/api/*` para `http://backend:8080/`.

### Endereços de Acesso (Docker)

| Serviço | URL | Descrição |
|---|---|---|
| **Frontend (Web UI)** | `http://localhost:3000` | Interface de listagem filtrada, criação de pedidos e Top 5 clientes |
| **Backend (API REST)** | `http://localhost:8080` | Endpoints `/pedidos`, `/relatorios/top-clientes`, `/clientes`, `/produtos` |
| **Documentação Scalar** | `http://localhost:8080/scalar/v1` | Documentação interativa OpenAPI (substitui o Swagger UI) |
| **Healthcheck** | `http://localhost:8080/health` | Verificação de saúde da API |

### Executando localmente sem Docker Compose (Desenvolvimento)

Caso deseje rodar o backend e o frontend diretamente na máquina local (apontando para uma instância SQL Server configurada em `appsettings.Development.json`):

```bash
# 1. Backend (.NET 10 SDK) - sobe em http://localhost:5084
cd backend
dotnet run --project src/OrderManagement.API

# 2. Frontend (Node.js 20+) - sobe em http://localhost:5173 com proxy /api -> localhost:5084
cd frontend
npm install
npm run dev
```

---

## 3. Como Rodar os Testes (Unitários e de Integração)

A solução possui duas suítes de testes automatizados escritas com **xUnit**:

1. **`OrderManagement.UnitTests` (12 execuções de teste):**
   - Exercita isoladamente em memória todas as regras de negócio do `OrderService` e das entidades de domínio, sem dependência de banco de dados.
   - Cobre: criação válida de pedido com preço congelado do catálogo e baixa de estoque, consolidação de produtos repetidos no mesmo pedido (`RN-04`), cliente inexistente (`404`), produto inexistente (`404`), pedido sem itens ou lista nula/vazia (`400`), quantidade zero ou negativa (`400`), `customerId` inválido (`400`), estoque insuficiente simples e após soma de item repetido (`409`), além de filtros de data invertidos (`startDate > endDate`) e parâmetros de paginação inválidos.

2. **`OrderManagement.IntegrationTests` (7 testes ponta a ponta):**
   - Utiliza **`Testcontainers.MsSql`** + **`WebApplicationFactory<Program>`** para subir automaticamente um container **SQL Server 2022 real**, criar o banco `OrderManagementDb` e executar os scripts oficiais `database/01_schema.sql` e `database/02_seed.sql`.
   - Antes de cada teste (`ResetSeedDataAsync`), o estado do banco é restaurado para garantir isolamento total.
   - Exercita os endpoints reais via `HttpClient`:
     - `POST /pedidos` com sucesso (`201 Created`) verificando a baixa real de estoque no SQL Server.
     - `POST /pedidos` com estoque insuficiente em item consolidado (`409 Conflict` em formato `ProblemDetails`), comprovando o **rollback transacional completo** (nenhum produto do pedido tem o estoque alterado).
     - `POST /pedidos` para cliente inexistente (`404 Not Found`) e lista de itens vazia (`400 Bad Request`).
     - `GET /pedidos` validando filtros combinados por `customerId`, `startDate`, `endDate`, ordenação decrescente e paginação (`page`, `pageSize`, `totalCount`, `totalPages`).
     - `GET /relatorios/top-clientes` validando a query corrigida da Parte 4 no ano de 2026 (retorno dos 5 maiores compradores em ordem decrescente, sem fundir os clientes homônimos `"Ana Silva"` e sem incluir pedidos de 2025).

### Comandos para Execução

> **Importante:** Para rodar os testes de integração (`OrderManagement.IntegrationTests`), o **Docker Desktop / Docker Engine deve estar em execução**, pois o Testcontainers criará e destruirá automaticamente o container do SQL Server durante o comando `dotnet test`.

```bash
# Executar TODOS os testes (unitários + integração com Testcontainers)
dotnet test backend/OrderManagement.sln

# Executar APENAS os testes unitários (rápidos, não requerem Docker)
dotnet test backend/tests/OrderManagement.UnitTests/OrderManagement.UnitTests.csproj

# Executar APENAS os testes de integração (requer Docker ativo)
dotnet test backend/tests/OrderManagement.IntegrationTests/OrderManagement.IntegrationTests.csproj
```

---

## 4. Principais Decisões Técnicas

1. **Acesso a Dados Híbrido (EF Core para Escrita + Dapper para Leitura):**
   - **Escrita (`POST /pedidos`) com EF Core:** A criação do pedido envolve múltiplas operações que precisam ser atômicas. No `OrderCommandRepository`, utilizamos `BeginTransactionAsync` aliado a uma instrução SQL condicional parametrizada (`UPDATE dbo.Produtos SET Estoque = Estoque - @qty WHERE Id = @id AND Estoque >= @qty`), executada em ordem crescente de `ProductId` (evitando *deadlocks*). Se `affectedRows == 0`, uma `InsufficientStockException` é lançada e toda a transação sofre *rollback*, prevenindo condições de corrida (*race conditions*) em compras concorrentes.
   - **Leitura (`GET /pedidos`, `GET /relatorios/top-clientes`, `/clientes`, `/produtos`) com Dapper:** Permite escrever SQL explícito, enxuto e 100% sargável, aproveitando os índices desenhados no banco sem o *overhead* de tradução de árvores de expressão do ORM em consultas agregadas.

2. **Eliminação do Problema N+1 na Listagem Paginada (`GET /pedidos`):**
   - Fazer `JOIN` entre `Pedidos` e `ItensPedido` na mesma query paginada com `OFFSET/FETCH` causaria paginação incorreta (cortando linhas de itens em vez de pedidos). Por outro lado, buscar os itens em um loop para cada pedido da página geraria *N+1 queries*.
   - **Solução adotada no `OrderReadRepository`:**
     1. Executa `COUNT(*)` filtrado em `Pedidos`.
     2. Busca a página de cabeçalhos em `Pedidos INNER JOIN Clientes` com `OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY`.
     3. Carrega todos os itens de todos os pedidos daquela página em **uma única consulta** (`WHERE i.PedidoId IN @OrderIds`) e agrupa em memória por `PedidoId` via dicionário (`O(1)`).

3. **Consolidação de Produtos Repetidos e Cálculo de Preço no Servidor:**
   - O cliente envia apenas `customerId` e `{ productId, quantity }`. O servidor ignora qualquer preço externo, copia `Produtos.Preco` para `ItensPedido.PrecoUnitario` (congelando o preço histórico da venda) e calcula `ValorTotal`.
   - Se o mesmo `productId` aparecer mais de uma vez no mesmo pedido, o `OrderService` consolida as quantidades antes de validar o estoque (`RN-04`), impedindo que múltiplas linhas parciais burlem o limite disponível em `Produtos.Estoque`.

4. **Padronização de Erros com ProblemDetails (RFC 7807):**
   - Implementado o `GlobalExceptionHandler` (`IExceptionHandler`) mapeando exceções de domínio para respostas HTTP padronizadas:
     - `ValidationException` → `400 Bad Request`
     - `ResourceNotFoundException` → `404 Not Found`
     - `InsufficientStockException` → `409 Conflict`
   - O cliente HTTP do frontend (`frontend/src/api/client.ts`) lê diretamente os campos `detail` e `title` do `ProblemDetails` para exibir mensagens claras ao usuário.

5. **OpenAPI Nativo + Scalar UI:**
   - Adotado `AddOpenApi()` nativo do `.NET 10` em conjunto com `Scalar.AspNetCore` (`/scalar/v1`), substituindo o Swagger UI tradicional por uma interface mais leve e moderna.

6. **Convenção de Nomenclatura Bilingue (Contrato em Português, Código em Inglês com Comentários Didáticos):**
   - Para respeitar estritamente o enunciado do teste, os nomes físicos no banco (`Clientes`, `Produtos`, `Pedidos`, `ItensPedido`) e as rotas HTTP (`/pedidos`, `/relatorios/top-clientes`) foram mantidos em português.
   - No código C# e TypeScript, todas as classes, métodos, propriedades e variáveis seguem o padrão em inglês (`Customer`, `Product`, `Order`, `OrderItem`, `OrderService`), com mapeamento explícito no `AppDbContext` e comentários didáticos em português explicando a responsabilidade de cada bloco.

7. **Frontend Minimalista e Proxy `/api`:**
   - Construído em React + TypeScript utilizando apenas recursos nativos (`fetch`, `useState`, `useEffect`) sem bibliotecas pesadas de gerenciamento de estado ou UI.
   - As chamadas partem de `/api/...`, redirecionadas pelo proxy do Vite em desenvolvimento e pelo Nginx no Docker Compose, eliminando problemas de CORS entre containers.

---

## 5. Uso Estruturado da IA: O que Funcionou Bem e o que Foi Corrigido

O desenvolvimento seguiu a metodologia **Spec-Driven Development (SDD)** estruturada na pasta [`spec/`](spec/):

1. **[`spec/constitution.md`](spec/constitution.md) (Rules e Convenções):** Definiu as diretrizes permanentes do projeto antes de qualquer código ser gerado — arquitetura em 4 camadas, acesso híbrido EF Core + Dapper, obrigatoriedade de queries sargáveis, padrão de commits (`Conventional Commits`), estratégia de testes com xUnit + Testcontainers e comandos de verificação (`dotnet build`, `dotnet test`).
2. **[`spec/spec.md`](spec/spec.md) (Especificação Funcional):** Formalizou o modelo de dados, as 10 regras de negócio (`RN-01` a `RN-10`), os payloads JSON de entrada/saída, os códigos de erro HTTP e os 9 critérios de aceite.
3. **[`spec/plan.md`](spec/plan.md) e [`spec/task.md`](spec/task.md) (Planejamento e Execução Incremental):** Decompuseram a entrega em fases verificáveis (Banco de Dados → Domain/Application → Infrastructure → API → Testes → Frontend → Docker → Debug da Query/Documentação), preservando um histórico de commits limpo e granular.

### O que funcionou muito bem com a IA

- **Alinhamento de Contratos Ponta a Ponta:** A existência prévia da `spec/spec.md` permitiu gerar os DTOs da camada `Application`, os controllers da `API` e as interfaces TypeScript do `frontend/src/types/api.ts` com 100% de simetria nos nomes de campos e tipos de dados.
- **Infraestrutura de Testes de Integração com Testcontainers:** A configuração da fixture `SqlServerTestFixture` subindo o container `mcr.microsoft.com/mssql/server:2022-latest`, localizando a raiz do repositório e aplicando os próprios scripts `database/01_schema.sql` e `database/02_seed.sql` funcionou de primeira e garantiu que o SQL do Dapper e os mapeamentos do EF Core fossem validados contra o motor real do SQL Server.
- **Elaboração da Massa de Dados (`database/02_seed.sql`):** A geração da massa inicial foi direcionada para cobrir todos os cenários de borda exigidos no teste: clientes homônimos (`"Ana Silva"` com `Id = 1` e `Id = 6`), produto com estoque reduzido (`Webcam Full HD` com `Estoque = 2` para testar facilmente o erro `409`), pedidos em `2025` e `2026`, e cliente sem compras (`Gabriela Alves`).

### O que precisou ser ajustado ou corrigido no código e nas propostas iniciais

Durante o planejamento (`spec/plan.md`) e a revisão do código gerado, fizemos intervenções técnicas importantes onde a abordagem padrão da IA seria insuficiente ou frágil:

1. **Prevenção de Condição de Corrida (*Race Condition*) na Baixa de Estoque:**
   - *Problema na abordagem padrão:* A tendência inicial de código gerado com EF Core é carregar o `Product` em memória, fazer `product.Stock -= quantity` e chamar `SaveChangesAsync()`. Em requisições simultâneas para o mesmo produto, duas transações podem ler o mesmo saldo antes da gravação e permitir estoque negativo ou venda duplicada.
   - *Correção aplicada:* No `OrderCommandRepository`, além da validação em domínio, implementamos a baixa atômica diretamente no banco dentro da transação com `ExecuteSqlInterpolatedAsync`:
     ```sql
     UPDATE dbo.Produtos
     SET Estoque = Estoque - @quantityToDecrease
     WHERE Id = @productId AND Estoque >= @quantityToDecrease
     ```
     Verificamos se `affectedRows == 0` para disparar `InsufficientStockException` (`409`) e ordenamos as baixas por `ProductId` para evitar *deadlocks* entre transações concorrentes.

2. **Consolidação de Produto Repetido no Mesmo Pedido (`RN-04`):**
   - *Problema:* Se um payload enviasse duas linhas para o mesmo `productId` (ex.: `2` unidades + `1` unidade de um item cujo estoque é `2`), validar cada linha isoladamente contra o estoque original deixaria a validação em memória passar ou criaria linhas duplicadas na tabela `ItensPedido`.
   - *Correção aplicada:* Adicionamos o método `ConsolidateItems` no `OrderService` para somar quantidades por `ProductId` antes de consultar o banco e validar o estoque, criando testes unitários e de integração específicos para esse cenário.

3. **Adição de `Email` em `Clientes`, `ValorTotal` em `Pedidos` e Endpoints de Catálogo (`/clientes` e `/produtos`):**
   - *Problema:* O enunciado mencionava apenas as 3 rotas principais e as colunas exibidas na query da Parte 4. Sem endpoints de listagem de clientes e produtos, o modal de criação de pedido no React dependeria de dados fixos (*hardcoded*) ou digitação manual de IDs. Além disso, sem `Email` na tabela `Clientes`, não haveria atributo único de negócio para diferenciar clientes homônimos.
   - *Correção aplicada:* Incluímos `Email` com índice único (`UX_Clientes_Email`), persistimos `ValorTotal` em `Pedidos` (acelerando a listagem paginada sem descaracterizar o cálculo por itens no relatório Top 5) e criamos os endpoints auxiliares `GET /clientes` e `GET /produtos`.

4. **Tratamento de Fuso Horário (`DateTimeKind.Utc`) na Materialização do Dapper:**
   - *Problema:* Colunas `DATETIME2` lidas do SQL Server pelo Dapper retornam `DateTime` com `Kind = Unspecified`, o que fazia a serialização JSON omitir o sufixo `Z` (UTC) nas listagens.
   - *Correção aplicada:* No `OrderReadRepository`, normalizamos com `DateTime.SpecifyKind(order.OrderDate, DateTimeKind.Utc)` para manter consistência ISO 8601 com o retorno do `POST /pedidos`.

---

## 6. Parte 4 — Debug e Otimização da Query SQL

### Query Original do Desafio

```sql
SELECT c.Nome, SUM(i.Quantidade * i.PrecoUnitario) AS Total
FROM Clientes c
LEFT JOIN Pedidos p ON p.ClienteId = c.Id
LEFT JOIN ItensPedido i ON i.PedidoId = p.Id
WHERE YEAR(p.DataPedido) = 2026
GROUP BY c.Nome
ORDER BY Total
```

---

### 6.1 O que estava errado? (Por que é lenta e por que retorna resultados incorretos)

A consulta original possui **5 problemas críticos** divididos entre **corretude de negócio** e **performance de banco de dados**:

#### Problemas de Corretude (Resultados Incorretos)

1. **`GROUP BY c.Nome` agrupa clientes homônimos em uma única linha:**
   - Agrupar apenas pelo nome faz com que dois clientes distintos que possuam o mesmo nome tenham seus pedidos somados como se fossem a mesma pessoa.
   - Na massa de dados do projeto (`database/02_seed.sql`), existem duas clientes chamadas **`Ana Silva`**:
     - `Id = 1` (`ana.silva@email.com`): comprou **R$ 6.600,00** em 2026 (Pedidos 1 e 2).
     - `Id = 6` (`ana.silva.sp@email.com`): comprou **R$ 720,00** em 2026 (Pedido 8).
   - A query original funde ambas em uma única linha com `Total = 7320.00`, distorcendo completamente o ranking. O agrupamento correto deve incluir a chave primária: `GROUP BY c.Id, c.Nome`.

2. **`ORDER BY Total` ordena de forma crescente (`ASC` por padrão):**
   - O objetivo da consulta é retornar o ranking dos clientes que **mais compraram**, mas omitir a direção no `ORDER BY` aplica `ASC` (do menor comprador para o maior), colocando os menores valores no topo. O correto é `ORDER BY Total DESC`.

3. **Uso contraditório de `LEFT JOIN` anulado pelo `WHERE`:**
   - A query utiliza `LEFT JOIN Pedidos p`, o que manteria clientes sem pedidos com colunas de `p` iguais a `NULL`. Porém, o filtro `WHERE YEAR(p.DataPedido) = 2026` avalia `UNKNOWN` quando `p.DataPedido IS NULL`, eliminando esses clientes e convertendo implicitamente o `LEFT JOIN` em um `INNER JOIN`.
   - Além disso, o `LEFT JOIN ItensPedido i` manteria cabeçalhos de pedidos sem itens com `SUM(...) = NULL`. Como o ranking deve listar quem efetivamente comprou no período, o relacionamento correto e explícito é **`INNER JOIN`**.

4. **Ausência de `TOP 5` (no contexto do endpoint `/relatorios/top-clientes`):**
   - Sem `TOP (5)`, a consulta transfere e ordena todos os clientes do banco em vez de limitar o conjunto de saída aos 5 maiores compradores.

#### Problemas de Performance (Por que a Query é Lenta)

5. **Predicado Não-Sargável (`WHERE YEAR(p.DataPedido) = 2026`):**
   - Aplicar a função determinística `YEAR()` sobre a coluna `p.DataPedido` impede que o Query Optimizer do SQL Server utilize a árvore B-Tree de um índice em `DataPedido` para fazer **Index Seek** (busca direta no intervalo).
   - O SQL Server é obrigado a executar um **Scan** (varredura completa da tabela ou do índice), lendo **todas as linhas de todos os anos** da tabela `Pedidos` e calculando `YEAR(DataPedido)` linha a linha (*CPU overhead* + *I/O* desnecessário).
   - **Correção Sargável:** Substituir a função na coluna por um intervalo semiaberto direto na coluna indexada:
     ```sql
     WHERE p.DataPedido >= '2026-01-01T00:00:00'
       AND p.DataPedido <  '2027-01-01T00:00:00'
     ```

---

### 6.2 Como o Problema foi Investigado (Plano de Execução, Estatísticas e Índices)

A investigação foi conduzida analisando o **Plano de Execução** (`SET SHOWPLAN_ALL ON` / *Actual Execution Plan*) e as métricas de leitura de páginas (`SET STATISTICS IO, TIME ON`) no SQL Server 2022:

1. **Comportamento da Query Original (Antes da Otimização):**
   - Mesmo existindo índice em `DataPedido`, o predicado `YEAR(p.DataPedido) = 2026` aparece no plano como `Predicate` residual dentro de um operador **`Clustered Index Scan`** (ou `Index Scan`) na tabela `Pedidos`, e não como `Seek Predicate`.
   - Sem um índice de cobertura em `ItensPedido(PedidoId)`, para cada item encontrado o SQL Server precisa varrer `PK_ItensPedido` (**`Clustered Index Scan`**) ou realizar **`Key Lookup`** para buscar as colunas `Quantidade` e `PrecoUnitario`.
   - O operador de agregação (`Hash Match` / `Sort`) trabalha sobre `c.Nome` (`NVARCHAR(150)`), que consome mais memória (*Memory Grant*) do que agrupar pela chave inteira `c.Id`.

2. **Estratégia de Indexação Implementada em [`database/01_schema.sql`](database/01_schema.sql):**
   - **`IX_Pedidos_DataPedido_ClienteId`:**
     ```sql
     CREATE NONCLUSTERED INDEX IX_Pedidos_DataPedido_ClienteId
         ON dbo.Pedidos (DataPedido DESC, ClienteId);
     ```
     *Por que funciona:* Com o filtro sargável (`>= '2026-01-01' AND < '2027-01-01'`), o SQL Server executa um **`Index Seek`** direto na faixa de datas de 2026. Como o índice já contém `ClienteId` na chave e inclui automaticamente o `Id` do pedido (chave clusterizada da tabela `Pedidos`), ele **cobre 100% da tabela `Pedidos`** para esta query, sem nenhum *Key Lookup*.
   - **`IX_ItensPedido_PedidoId` (Covering Index com `INCLUDE`):**
     ```sql
     CREATE NONCLUSTERED INDEX IX_ItensPedido_PedidoId
         ON dbo.ItensPedido (PedidoId) INCLUDE (Quantidade, PrecoUnitario);
     ```
     *Por que funciona:* Permite fazer **`Index Seek`** por `PedidoId` e já entrega `Quantidade` e `PrecoUnitario` nas páginas folha do índice não-clusterizado, calculando `SUM(i.Quantidade * i.PrecoUnitario)` sem tocar na tabela base (`PK_ItensPedido`).

---

### 6.3 Query Corrigida e Otimizada

```sql
SELECT TOP 5
    c.Id AS CustomerId,
    c.Nome AS CustomerName,
    COUNT(DISTINCT p.Id) AS OrdersCount,
    SUM(i.Quantidade * i.PrecoUnitario) AS TotalAmount
FROM dbo.Clientes AS c
INNER JOIN dbo.Pedidos AS p ON p.ClienteId = c.Id
INNER JOIN dbo.ItensPedido AS i ON i.PedidoId = p.Id
WHERE p.DataPedido >= '2026-01-01T00:00:00'
  AND p.DataPedido <  '2027-01-01T00:00:00'
GROUP BY c.Id, c.Nome
ORDER BY TotalAmount DESC, c.Id ASC;
```

### 6.4 Comparativo de Resultados na Massa de Dados (`database/02_seed.sql`)

| Posição | Resultado da Query Original (Incorreto) | Resultado da Query Corrigida (Correto — Top 5 de 2026) |
|---|---|---|
| **1º** | `Eduarda Lima` — R$ 1.200,00 *(ordenado ASC)* | **`Ana Silva` (Id 1)** — 2 pedidos — **R$ 6.600,00** |
| **2º** | `Daniel Souza` — R$ 2.440,00 | **`Carla Mendes` (Id 3)** — 2 pedidos — **R$ 5.060,00** |
| **3º** | `Bruno Costa` — R$ 4.500,00 | **`Bruno Costa` (Id 2)** — 1 pedido em 2026 — **R$ 4.500,00** *(ignora pedido de 2025)* |
| **4º** | `Carla Mendes` — R$ 5.060,00 | **`Daniel Souza` (Id 4)** — 1 pedido — **R$ 2.440,00** |
| **5º** | `Ana Silva` — **R$ 7.320,00** *(somou indevidamente Id 1 + Id 6)* | **`Eduarda Lima` (Id 5)** — 1 pedido — **R$ 1.200,00** |

> *Observação:* Na query corrigida, a segunda **`Ana Silva` (`Id = 6`, R$ 720,00 em 2026)** permanece separada (6º lugar, fora do `TOP 5`), **`Felipe Rocha` (`Id = 7`)** não aparece pois seu único pedido é de `2025`, e **`Gabriela Alves` (`Id = 8`)** não aparece pois não possui pedidos.

---

## 7. Endpoints da API

| Método | Rota | Descrição | Status Codes |
|---|---|---|---|
| `POST` | `/pedidos` | Cria um novo pedido validando cliente, produtos e baixando estoque atomicamente | `201 Created`, `400 Bad Request`, `404 Not Found`, `409 Conflict` |
| `GET` | `/pedidos` | Lista pedidos com filtros opcionais (`customerId`, `startDate`, `endDate`) e paginação (`page`, `pageSize`) | `200 OK`, `400 Bad Request` |
| `GET` | `/relatorios/top-clientes` | Retorna os 5 clientes com maior valor total comprado no período (`startDate`, `endDate`) | `200 OK`, `400 Bad Request` |
| `GET` | `/clientes` | Lista clientes cadastrados ordenados por nome (apoio ao Frontend) | `200 OK` |
| `GET` | `/produtos` | Lista produtos com preço atual e saldo em estoque (apoio ao Frontend) | `200 OK` |
| `GET` | `/health` | Endpoint de verificação de saúde da API | `200 OK` |
