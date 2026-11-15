# Constitution — Mini Gestão de Pedidos

Este documento define os princípios, restrições e padrões permanentes do projeto **Mini Gestão de Pedidos**. Todas as especificações (`spec.md`), planos (`plan.md`), tarefas (`task.md`) e implementações devem obedecer a estas regras.

---

## 1. Propósito e Escopo do Projeto

- Construir uma aplicação de cadastro e consulta de **Pedidos de Clientes**, demonstrando fluxo estruturado com IA (Spec-Driven Development), qualidade de código, modelagem SQL e testes automatizados.
- **Escopo enxuto (somente requisitos obrigatórios):** Não implementar diferenciais fora do escopo definido (sem testes E2E com Playwright, sem pipeline de GitHub Actions e sem deploy online).

---

## 2. Fluxo de Trabalho (SDD com Antigravity)

Todo o ciclo de especificação e planejamento é centralizado na pasta `spec/`:

1. `spec/constitution.md` — Regras permanentes e padrões inegociáveis do projeto (este arquivo).
2. `spec/spec.md` — Especificação funcional (objetivo, regras de negócio, endpoints, modelo de dados e critérios de aceite).
3. `spec/plan.md` — Plano técnico de implementação, decisões arquiteturais e registro crítico de ajustes ou discordâncias em relação às propostas geradas pela IA.
4. `spec/task.md` — Lista de tarefas incrementais e verificáveis para guiar a execução e os commits.

> **Nota de adaptação:** Este projeto utiliza a pasta `spec/` no lugar de `docs/`, `CLAUDE.md` e `.claude/` para condução do fluxo via Antigravity.

---

## 3. Stack Tecnológica Obrigatória

### 3.1 Banco de Dados
- **SGBD:** SQL Server.
- **Tabelas obrigatórias:** `Clientes`, `Produtos`, `Pedidos` e `ItensPedido`, com chaves primárias, chaves estrangeiras e índices adequados.
- **Scripts:** Scripts de criação de schema e carga de dados de exemplo devem residir na pasta `database/`.

### 3.2 Backend
- **Plataforma:** `.NET 10`.
- **Arquitetura:** Em camadas — `Domain`, `Application`, `Infrastructure` e `API`.
- **Acesso a dados:** Híbrido com **Entity Framework Core** e **Dapper**.
- **Documentação da API:** OpenAPI com **Scalar** (não utilizar Swagger UI).
- **Tratamento de erros:** Respostas de erro padronizadas utilizando **ProblemDetails (RFC 7807)**.
- **Endpoints obrigatórios:**
  - `POST /pedidos` — Criação de pedido com itens, validando existência do cliente e disponibilidade de estoque.
  - `GET /pedidos` — Listagem paginada de pedidos com filtros por cliente e período.
  - `GET /relatorios/top-clientes` — Top 5 clientes com maior valor total de pedidos no período informado.

### 3.3 Frontend
- **Stack:** **React** com **TypeScript**.
- **Arquitetura minimalista:** Manter o frontend o mais simples e enxuto possível, com o **mínimo de dependências externas** (evitar bibliotecas complexas de estado global, formulários ou data-fetching como Redux, Zustand ou React Query; usar recursos nativos como `fetch`, `useState`, `useEffect` e CSS simples).
- **Funcionalidades obrigatórias:**
  - Tela de listagem de pedidos com filtros.
  - Tela ou modal de criação de pedido.

---

## 4. Estilo de Código, Nomenclatura e Comentários

- **Simplicidade e legibilidade:** Escrever código simples, direto e fácil de entender e explicar; evitar abstrações excessivas ou complexidade desnecessária (*over-engineering*).
- **Identificadores em inglês:** Nomes de classes, interfaces, funções, métodos, variáveis, parâmetros, propriedades e componentes devem ser claros, descritivos e escritos **em inglês**.
  - *Exceção de contrato:* Os nomes das tabelas SQL (`Clientes`, `Produtos`, `Pedidos`, `ItensPedido`) e as rotas de API (`/pedidos`, `/relatorios/top-clientes`) exigidos no requisito original são preservados no banco e nos endpoints, sendo mapeados para entidades e símbolos em inglês no código (ex.: entidade `Order` mapeada para a tabela `Pedidos`).
- **Tradução e explicação em comentários (português):** Variáveis, funções, métodos e trechos-chave devem trazer ao lado (ou logo acima) comentários curtos em **português** contendo a **tradução do nome** e uma **explicação objetiva** da sua responsabilidade, facilitando o estudo e a defesa técnica do código.
- **Detalhamento extra no Frontend:** Como o foco é total compreensão e facilidade de explicação, o código React/TypeScript deve conter **comentários didáticos mais detalhados** em português — explicando não apenas a tradução dos nomes, mas também o papel de cada estado (`useState`), efeito (`useEffect`), propriedade (`props`), evento e chamada HTTP (`fetch`) no fluxo da tela.

---

## 5. Regras de Qualidade de Dados e Performance SQL

- Queries SQL e consultas de relatório devem ser **sargáveis** (evitar funções aplicadas sobre colunas indexadas em filtros `WHERE`, como `YEAR(DataPedido)`).
- Joins devem respeitar a semântica do filtro (evitar `LEFT JOIN` transformado em filtro implícito ou que produza linhas incorretas/nulas indesejadas).
- Consultas críticas (como o ranking de clientes da Parte 4 do desafio) devem ter índices e plano de execução analisados e documentados.

---

## 6. Estratégia de Testes

- **Framework de testes:** **xUnit**.
- **Testes unitários (obrigatórios):** Devem cobrir as regras de negócio e validações do pedido (ex.: cliente inexistente, estoque insuficiente, pedido sem itens, etc.).
- **Testes de integração (obrigatórios):** Devem exercitar os endpoints da API contra um **SQL Server real** utilizando **Testcontainers** (execução automatizada e isolada, sem depender de setup manual).

---

## 7. Containerização (Docker)

- Deve existir um `docker-compose.yml` na raiz do repositório capaz de subir toda a aplicação com um único comando (`docker compose up`), sem necessidade de ajustes manuais:
  - SQL Server (executando automaticamente os scripts de criação e carga de `database/`).
  - Backend `.NET 10`.
  - Frontend `React`.

---

## 8. Documentação (`README.md`)

O `README.md` na raiz do repositório deve documentar obrigatoriamente:
1. Como rodar o projeto com Docker.
2. Como rodar os testes (unitários e de integração).
3. Principais decisões técnicas.
4. Como a IA foi utilizada: o que funcionou bem e o que precisou ser corrigido no código gerado.
5. Explicação técnica da **Parte 4 (debug da query)**: o que estava errado (lenta e resultados incorretos) e como o problema foi investigado (plano de execução, índices, reescrita da query).

---

## 9. Versionamento e Git

- **Conventional Commits:** Mensagens claras seguindo o padrão `feat:`, `fix:`, `test:`, `docs:`, `refactor:`, `chore:`.
- **Commits granulares:** Preferir commits pequenos, frequentes e focados em entregas incrementais.
- **Preservação do histórico:** É **proibido** fazer `squash`, `amend` em commits já compartilhados ou reescrever o histórico do Git.

---

## 10. Comandos Padrão de Verificação

- **Build do backend:** `dotnet build`
- **Execução dos testes (unitários e integração):** `dotnet test`
- **Subida completa via Docker:** `docker compose up --build`
