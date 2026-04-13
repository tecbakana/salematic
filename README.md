# Salematic

Agente de vendas com IA conversacional. O cliente interage via chat em linguagem natural; a LLM executa ferramentas para consultar estoque, cadastrar clientes, registrar pedidos e manter dados atualizados.

Integrado ao **CMSX**, que consome a API do Salematic para exibir pedidos.

---

## Stack

| Camada | Tecnologia |
|---|---|
| API | .NET 8 — ASP.NET Core |
| ORM | Dapper |
| Banco | SQL Server (SalematicDB) |
| Cache | Redis (prod) / MemoryCache (dev) |
| Frontend | React 18 + TypeScript + Vite |
| LLM padrão | Gemini 2.5 Flash |
| LLM alternativa | Claude Sonnet 4.6 |

---

## Estrutura

```
Salematic.API/           — Controllers, middlewares, configuração
Salematic.Application/   — ChatService, AgentToolsService, DTOs
Salematic.Domain/        — Entidades, interfaces de repositório
Salematic.Infrastructure/ — Repositórios (Dapper), clientes LLM
Salematic.Tests/
database/                — Scripts SQL
dev-requests/            — Fila de solicitações do agente (queue.json)
frontend/                — React app
```

---

## Configuração

API keys e secrets são gerenciados via `dotnet user-secrets` — o `appsettings.json` não contém valores sensíveis.

```bash
dotnet user-secrets set "Llm:Anthropic:ApiKey" "<key>"
dotnet user-secrets set "Llm:Gemini:ApiKey" "<key>"
dotnet user-secrets set "Webhook:Secret" "<secret>"
```

`appsettings.json` relevante:

```json
{
  "ConnectionStrings": {
    "SalematicDB": "Server=localhost\\SQLEXPRESS;Database=SalematicDB;Trusted_Connection=True;TrustServerCertificate=True;",
    "Redis": ""
  },
  "Llm": {
    "Provider": "Gemini",
    "Gemini": { "Model": "gemini-2.5-flash" },
    "Anthropic": { "Model": "claude-sonnet-4-6" }
  }
}
```

Para trocar de provider, altere `Llm:Provider` para `"Anthropic"`.

---

## Banco de dados

Script de criação: `database/create.sql`

Migrations:
- `database/migration_endereco_cliente.sql` — adiciona colunas de endereço na tabela Clientes

Tabelas:

| Tabela | Descrição |
|---|---|
| `Clientes` | Dados cadastrais + endereço |
| `Produtos` | Catálogo com preço e unidade |
| `Estoques` | Quantidade por produto |
| `Pedidos` | Cabeçalho do pedido |
| `ItensPedido` | Linhas do pedido |

---

## Endpoints

### `POST /api/chat/mensagem`

Ponto de entrada do chat. Recebe histórico + mensagem atual, devolve resposta do agente.

```json
// Request
{
  "mensagem": "Quero comprar um teclado mecânico",
  "historico": []
}

// Response
{
  "resposta": "...",
  "ferramentaUsada": "consultar_estoque",
  "historico": [...]
}
```

### `POST /api/pedidos/webhook`

Recebe notificações externas de pedidos (integração CMSX). Requer header `X-Webhook-Secret` quando configurado.

---

## Ferramentas do agente

A LLM aciona as ferramentas abaixo durante a conversa conforme a necessidade.

### `consultar_estoque`
Busca produtos por nome (aceita termo único ou lista de termos). Retorna id, nome, descrição, preço, unidade e quantidade em estoque.

| Parâmetro | Tipo | Obrigatório |
|---|---|---|
| `nome_produto` | `string[]` | sim |

---

### `cadastrar_cliente`
Cria um novo cliente. Retorna o `cliente_id` gerado.

| Parâmetro | Tipo | Obrigatório |
|---|---|---|
| `nome` | string | sim |
| `documento` | string (CPF/CNPJ) | sim |
| `email` | string | não |
| `telefone` | string | não |

---

### `consultar_cliente`
Retorna todos os dados cadastrais de um cliente (nome, documento, e-mail, telefone e endereço completo). Usar antes de atualizar para verificar o que já existe.

| Parâmetro | Tipo | Obrigatório |
|---|---|---|
| `cliente_id` | integer | sim |

---

### `atualizar_cliente`
Atualiza dados cadastrais. Informar apenas os campos que devem mudar — os demais são mantidos.

| Parâmetro | Tipo | Obrigatório |
|---|---|---|
| `cliente_id` | integer | sim |
| `nome` | string | não |
| `documento` | string | não |
| `email` | string | não |
| `telefone` | string | não |

---

### `atualizar_endereco`
Atualiza o endereço via CEP (consulta automática no ViaCEP). Se o CEP não retornar logradouro (CEP rural, condomínio), informar `logradouro` manualmente.

| Parâmetro | Tipo | Obrigatório |
|---|---|---|
| `cliente_id` | integer | sim |
| `cep` | string | sim |
| `numero` | string | sim |
| `complemento` | string | não |
| `logradouro` | string | não (sobrescreve ViaCEP) |

---

### `registrar_pedido`
Cria um novo pedido. Valida estoque de cada item antes de registrar.

| Parâmetro | Tipo | Obrigatório |
|---|---|---|
| `cliente_id` | integer | sim |
| `itens` | string (JSON) | sim |

Formato de `itens`: `[{"ProdutoId": 1, "Quantidade": 2}]`

---

### `consultar_pedidos`
Lista todos os pedidos de um cliente com itens, valores e status.

| Parâmetro | Tipo | Obrigatório |
|---|---|---|
| `cliente_id` | integer | sim |

---

### `cancelar_pedido`
Cancela um pedido pelo ID.

| Parâmetro | Tipo | Obrigatório |
|---|---|---|
| `pedido_id` | integer | sim |

---

### `solicitar_desenvolvimento` *(somente ambiente dev)*

Registra uma solicitação de nova funcionalidade na fila `dev-requests/queue.json`. Se a solicitação envolver acesso a uma API externa, incluir `url_externa` — o status será `aguardando_aprovacao` até autorização manual.

| Parâmetro | Tipo | Obrigatório |
|---|---|---|
| `descricao` | string | sim |
| `tipo` | `expor_campo` \| `nova_ferramenta` \| `novo_endpoint` \| `correcao` | sim |
| `impacto` | `baixo` \| `medio` \| `alto` | sim |
| `detalhes` | string | não |
| `url_externa` | string | não |

Status possíveis na fila: `pendente`, `em_andamento`, `aguardando_aprovacao`, `concluido`, `ignorado`.

---

## Rodando localmente

```bash
# API
cd Salematic.API
dotnet run

# Frontend
cd frontend
npm install
npm run dev
```

Swagger disponível em `http://localhost:<porta>/swagger` no ambiente de desenvolvimento.
