# Spec — Mini Gestão de Pedidos

Especificação funcional da aplicação **Mini Gestão de Pedidos**, elaborada em conformidade com a [constitution.md](constitution.md) e os requisitos do [teste.md](../teste.md).

---

## 1. Objetivo

Disponibilizar uma aplicação web completa para cadastro e consulta de **Pedidos de Clientes**, composta por:
- Banco de dados **SQL Server** com modelagem relacional, integridade referencial, índices adequados e massa de dados inicial.
- API REST em **.NET 10** responsável pelas regras de negócio, validações transacionais, consultas paginadas e relatório de ranking de clientes.
- Interface web minimalista em **React + TypeScript** para listagem filtrada de pedidos, criação de novos pedidos e visualização do Top 5 clientes.

---

## 2. Modelo de Dados

### 2.1 Entidades e Tabelas (SQL Server)

> **Convenção:** Os nomes físicos das tabelas e colunas no banco seguem o português conforme exigido pelo desafio; no código (.NET e React), os símbolos são mapeados em inglês conforme a `constitution.md`.

| Tabela SQL | Colunas | Restrições e Índices |
|---|---|---|
| `Clientes` | `Id` (PK, `INT IDENTITY`), `Nome` (`NVARCHAR(150) NOT NULL`), `Email` (`NVARCHAR(200) NOT NULL`) | PK em `Id`; Índice único em `Email` |
| `Produtos` | `Id` (PK, `INT IDENTITY`), `Nome` (`NVARCHAR(150) NOT NULL`), `Preco` (`DECIMAL(18,2) NOT NULL`), `Estoque` (`INT NOT NULL`) | PK em `Id`; `CHECK (Preco >= 0)`; `CHECK (Estoque >= 0)` |
| `Pedidos` | `Id` (PK, `INT IDENTITY`), `ClienteId` (FK, `INT NOT NULL`), `DataPedido` (`DATETIME2 NOT NULL`), `ValorTotal` (`DECIMAL(18,2) NOT NULL`) | PK em `Id`; FK `ClienteId -> Clientes(Id)`; `CHECK (ValorTotal >= 0)`; Índice composto em `(DataPedido DESC, ClienteId)` |
| `ItensPedido` | `Id` (PK, `INT IDENTITY`), `PedidoId` (FK, `INT NOT NULL`), `ProdutoId` (FK, `INT NOT NULL`), `Quantidade` (`INT NOT NULL`), `PrecoUnitario` (`DECIMAL(18,2) NOT NULL`) | PK em `Id`; FK `PedidoId -> Pedidos(Id)`; FK `ProdutoId -> Produtos(Id)`; `CHECK (Quantidade > 0)`; `CHECK (PrecoUnitario >= 0)`; Índice em `PedidoId` (incluindo `Quantidade, PrecoUnitario`) e Índice em `ProdutoId` |

### 2.2 Relacionamentos
- Um **Cliente** possui `0..N` **Pedidos**.
- Um **Pedido** pertence a `1` **Cliente** e possui `1..N` **ItensPedido**.
- Um **Produto** pode estar referenciado em `0..N` **ItensPedido**.

---

## 3. Regras de Negócio

- **RN-01 (Existência do Cliente):** Ao criar um pedido, o `customerId` informado deve existir na tabela `Clientes`. Se não existir, a operação falha com `404 Not Found`.
- **RN-02 (Pedido sem Itens):** Um pedido deve conter ao menos 1 item. Lista nula ou vazia falha com `400 Bad Request`.
- **RN-03 (Quantidade Positiva):** Cada item do pedido deve ter `quantity > 0`. Valores menores ou iguais a zero falham com `400 Bad Request`.
- **RN-04 (Consolidação de Produto Repetido):** Caso o mesmo `productId` seja enviado mais de uma vez na lista de itens de um mesmo pedido, as quantidades devem ser somadas em um único item antes de validar o estoque e persistir.
- **RN-05 (Existência do Produto):** Todos os `productId` informados devem existir na tabela `Produtos`. Se algum não existir, a operação falha com `404 Not Found`.
- **RN-06 (Validação e Baixa Atômica de Estoque):** Para cada produto consolidado, `Produtos.Estoque` deve ser maior ou igual à quantidade solicitada. Se houver estoque insuficiente para qualquer item, nenhum pedido é criado e nenhum estoque é alterado, retornando `409 Conflict`. Havendo estoque suficiente, o estoque é decrementado na mesma transação de criação do pedido.
- **RN-07 (Preço e Total Calculados no Servidor):** O `PrecoUnitario` salvo em `ItensPedido` é sempre copiado de `Produtos.Preco` no momento da compra (nunca informado pelo cliente). O `ValorTotal` do pedido é calculado no servidor como `SUM(Quantidade * PrecoUnitario)`.
- **RN-08 (Data do Pedido):** Definida automaticamente pelo servidor em UTC no momento da criação do pedido.
- **RN-09 (Validação de Período em Consultas):** Nos endpoints que aceitam `startDate` e `endDate`, se ambos forem informados e `startDate > endDate`, retornar `400 Bad Request`.
- **RN-10 (Regra do Ranking Top 5 Clientes):** O relatório deve considerar apenas clientes que possuem pedidos dentro do período informado (sem incluir clientes sem compras no período) e retornar no máximo os **5 primeiros**, ordenados do maior valor total comprado para o menor (`DESC`). O valor total é calculado a partir dos itens (`SUM(Quantidade * PrecoUnitario)`).

---

## 4. Especificação dos Endpoints (API)

> Todas as rotas seguem os caminhos definidos no requisito, com payloads JSON em `camelCase` (propriedades em inglês) e respostas de erro padronizadas com **ProblemDetails (RFC 7807)**.

### 4.1 `POST /pedidos` — Criar Pedido
- **Objetivo:** Cadastrar um novo pedido com seus itens, validando cliente, produtos e baixando estoque atomicamente.
- **Request Body (`201 Created` em caso de sucesso):**
  ```json
  {
    "customerId": 1,
    "items": [
      { "productId": 10, "quantity": 2 },
      { "productId": 12, "quantity": 1 }
    ]
  }
  ```
- **Response Body (`201 Created`):**
  ```json
  {
    "id": 42,
    "customerId": 1,
    "customerName": "Ana Silva",
    "orderDate": "2026-11-15T18:00:00Z",
    "totalAmount": 250.00,
    "items": [
      {
        "productId": 10,
        "productName": "Mouse Sem Fio",
        "quantity": 2,
        "unitPrice": 100.00,
        "subtotal": 200.00
      },
      {
        "productId": 12,
        "productName": "Mousepad",
        "quantity": 1,
        "unitPrice": 50.00,
        "subtotal": 50.00
      }
    ]
  }
  ```
- **Respostas de Erro (`ProblemDetails`):**
  - `400 Bad Request`: lista de itens vazia, quantidade `<= 0` ou `customerId <= 0`.
  - `404 Not Found`: cliente ou algum produto não encontrado.
  - `409 Conflict`: estoque insuficiente para um ou mais produtos.

---

### 4.2 `GET /pedidos` — Listar Pedidos com Filtros e Paginação
- **Objetivo:** Retornar lista paginada de pedidos ordenada do mais recente para o mais antigo (`DataPedido DESC, Id DESC`).
- **Query Parameters (todos opcionais):**
  - `customerId` (`int`): filtra pedidos de um cliente específico.
  - `startDate` (`DateOnly` / `YYYY-MM-DD`): data inicial (inclusiva, a partir de `00:00:00`).
  - `endDate` (`DateOnly` / `YYYY-MM-DD`): data final (inclusiva, até o fim do dia `< endDate + 1 dia`).
  - `page` (`int`, padrão `1`, mínimo `1`): número da página.
  - `pageSize` (`int`, padrão `10`, mínimo `1`, máximo `100`): quantidade de registros por página.
- **Response Body (`200 OK`):**
  ```json
  {
    "items": [
      {
        "id": 42,
        "customerId": 1,
        "customerName": "Ana Silva",
        "orderDate": "2026-11-15T18:00:00Z",
        "totalAmount": 250.00,
        "items": [
          {
            "productId": 10,
            "productName": "Mouse Sem Fio",
            "quantity": 2,
            "unitPrice": 100.00,
            "subtotal": 200.00
          }
        ]
      }
    ],
    "page": 1,
    "pageSize": 10,
    "totalCount": 1,
    "totalPages": 1
  }
  ```
- **Respostas de Erro (`ProblemDetails`):**
  - `400 Bad Request`: `startDate > endDate`, `page < 1` ou `pageSize` fora de `1..100`.

---

### 4.3 `GET /relatorios/top-clientes` — Top 5 Clientes no Período
- **Objetivo:** Retornar os até 5 clientes com maior valor total de pedidos no período informado.
- **Query Parameters (opcionais):**
  - `startDate` (`DateOnly` / `YYYY-MM-DD`): início do período (inclusivo).
  - `endDate` (`DateOnly` / `YYYY-MM-DD`): fim do período (inclusivo).
  - *Nota:* Se omitidos, considera todos os pedidos registrados; se apenas um for informado, aplica apenas o limite correspondente.
- **Response Body (`200 OK`):**
  ```json
  [
    {
      "customerId": 1,
      "customerName": "Ana Silva",
      "ordersCount": 3,
      "totalAmount": 1850.00
    }
  ]
  ```
- **Respostas de Erro (`ProblemDetails`):**
  - `400 Bad Request`: `startDate > endDate`.

---

### 4.4 Endpoints Auxiliares para o Frontend

Para permitir que a interface React carregue clientes e produtos reais do SQL Server sem dados fixos no código:

- **`GET /clientes`**: Retorna lista de clientes ordenada por nome (`[{ "id": 1, "name": "Ana Silva", "email": "ana@email.com" }]`).
- **`GET /produtos`**: Retorna lista de produtos ordenada por nome (`[{ "id": 10, "name": "Mouse Sem Fio", "price": 100.00, "stock": 15 }]`).

---

## 5. Correção da Query de Ranking (Parte 4)

A query original apresentada no teste apresenta problemas de corretude e performance:
1. **Filtro não-sargável:** `WHERE YEAR(p.DataPedido) = 2026` impede o uso eficiente de índices em `Pedidos.DataPedido` (forçando scan). Deve ser substituído por intervalo sargável (`p.DataPedido >= '2026-01-01' AND p.DataPedido < '2027-01-01'`).
2. **Semântica de JOIN inconsistente:** Usa `LEFT JOIN` em `Pedidos` e `ItensPedido`, mas o `WHERE` em `p.DataPedido` anula o efeito do `LEFT JOIN` (convertendo na prática em `INNER JOIN` sem clareza de intenção). Deve usar `INNER JOIN` explicitamente.
3. **Agrupamento incorreto por nome:** `GROUP BY c.Nome` agrupa clientes diferentes que tenham o mesmo nome. Deve agrupar por `(c.Id, c.Nome)`.
4. **Ordenação invertida e sem limite:** `ORDER BY Total` ordena do menor para o maior (`ASC`) em vez dos que mais compraram (`DESC`), e não limita aos primeiros resultados quando aplicável (`TOP 5`).

---

## 6. Requisitos da Interface Web (Frontend React + TypeScript)

Interface simples, de página única e fácil entendimento, contendo:

1. **Card/Seção Top 5 Clientes:**
   - Consome `GET /relatorios/top-clientes`.
   - Permite informar período opcional (`startDate` e `endDate`) e atualizar o ranking.
   - Exibe posição, nome do cliente, quantidade de pedidos e valor total comprado.
2. **Listagem de Pedidos com Filtros e Paginação:**
   - Consome `GET /pedidos` e `GET /clientes` (para o `<select>` de filtro por cliente).
   - Filtros por **Cliente**, **Data Inicial** e **Data Final**, além de botão para limpar filtros.
   - Tabela exibindo número do pedido, cliente, data formatada, resumo dos itens e valor total.
   - Controles simples de paginação (*Anterior* / *Próxima* + indicação da página atual).
3. **Criação de Pedido (Modal ou Formulário dedicado):**
   - Botão *"Novo Pedido"* abre o modal/formulário.
   - Seleção do **Cliente** via `<select>` (`GET /clientes`).
   - Adição e remoção dinâmica de itens selecionando **Produto** via `<select>` (mostrando nome, preço e estoque disponível via `GET /produtos`) e **Quantidade**.
   - Prévia do valor total calculada na tela antes do envio.
   - Ao confirmar, envia `POST /pedidos`:
     - Em sucesso (`201`): fecha o modal/limpa o formulário, exibe feedback positivo e recarrega listagem de pedidos, produtos (estoque atualizado) e Top 5 clientes.
     - Em erro (`400`/`404`/`409`): exibe de forma amigável a mensagem retornada pelo `ProblemDetails`.

---

## 7. Critérios de Aceite

1. **Banco de Dados:** Ao iniciar via Docker Compose, as tabelas `Clientes`, `Produtos`, `Pedidos` e `ItensPedido` são criadas automaticamente com PKs, FKs, índices e dados de exemplo da pasta `database/`.
2. **Criação de Pedido Válida (`POST /pedidos`):** Dado cliente existente e produto(s) com estoque suficiente, o pedido é criado com `201`, `ValorTotal` correto, `PrecoUnitario` copiado do catálogo e estoque decrementado no banco.
3. **Consolidação de Item Repetido:** Dado um mesmo produto repetido no payload cuja soma excede o estoque disponível, a requisição é rejeitada com `409 Conflict` e o estoque permanece intacto.
4. **Validações de Erro:**
   - Cliente inexistente → `404 ProblemDetails`.
   - Produto inexistente → `404 ProblemDetails`.
   - Pedido sem itens ou quantidade `<= 0` → `400 ProblemDetails`.
   - Estoque insuficiente → `409 ProblemDetails` e rollback completo da transação.
5. **Listagem Paginada (`GET /pedidos`):** Filtros por `customerId`, `startDate` e `endDate` retornam apenas os pedidos correspondentes, ordenados do mais recente ao mais antigo, respeitando `page` e `pageSize`.
6. **Top 5 Clientes (`GET /relatorios/top-clientes`):** Retorna no máximo 5 clientes ordenados por maior valor total no período, ignorando clientes sem pedidos no intervalo e usando consulta sargável.
7. **Testes Automatizados (`dotnet test`):**
   - Testes unitários cobrem todas as regras de validação e cálculo do pedido sem depender de banco externo.
   - Testes de integração exercitam os endpoints contra SQL Server real via **Testcontainers** e passam integralmente.
8. **Frontend Funcional:** É possível filtrar pedidos, navegar entre páginas, visualizar o Top 5 clientes e criar um novo pedido pela interface vendo-o imediatamente na listagem e com o estoque atualizado.
9. **Docker Turnkey:** `docker compose up --build` sobe SQL Server, Backend `.NET 10` e Frontend `React` funcionando em conjunto sem passos manuais.
