# Documento Funcional: SAGA de Pedidos CMSX <-> Salematic

**Data:** 2026-04-08
**Versao:** 1.0
**Padrao:** SAGA Coreografado (Choreography-based)

---

## 1. Visao Geral

O CMSX (Multiplai) e o Salematic se comunicam via Azure Service Bus usando o padrao SAGA coreografado.
Cada servico reage a eventos publicados pelo outro, sem orquestrador central.

```
CMSX                          Service Bus                     Salematic
  |                               |                               |
  |-- pedido.criado ------------->|                               |
  |                               |------------------------------>|
  |                               |                    processa pagamento
  |                               |<-- pagamento.confirmado ------|
  |<------------------------------|                               |
  | atualiza status, baixa estoque|                               |
```

### 1.1 Topologia Service Bus

| Recurso         | Nome                   | Quem usa           |
|-----------------|------------------------|--------------------|
| Namespace        | sb-limpmax-dev         | Ambos              |
| Topico (ida)     | top-pedidos            | CMSX publica, Salematic consome |
| Subscription     | sub-pedidos-salematic  | Salematic          |
| Topico (volta)   | top-status-pedidos     | Salematic publica, CMSX consome |
| Subscription     | sub-status-cmsx        | CMSX (ja existe)   |

> O topico `top-status-pedidos` e a subscription `sub-status-cmsx` ja existem e o consumer do CMSX ja escuta neles.
> O topico `top-pedidos` e a subscription `sub-pedidos-salematic` precisam ser criados.

---

## 2. Contratos de Evento

### 2.1 pedido.criado (CMSX -> Salematic)

Publicado pelo CMSX quando um pedido e registrado pela tela de pedidos.

```json
{
  "evento": "pedido.criado",
  "aplicacaoid": "guid-do-tenant",
  "numeropedido": "PED-2026-0042",
  "clienteId": 1,
  "clienteNome": "Joao Silva",
  "clienteDocumento": "12345678901",
  "clienteEmail": "joao@email.com",
  "clienteTelefone": "11999998888",
  "metodoPagamento": "PIX",
  "itens": [
    {
      "produtoId": 5,
      "quantidade": 3
    },
    {
      "produtoId": 12,
      "quantidade": 1
    }
  ],
  "timestamp": "2026-04-08T14:30:00Z"
}
```

**Campos obrigatorios:** aplicacaoid, numeropedido, clienteId, itens (min 1), metodoPagamento
**Campos opcionais:** clienteEmail, clienteTelefone
**metodoPagamento:** `PIX` | `BOLETO` | `CARTAO`

---

### 2.2 pagamento.confirmado (Salematic -> CMSX)

Publicado quando o pagamento e aprovado pelo gateway (ou mock).

```json
{
  "evento": "pagamento.confirmado",
  "aplicacaoid": "guid-do-tenant",
  "numeropedido": "PED-2026-0042",
  "pedidoIdSalematic": 47,
  "clienteNome": "Joao Silva",
  "clienteEmail": "joao@email.com",
  "valorPedido": 150.00,
  "codigoTransacao": "txn_a1b2c3d4",
  "status": "confirmado",
  "descricao": "Pagamento aprovado via PIX",
  "timestamp": "2026-04-08T14:30:05Z"
}
```

---

### 2.3 pagamento.recusado (Salematic -> CMSX)

Publicado quando o gateway recusa o pagamento.

```json
{
  "evento": "pagamento.recusado",
  "aplicacaoid": "guid-do-tenant",
  "numeropedido": "PED-2026-0042",
  "pedidoIdSalematic": 47,
  "clienteNome": "Joao Silva",
  "clienteEmail": "joao@email.com",
  "valorPedido": 150.00,
  "codigoTransacao": null,
  "status": "pagamento_recusado",
  "descricao": "Cartao recusado pelo banco emissor",
  "timestamp": "2026-04-08T14:30:04Z"
}
```

**Acao esperada no CMSX:** atualizar status para `pagamento_recusado`. NAO baixar estoque.

---

### 2.4 pagamento.pendente (Salematic -> CMSX)

Para metodos assincronos (boleto, PIX com prazo).

```json
{
  "evento": "pagamento.pendente",
  "aplicacaoid": "guid-do-tenant",
  "numeropedido": "PED-2026-0042",
  "pedidoIdSalematic": 47,
  "clienteNome": "Joao Silva",
  "clienteEmail": "joao@email.com",
  "valorPedido": 120.00,
  "codigoTransacao": "txn_e5f6g7h8",
  "linkPagamento": "https://gateway.com/pay/txn_e5f6g7h8",
  "pixCopiaCola": "00020126...",
  "codigoBarras": "34191.09008...",
  "dataVencimento": "2026-04-11",
  "status": "aguardando_pagamento",
  "descricao": "Boleto gerado — vencimento em 3 dias uteis",
  "timestamp": "2026-04-08T14:30:03Z"
}
```

**Acao esperada no CMSX:** atualizar status para `aguardando_pagamento`. Exibir link/codigo para o cliente. Aguardar evento definitivo (confirmado ou expirado).

---

### 2.5 pagamento.timeout (Salematic -> CMSX)

Gateway nao respondeu no tempo esperado.

```json
{
  "evento": "pagamento.timeout",
  "aplicacaoid": "guid-do-tenant",
  "numeropedido": "PED-2026-0042",
  "pedidoIdSalematic": 47,
  "valorPedido": 150.00,
  "status": "erro_timeout",
  "descricao": "Gateway nao respondeu em 30s",
  "timestamp": "2026-04-08T14:30:35Z"
}
```

**Acao esperada no CMSX:** atualizar status para `erro_timeout`. Pode agendar retry ou notificar operador.

---

### 2.6 pagamento.erro (Salematic -> CMSX)

Erro interno no processamento (excecao, gateway fora, etc).

```json
{
  "evento": "pagamento.erro",
  "aplicacaoid": "guid-do-tenant",
  "numeropedido": "PED-2026-0042",
  "pedidoIdSalematic": 47,
  "valorPedido": 150.00,
  "status": "erro",
  "descricao": "Erro interno: gateway indisponivel",
  "timestamp": "2026-04-08T14:30:04Z"
}
```

**Acao esperada no CMSX:** atualizar status para `erro`. Notificar operador. NAO baixar estoque.

---

## 3. Fluxos

### 3.1 Happy Path (PIX/Cartao aprovado)

```
1. CMSX cria pedido na tela de pedidos
2. CMSX publica "pedido.criado" no topico top-pedidos
3. Salematic consome o evento
4. Salematic valida cliente, produtos e estoque
5. Salematic cria o pedido local (status: aguardando_pagamento)
6. Salematic chama IPagamentoService.ProcessarAsync()
7. Gateway retorna aprovado
8. Salematic atualiza pedido local (status: confirmado)
9. Salematic publica "pagamento.confirmado" no topico top-status-pedidos
10. CMSX consome o evento
11. CMSX atualiza Pedido.Statusatual = "confirmado"
12. CMSX registra historico em Statuspedido
13. CMSX baixa estoque (IMPLEMENTAR)
```

### 3.2 Pagamento Recusado

```
1-6. Igual ao happy path
7. Gateway retorna recusado
8. Salematic atualiza pedido local (status: pagamento_recusado)
9. Salematic publica "pagamento.recusado" no top-status-pedidos
10. CMSX consome o evento
11. CMSX atualiza Pedido.Statusatual = "pagamento_recusado"
12. CMSX registra historico em Statuspedido
13. CMSX NAO baixa estoque
```

### 3.3 Boleto/PIX Pendente

```
1-6. Igual ao happy path
7. Gateway retorna pendente (boleto gerado)
8. Salematic atualiza pedido local (status: aguardando_pagamento)
9. Salematic publica "pagamento.pendente" no top-status-pedidos
10. CMSX consome, atualiza status, guarda link/codigo
11. (aguarda webhook do gateway confirmando pagamento)
12. Quando confirmado: Salematic publica "pagamento.confirmado"
13. CMSX finaliza o fluxo como happy path
```

### 3.4 Validacao Falhou no Salematic

```
1-3. Igual ao happy path
4. Salematic detecta erro (cliente nao encontrado, estoque insuficiente)
5. Salematic publica "pagamento.erro" com descricao do motivo
6. CMSX consome, atualiza status para "erro"
```

---

## 4. Implementacao por Servico

### 4.1 Salematic — O que implementar

#### A. Publisher de eventos (NOVO)

**Criar:** `Salematic.Infrastructure/ServiceBus/ServiceBusPublisher.cs`

```
Interface: IEventPublisher (em Salematic.Domain/Interfaces/)
    Task PublicarAsync(string evento, object payload)

Implementacao:
    - Recebe connection string e nome do topico via IConfiguration
    - Serializa payload para JSON
    - Envia via ServiceBusSender
    - Topico destino: top-status-pedidos
```

**Dependencia NuGet:** `Azure.Messaging.ServiceBus`

#### B. Consumer de pedido.criado (NOVO)

**Criar:** `Salematic.Infrastructure/ServiceBus/PedidoCriadoConsumer.cs`

```
BackgroundService que escuta:
    - Topico: top-pedidos
    - Subscription: sub-pedidos-salematic

Ao receber mensagem:
    1. Deserializar para PedidoCriadoEvent
    2. Mapear para WebhookPedidoRequest
    3. Chamar PedidoService.ProcessarAsync()
    4. Com base no resultado, publicar evento de retorno via IEventPublisher
    5. Incluir aplicacaoid e numeropedido no evento de retorno
```

#### C. Alterar PedidoService.ProcessarAsync()

**Arquivo:** `Salematic.Application/Services/PedidoService.cs`

Atualmente o metodo retorna `WebhookPedidoResponse` com sucesso/falha. Precisa:
1. Receber (ou propagar) `aplicacaoid` e `numeropedido` para incluir nos eventos de retorno
2. Publicar evento via `IEventPublisher` apos processar pagamento

Opcao limpa: injetar `IEventPublisher` no `PedidoService` e publicar direto no final do `ProcessarAsync()`.

#### D. Configuracao

**Arquivo:** `Salematic.API/appsettings.json`

```json
"ServiceBus": {
  "ConnectionString": "(via user-secrets)",
  "TopicoConsumo": "top-pedidos",
  "SubscriptionConsumo": "sub-pedidos-salematic",
  "TopicoPublicacao": "top-status-pedidos"
}
```

> Connection string via `dotnet user-secrets` — NAO colocar no appsettings.json.

---

### 4.2 CMSX — O que implementar

#### A. Publisher de pedido.criado (NOVO)

**Criar:** `CMSUI/Services/PedidosServiceBusPublisher.cs`

```
Classe com metodo:
    Task PublicarPedidoCriadoAsync(PedidoCriadoMsg msg)

Usa ServiceBusSender para publicar no topico: top-pedidos
Registrar como Singleton no DI
```

#### B. Chamar o publisher na criacao de pedido

Identificar onde o pedido e criado na tela de pedidos do CMSX e chamar o publisher apos salvar.

#### C. Enriquecer o consumer existente

**Arquivo:** `CMSUI/Services/PedidosServiceBusConsumer.cs`

O consumer atual ja funciona — ele recebe status e atualiza. Mas pode ser enriquecido para:

1. **Tratar o campo `evento`** para diferenciar logica por tipo:

```csharp
switch (msg.Evento)
{
    case "pagamento.confirmado":
        // atualizar status + BAIXAR ESTOQUE
        break;
    case "pagamento.recusado":
        // atualizar status, nao baixar estoque
        break;
    case "pagamento.pendente":
        // atualizar status, guardar linkPagamento/pixCopiaCola
        break;
    case "pagamento.timeout":
    case "pagamento.erro":
        // atualizar status, notificar operador
        break;
}
```

2. **Ampliar o modelo PedidoStatusMsg** para incluir os novos campos:

```csharp
private sealed class PedidoStatusMsg
{
    // campos existentes...
    public string?  Evento           { get; set; }
    public int?     PedidoIdSalematic { get; set; }
    public string?  CodigoTransacao  { get; set; }
    public string?  LinkPagamento    { get; set; }
    public string?  PixCopiaCola     { get; set; }
    public string?  CodigoBarras     { get; set; }
    public string?  DataVencimento   { get; set; }
}
```

3. **Ampliar o modelo Pedido** para guardar dados de pagamento:

```
Novos campos sugeridos em CMSUI/Models/Pedido.cs:
    - CodigoTransacao (string?)
    - LinkPagamento (string?)
    - PedidoIdExterno (int?) — ID do pedido no Salematic
```

> Isso exige migration no banco do CMSX.

#### D. Baixa de estoque (IMPLEMENTAR)

Quando o evento `pagamento.confirmado` chegar, o CMSX deve baixar o estoque.
Depende de como o CMSX gerencia estoque (se tem tabela de estoque propria ou se usa outro servico).

---

## 5. Recursos Azure a Criar

| Recurso | Nome | Acao |
|---------|------|------|
| Topico  | top-pedidos | Criar no namespace sb-limpmax-dev |
| Subscription | sub-pedidos-salematic | Criar no topico top-pedidos |

O topico `top-status-pedidos` e a subscription `sub-status-cmsx` ja existem.

---

## 6. Mapa de Status

| Evento | Status no Salematic | Status no CMSX | Acao CMSX |
|--------|--------------------|--------------------|-----------|
| pedido.criado | aguardando_pagamento | entrada | — |
| pagamento.confirmado | confirmado | confirmado | baixar estoque |
| pagamento.recusado | pagamento_recusado | pagamento_recusado | nenhuma |
| pagamento.pendente | aguardando_pagamento | aguardando_pagamento | exibir link |
| pagamento.timeout | erro_timeout | erro_timeout | notificar/retry |
| pagamento.erro | erro | erro | notificar |

---

## 7. Ordem de Implementacao Sugerida

```
Fase 1 — Salematic (publisher)
  1. Criar IEventPublisher + ServiceBusPublisher
  2. Injetar no PedidoService e publicar eventos apos processar
  3. Configurar topico/subscription no appsettings + user-secrets
  4. Testar: chamar webhook do Salematic e verificar se evento chega no Service Bus

Fase 2 — CMSX (publisher)
  1. Criar PedidosServiceBusPublisher
  2. Publicar pedido.criado ao criar pedido na tela
  3. Testar: criar pedido no CMSX e verificar se evento chega no Service Bus

Fase 3 — Salematic (consumer)
  1. Criar PedidoCriadoConsumer (BackgroundService)
  2. Mapear evento para WebhookPedidoRequest e chamar PedidoService
  3. Testar: publicar evento manual no top-pedidos e ver se Salematic processa

Fase 4 — CMSX (enriquecer consumer)
  1. Ampliar PedidoStatusMsg com novos campos
  2. Tratar campo evento com switch
  3. Ampliar modelo Pedido (migration)
  4. Implementar baixa de estoque no caso confirmado

Fase 5 — Teste ponta a ponta
  1. Criar pedido no CMSX
  2. Verificar se Salematic processa e publica retorno
  3. Verificar se CMSX atualiza status e baixa estoque
  4. Testar cenarios de falha (recusado, timeout, erro)
```

---

## 8. Verificacao / Testes

- [ ] Publicar evento manual no `top-pedidos` e verificar se Salematic consome
- [ ] Processar pagamento mock e verificar se evento chega no `top-status-pedidos`
- [ ] CMSX consome evento e atualiza Pedido + Statuspedido
- [ ] Timeline do pedido (`/Pedidos/{id}/timeline`) mostra historico completo
- [ ] Cenarios: confirmado, recusado, pendente, timeout, erro
- [ ] Mock frontend (`?mock=payment`) simula todos os comportamentos
