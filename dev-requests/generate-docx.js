const fs = require("fs");
const {
  Document, Packer, Paragraph, TextRun, Table, TableRow, TableCell,
  Header, Footer, AlignmentType, LevelFormat,
  HeadingLevel, BorderStyle, WidthType, ShadingType,
  PageNumber, PageBreak
} = require("docx");

// ── Colors ──
const BLUE = "1B4F72";
const DARK = "2C3E50";
const LIGHT_BLUE = "D6EAF8";
const LIGHT_GRAY = "F2F3F4";
const WHITE = "FFFFFF";
const GREEN = "27AE60";
const RED = "E74C3C";
const ORANGE = "F39C12";

// ── Borders ──
const thinBorder = { style: BorderStyle.SINGLE, size: 1, color: "BDC3C7" };
const borders = { top: thinBorder, bottom: thinBorder, left: thinBorder, right: thinBorder };
const noBorder = { style: BorderStyle.NONE, size: 0 };
const noBorders = { top: noBorder, bottom: noBorder, left: noBorder, right: noBorder };
const cellMargins = { top: 60, bottom: 60, left: 100, right: 100 };

// ── Helpers ──
function heading1(text) {
  return new Paragraph({ heading: HeadingLevel.HEADING_1, spacing: { before: 360, after: 200 }, children: [new TextRun({ text, bold: true, size: 32, font: "Arial", color: BLUE })] });
}
function heading2(text) {
  return new Paragraph({ heading: HeadingLevel.HEADING_2, spacing: { before: 280, after: 160 }, children: [new TextRun({ text, bold: true, size: 26, font: "Arial", color: DARK })] });
}
function heading3(text) {
  return new Paragraph({ spacing: { before: 200, after: 120 }, children: [new TextRun({ text, bold: true, size: 22, font: "Arial", color: BLUE })] });
}
function para(text, opts = {}) {
  return new Paragraph({ spacing: { after: 120 }, children: [new TextRun({ text, size: 20, font: "Arial", ...opts })] });
}
function boldPara(label, value) {
  return new Paragraph({ spacing: { after: 80 }, children: [
    new TextRun({ text: label, bold: true, size: 20, font: "Arial" }),
    new TextRun({ text: value, size: 20, font: "Arial" })
  ]});
}
function codePara(text) {
  return new Paragraph({
    spacing: { after: 40 },
    shading: { fill: "F7F9FA", type: ShadingType.CLEAR },
    indent: { left: 200, right: 200 },
    children: [new TextRun({ text, size: 18, font: "Consolas", color: "34495E" })]
  });
}
function codeBlock(lines) {
  return lines.map(l => codePara(l));
}
function bulletItem(text, opts = {}) {
  return new Paragraph({
    numbering: { reference: "bullets", level: 0 },
    spacing: { after: 60 },
    children: [new TextRun({ text, size: 20, font: "Arial", ...opts })]
  });
}
function numberItem(text, level = 0) {
  return new Paragraph({
    numbering: { reference: "numbers", level },
    spacing: { after: 60 },
    children: [new TextRun({ text, size: 20, font: "Arial" })]
  });
}
function checkItem(text) {
  return new Paragraph({
    spacing: { after: 60 },
    indent: { left: 360 },
    children: [
      new TextRun({ text: "\u2610 ", size: 22, font: "Arial" }),
      new TextRun({ text, size: 20, font: "Arial" })
    ]
  });
}

function headerCell(text, width) {
  return new TableCell({
    borders, width: { size: width, type: WidthType.DXA },
    shading: { fill: BLUE, type: ShadingType.CLEAR },
    margins: cellMargins,
    children: [new Paragraph({ children: [new TextRun({ text, bold: true, size: 18, font: "Arial", color: WHITE })] })]
  });
}
function cell(text, width, opts = {}) {
  const fill = opts.fill || WHITE;
  return new TableCell({
    borders, width: { size: width, type: WidthType.DXA },
    shading: { fill, type: ShadingType.CLEAR },
    margins: cellMargins,
    children: [new Paragraph({ children: [new TextRun({ text, size: 18, font: "Arial", color: opts.color || "2C3E50", bold: opts.bold || false })] })]
  });
}

function makeTable(headers, rows, colWidths) {
  const totalWidth = colWidths.reduce((a, b) => a + b, 0);
  return new Table({
    width: { size: totalWidth, type: WidthType.DXA },
    columnWidths: colWidths,
    rows: [
      new TableRow({ children: headers.map((h, i) => headerCell(h, colWidths[i])) }),
      ...rows.map((row, ri) => new TableRow({
        children: row.map((c, ci) => cell(c, colWidths[ci], { fill: ri % 2 === 0 ? WHITE : LIGHT_GRAY }))
      }))
    ]
  });
}

// ── Spacer ──
function spacer(pts = 120) {
  return new Paragraph({ spacing: { after: pts }, children: [] });
}

// ── JSON block helper ──
function jsonBlock(obj) {
  const lines = JSON.stringify(obj, null, 2).split("\n");
  return codeBlock(lines);
}

// ══════════════════════════════════════════════
//  BUILD DOCUMENT
// ══════════════════════════════════════════════

const doc = new Document({
  styles: {
    default: { document: { run: { font: "Arial", size: 20 } } },
    paragraphStyles: [
      { id: "Heading1", name: "Heading 1", basedOn: "Normal", next: "Normal", quickFormat: true,
        run: { size: 32, bold: true, font: "Arial", color: BLUE },
        paragraph: { spacing: { before: 360, after: 200 }, outlineLevel: 0 } },
      { id: "Heading2", name: "Heading 2", basedOn: "Normal", next: "Normal", quickFormat: true,
        run: { size: 26, bold: true, font: "Arial", color: DARK },
        paragraph: { spacing: { before: 280, after: 160 }, outlineLevel: 1 } },
    ]
  },
  numbering: {
    config: [
      { reference: "bullets",
        levels: [{ level: 0, format: LevelFormat.BULLET, text: "\u2022", alignment: AlignmentType.LEFT,
          style: { paragraph: { indent: { left: 720, hanging: 360 } } } }] },
      { reference: "numbers",
        levels: [
          { level: 0, format: LevelFormat.DECIMAL, text: "%1.", alignment: AlignmentType.LEFT,
            style: { paragraph: { indent: { left: 720, hanging: 360 } } } },
          { level: 1, format: LevelFormat.DECIMAL, text: "%2.", alignment: AlignmentType.LEFT,
            style: { paragraph: { indent: { left: 1440, hanging: 360 } } } },
        ] },
      { reference: "phases",
        levels: [{ level: 0, format: LevelFormat.DECIMAL, text: "%1.", alignment: AlignmentType.LEFT,
          style: { paragraph: { indent: { left: 720, hanging: 360 } } } }] },
    ]
  },
  sections: [
    // ── COVER PAGE ──
    {
      properties: {
        page: {
          size: { width: 11906, height: 16838 },
          margin: { top: 1440, right: 1440, bottom: 1440, left: 1440 }
        }
      },
      children: [
        spacer(2400),
        new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 200 }, children: [
          new TextRun({ text: "DOCUMENTO FUNCIONAL", size: 24, font: "Arial", color: "7F8C8D", bold: true })
        ]}),
        new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 120 }, children: [
          new TextRun({ text: "SAGA de Pedidos", size: 52, font: "Arial", color: BLUE, bold: true })
        ]}),
        new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 80 }, children: [
          new TextRun({ text: "CMSX (Multiplai)  \u2194  Salematic", size: 32, font: "Arial", color: DARK })
        ]}),
        spacer(600),
        new Paragraph({
          alignment: AlignmentType.CENTER,
          border: { top: { style: BorderStyle.SINGLE, size: 2, color: BLUE, space: 8 } },
          spacing: { before: 200, after: 80 },
          children: []
        }),
        new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 60 }, children: [
          new TextRun({ text: "Padrao: SAGA Coreografado (Choreography-based)", size: 22, font: "Arial", color: "7F8C8D" })
        ]}),
        new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 60 }, children: [
          new TextRun({ text: "Comunicacao: Azure Service Bus (Topics/Subscriptions)", size: 22, font: "Arial", color: "7F8C8D" })
        ]}),
        spacer(1200),
        new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 60 }, children: [
          new TextRun({ text: "Versao 1.0  |  Abril 2026", size: 20, font: "Arial", color: "95A5A6" })
        ]}),
      ]
    },

    // ── MAIN CONTENT ──
    {
      properties: {
        page: {
          size: { width: 11906, height: 16838 },
          margin: { top: 1440, right: 1200, bottom: 1200, left: 1200 }
        }
      },
      headers: {
        default: new Header({ children: [
          new Paragraph({
            border: { bottom: { style: BorderStyle.SINGLE, size: 1, color: "BDC3C7", space: 4 } },
            children: [
              new TextRun({ text: "SAGA CMSX \u2194 Salematic", size: 16, font: "Arial", color: "95A5A6", italics: true }),
            ]
          })
        ]})
      },
      footers: {
        default: new Footer({ children: [
          new Paragraph({
            alignment: AlignmentType.CENTER,
            border: { top: { style: BorderStyle.SINGLE, size: 1, color: "BDC3C7", space: 4 } },
            children: [
              new TextRun({ text: "Pagina ", size: 16, font: "Arial", color: "95A5A6" }),
              new TextRun({ children: [PageNumber.CURRENT], size: 16, font: "Arial", color: "95A5A6" }),
            ]
          })
        ]})
      },
      children: [
        // ═══ 1. VISAO GERAL ═══
        heading1("1. Visao Geral"),
        para("O CMSX (Multiplai) e o Salematic se comunicam via Azure Service Bus usando o padrao SAGA coreografado. Cada servico reage a eventos publicados pelo outro, sem orquestrador central."),
        spacer(80),

        // Diagrama ASCII
        ...codeBlock([
          "CMSX                        Service Bus                   Salematic",
          "  |                               |                           |",
          "  |-- pedido.criado ------------->|                           |",
          "  |                               |------------------------->|",
          "  |                               |              processa pagamento",
          "  |                               |<-- pagamento.confirmado -|",
          "  |<------------------------------|                           |",
          "  | atualiza status, baixa estoque|                           |",
        ]),
        spacer(120),

        heading2("1.1 Topologia Service Bus"),
        makeTable(
          ["Recurso", "Nome", "Quem usa"],
          [
            ["Namespace", "sb-limpmax-dev", "Ambos"],
            ["Topico (ida)", "top-pedidos", "CMSX publica, Salematic consome"],
            ["Subscription", "sub-pedidos-salematic", "Salematic"],
            ["Topico (volta)", "top-status-pedidos", "Salematic publica, CMSX consome"],
            ["Subscription", "sub-status-cmsx", "CMSX (ja existe)"],
          ],
          [2400, 3200, 3900]
        ),
        spacer(80),
        para("O topico top-status-pedidos e a subscription sub-status-cmsx ja existem. O topico top-pedidos e a subscription sub-pedidos-salematic precisam ser criados.", { italics: true, color: "7F8C8D" }),

        // ═══ 2. CONTRATOS DE EVENTO ═══
        new Paragraph({ children: [new PageBreak()] }),
        heading1("2. Contratos de Evento"),

        // 2.1
        heading2("2.1 pedido.criado (CMSX \u2192 Salematic)"),
        para("Publicado pelo CMSX quando um pedido e registrado pela tela de pedidos."),
        spacer(40),
        ...jsonBlock({
          evento: "pedido.criado",
          aplicacaoid: "guid-do-tenant",
          numeropedido: "PED-2026-0042",
          clienteId: 1,
          clienteNome: "Joao Silva",
          clienteDocumento: "12345678901",
          clienteEmail: "joao@email.com",
          clienteTelefone: "11999998888",
          metodoPagamento: "PIX",
          itens: [{ produtoId: 5, quantidade: 3 }, { produtoId: 12, quantidade: 1 }],
          timestamp: "2026-04-08T14:30:00Z"
        }),
        spacer(80),
        boldPara("Campos obrigatorios: ", "aplicacaoid, numeropedido, clienteId, itens (min 1), metodoPagamento"),
        boldPara("Campos opcionais: ", "clienteEmail, clienteTelefone"),
        boldPara("metodoPagamento: ", "PIX | BOLETO | CARTAO"),

        // 2.2
        spacer(120),
        heading2("2.2 pagamento.confirmado (Salematic \u2192 CMSX)"),
        para("Publicado quando o pagamento e aprovado pelo gateway (ou mock)."),
        spacer(40),
        ...jsonBlock({
          evento: "pagamento.confirmado",
          aplicacaoid: "guid-do-tenant",
          numeropedido: "PED-2026-0042",
          pedidoIdSalematic: 47,
          clienteNome: "Joao Silva",
          clienteEmail: "joao@email.com",
          valorPedido: 150.00,
          codigoTransacao: "txn_a1b2c3d4",
          status: "confirmado",
          descricao: "Pagamento aprovado via PIX",
          timestamp: "2026-04-08T14:30:05Z"
        }),

        // 2.3
        spacer(120),
        heading2("2.3 pagamento.recusado (Salematic \u2192 CMSX)"),
        para("Publicado quando o gateway recusa o pagamento."),
        spacer(40),
        ...jsonBlock({
          evento: "pagamento.recusado",
          aplicacaoid: "guid-do-tenant",
          numeropedido: "PED-2026-0042",
          pedidoIdSalematic: 47,
          clienteNome: "Joao Silva",
          clienteEmail: "joao@email.com",
          valorPedido: 150.00,
          codigoTransacao: null,
          status: "pagamento_recusado",
          descricao: "Cartao recusado pelo banco emissor",
          timestamp: "2026-04-08T14:30:04Z"
        }),
        spacer(40),
        boldPara("Acao esperada no CMSX: ", "atualizar status para pagamento_recusado. NAO baixar estoque."),

        // 2.4
        new Paragraph({ children: [new PageBreak()] }),
        heading2("2.4 pagamento.pendente (Salematic \u2192 CMSX)"),
        para("Para metodos assincronos (boleto, PIX com prazo)."),
        spacer(40),
        ...jsonBlock({
          evento: "pagamento.pendente",
          aplicacaoid: "guid-do-tenant",
          numeropedido: "PED-2026-0042",
          pedidoIdSalematic: 47,
          clienteNome: "Joao Silva",
          clienteEmail: "joao@email.com",
          valorPedido: 120.00,
          codigoTransacao: "txn_e5f6g7h8",
          linkPagamento: "https://gateway.com/pay/txn_e5f6g7h8",
          pixCopiaCola: "00020126...",
          codigoBarras: "34191.09008...",
          dataVencimento: "2026-04-11",
          status: "aguardando_pagamento",
          descricao: "Boleto gerado - vencimento em 3 dias uteis",
          timestamp: "2026-04-08T14:30:03Z"
        }),
        spacer(40),
        boldPara("Acao esperada no CMSX: ", "atualizar status para aguardando_pagamento. Exibir link/codigo para o cliente. Aguardar evento definitivo."),

        // 2.5
        spacer(120),
        heading2("2.5 pagamento.timeout (Salematic \u2192 CMSX)"),
        para("Gateway nao respondeu no tempo esperado."),
        spacer(40),
        ...jsonBlock({
          evento: "pagamento.timeout",
          aplicacaoid: "guid-do-tenant",
          numeropedido: "PED-2026-0042",
          pedidoIdSalematic: 47,
          valorPedido: 150.00,
          status: "erro_timeout",
          descricao: "Gateway nao respondeu em 30s",
          timestamp: "2026-04-08T14:30:35Z"
        }),
        spacer(40),
        boldPara("Acao esperada no CMSX: ", "atualizar status para erro_timeout. Pode agendar retry ou notificar operador."),

        // 2.6
        spacer(120),
        heading2("2.6 pagamento.erro (Salematic \u2192 CMSX)"),
        para("Erro interno no processamento (excecao, gateway fora, etc)."),
        spacer(40),
        ...jsonBlock({
          evento: "pagamento.erro",
          aplicacaoid: "guid-do-tenant",
          numeropedido: "PED-2026-0042",
          pedidoIdSalematic: 47,
          valorPedido: 150.00,
          status: "erro",
          descricao: "Erro interno: gateway indisponivel",
          timestamp: "2026-04-08T14:30:04Z"
        }),
        spacer(40),
        boldPara("Acao esperada no CMSX: ", "atualizar status para erro. Notificar operador. NAO baixar estoque."),

        // ═══ 3. FLUXOS ═══
        new Paragraph({ children: [new PageBreak()] }),
        heading1("3. Fluxos"),

        heading2("3.1 Happy Path (PIX/Cartao aprovado)"),
        ...codeBlock([
          " 1. CMSX cria pedido na tela de pedidos",
          " 2. CMSX publica \"pedido.criado\" no topico top-pedidos",
          " 3. Salematic consome o evento",
          " 4. Salematic valida cliente, produtos e estoque",
          " 5. Salematic cria o pedido local (status: aguardando_pagamento)",
          " 6. Salematic chama IPagamentoService.ProcessarAsync()",
          " 7. Gateway retorna aprovado",
          " 8. Salematic atualiza pedido local (status: confirmado)",
          " 9. Salematic publica \"pagamento.confirmado\" no top-status-pedidos",
          "10. CMSX consome o evento",
          "11. CMSX atualiza Pedido.Statusatual = \"confirmado\"",
          "12. CMSX registra historico em Statuspedido",
          "13. CMSX baixa estoque (IMPLEMENTAR)",
        ]),

        spacer(120),
        heading2("3.2 Pagamento Recusado"),
        ...codeBlock([
          "1-6. Igual ao happy path",
          " 7.  Gateway retorna recusado",
          " 8.  Salematic atualiza pedido local (status: pagamento_recusado)",
          " 9.  Salematic publica \"pagamento.recusado\" no top-status-pedidos",
          "10.  CMSX consome o evento",
          "11.  CMSX atualiza Pedido.Statusatual = \"pagamento_recusado\"",
          "12.  CMSX registra historico em Statuspedido",
          "13.  CMSX NAO baixa estoque",
        ]),

        spacer(120),
        heading2("3.3 Boleto/PIX Pendente"),
        ...codeBlock([
          "1-6.  Igual ao happy path",
          " 7.   Gateway retorna pendente (boleto gerado)",
          " 8.   Salematic atualiza pedido local (status: aguardando_pagamento)",
          " 9.   Salematic publica \"pagamento.pendente\" no top-status-pedidos",
          "10.   CMSX consome, atualiza status, guarda link/codigo",
          "11.   (aguarda webhook do gateway confirmando pagamento)",
          "12.   Quando confirmado: Salematic publica \"pagamento.confirmado\"",
          "13.   CMSX finaliza o fluxo como happy path",
        ]),

        spacer(120),
        heading2("3.4 Validacao Falhou no Salematic"),
        ...codeBlock([
          "1-3. Igual ao happy path",
          " 4.  Salematic detecta erro (cliente nao encontrado, estoque insuficiente)",
          " 5.  Salematic publica \"pagamento.erro\" com descricao do motivo",
          " 6.  CMSX consome, atualiza status para \"erro\"",
        ]),

        // ═══ 4. IMPLEMENTACAO POR SERVICO ═══
        new Paragraph({ children: [new PageBreak()] }),
        heading1("4. Implementacao por Servico"),

        heading2("4.1 Salematic"),

        heading3("A. Publisher de eventos (NOVO)"),
        boldPara("Criar: ", "Salematic.Infrastructure/ServiceBus/ServiceBusPublisher.cs"),
        spacer(40),
        ...codeBlock([
          "Interface: IEventPublisher (em Salematic.Domain/Interfaces/)",
          "    Task PublicarAsync(string evento, object payload)",
          "",
          "Implementacao:",
          "    - Recebe connection string e nome do topico via IConfiguration",
          "    - Serializa payload para JSON",
          "    - Envia via ServiceBusSender",
          "    - Topico destino: top-status-pedidos",
        ]),
        spacer(40),
        boldPara("Dependencia NuGet: ", "Azure.Messaging.ServiceBus"),

        spacer(120),
        heading3("B. Consumer de pedido.criado (NOVO)"),
        boldPara("Criar: ", "Salematic.Infrastructure/ServiceBus/PedidoCriadoConsumer.cs"),
        spacer(40),
        ...codeBlock([
          "BackgroundService que escuta:",
          "    - Topico: top-pedidos",
          "    - Subscription: sub-pedidos-salematic",
          "",
          "Ao receber mensagem:",
          "    1. Deserializar para PedidoCriadoEvent",
          "    2. Mapear para WebhookPedidoRequest",
          "    3. Chamar PedidoService.ProcessarAsync()",
          "    4. Com base no resultado, publicar evento via IEventPublisher",
          "    5. Incluir aplicacaoid e numeropedido no evento de retorno",
        ]),

        spacer(120),
        heading3("C. Alterar PedidoService.ProcessarAsync()"),
        boldPara("Arquivo: ", "Salematic.Application/Services/PedidoService.cs"),
        para("Atualmente o metodo retorna WebhookPedidoResponse com sucesso/falha. Precisa:"),
        numberItem("Receber (ou propagar) aplicacaoid e numeropedido para incluir nos eventos de retorno"),
        numberItem("Publicar evento via IEventPublisher apos processar pagamento"),
        para("Opcao limpa: injetar IEventPublisher no PedidoService e publicar direto no final do ProcessarAsync().", { italics: true }),

        spacer(120),
        heading3("D. Configuracao"),
        boldPara("Arquivo: ", "Salematic.API/appsettings.json"),
        spacer(40),
        ...codeBlock([
          '"ServiceBus": {',
          '  "ConnectionString": "(via user-secrets)",',
          '  "TopicoConsumo": "top-pedidos",',
          '  "SubscriptionConsumo": "sub-pedidos-salematic",',
          '  "TopicoPublicacao": "top-status-pedidos"',
          '}',
        ]),
        spacer(40),
        para("Connection string via dotnet user-secrets - NAO colocar no appsettings.json.", { bold: true, color: RED }),

        // 4.2 CMSX
        new Paragraph({ children: [new PageBreak()] }),
        heading2("4.2 CMSX"),

        heading3("A. Publisher de pedido.criado (NOVO)"),
        boldPara("Criar: ", "CMSUI/Services/PedidosServiceBusPublisher.cs"),
        spacer(40),
        ...codeBlock([
          "Classe com metodo:",
          "    Task PublicarPedidoCriadoAsync(PedidoCriadoMsg msg)",
          "",
          "Usa ServiceBusSender para publicar no topico: top-pedidos",
          "Registrar como Singleton no DI",
        ]),

        spacer(120),
        heading3("B. Chamar o publisher na criacao de pedido"),
        para("Identificar onde o pedido e criado na tela de pedidos do CMSX e chamar o publisher apos salvar."),

        spacer(120),
        heading3("C. Enriquecer o consumer existente"),
        boldPara("Arquivo: ", "CMSUI/Services/PedidosServiceBusConsumer.cs"),
        para("O consumer atual ja funciona. Enriquecer para tratar o campo evento:"),
        spacer(40),
        ...codeBlock([
          'switch (msg.Evento)',
          '{',
          '    case "pagamento.confirmado":',
          '        // atualizar status + BAIXAR ESTOQUE',
          '        break;',
          '    case "pagamento.recusado":',
          '        // atualizar status, nao baixar estoque',
          '        break;',
          '    case "pagamento.pendente":',
          '        // atualizar status, guardar linkPagamento/pixCopiaCola',
          '        break;',
          '    case "pagamento.timeout":',
          '    case "pagamento.erro":',
          '        // atualizar status, notificar operador',
          '        break;',
          '}',
        ]),
        spacer(80),
        para("Ampliar o modelo PedidoStatusMsg com novos campos:", { bold: true }),
        spacer(40),
        ...codeBlock([
          "public string?  Evento           { get; set; }",
          "public int?     PedidoIdSalematic { get; set; }",
          "public string?  CodigoTransacao  { get; set; }",
          "public string?  LinkPagamento    { get; set; }",
          "public string?  PixCopiaCola     { get; set; }",
          "public string?  CodigoBarras     { get; set; }",
          "public string?  DataVencimento   { get; set; }",
        ]),
        spacer(80),
        para("Ampliar o modelo Pedido (CMSUI/Models/Pedido.cs) — novos campos:", { bold: true }),
        bulletItem("CodigoTransacao (string?)"),
        bulletItem("LinkPagamento (string?)"),
        bulletItem("PedidoIdExterno (int?) - ID do pedido no Salematic"),
        para("Isso exige migration no banco do CMSX.", { italics: true, color: "7F8C8D" }),

        spacer(120),
        heading3("D. Baixa de estoque (IMPLEMENTAR)"),
        para("Quando o evento pagamento.confirmado chegar, o CMSX deve baixar o estoque. Depende de como o CMSX gerencia estoque (tabela propria ou outro servico)."),

        // ═══ 5. RECURSOS AZURE ═══
        new Paragraph({ children: [new PageBreak()] }),
        heading1("5. Recursos Azure a Criar"),
        makeTable(
          ["Recurso", "Nome", "Acao"],
          [
            ["Topico", "top-pedidos", "Criar no namespace sb-limpmax-dev"],
            ["Subscription", "sub-pedidos-salematic", "Criar no topico top-pedidos"],
          ],
          [2400, 3400, 3700]
        ),
        spacer(80),
        para("O topico top-status-pedidos e a subscription sub-status-cmsx ja existem.", { italics: true, color: "7F8C8D" }),

        // ═══ 6. MAPA DE STATUS ═══
        spacer(200),
        heading1("6. Mapa de Status"),
        makeTable(
          ["Evento", "Status Salematic", "Status CMSX", "Acao CMSX"],
          [
            ["pedido.criado", "aguardando_pagamento", "entrada", "\u2014"],
            ["pagamento.confirmado", "confirmado", "confirmado", "baixar estoque"],
            ["pagamento.recusado", "pagamento_recusado", "pagamento_recusado", "nenhuma"],
            ["pagamento.pendente", "aguardando_pagamento", "aguardando_pagamento", "exibir link"],
            ["pagamento.timeout", "erro_timeout", "erro_timeout", "notificar/retry"],
            ["pagamento.erro", "erro", "erro", "notificar"],
          ],
          [2400, 2400, 2400, 2300]
        ),

        // ═══ 7. ORDEM DE IMPLEMENTACAO ═══
        new Paragraph({ children: [new PageBreak()] }),
        heading1("7. Ordem de Implementacao Sugerida"),

        heading2("Fase 1 - Salematic (publisher)"),
        numberItem("Criar IEventPublisher + ServiceBusPublisher"),
        numberItem("Injetar no PedidoService e publicar eventos apos processar"),
        numberItem("Configurar topico/subscription no appsettings + user-secrets"),
        numberItem("Testar: chamar webhook do Salematic e verificar se evento chega no Service Bus"),

        spacer(120),
        heading2("Fase 2 - CMSX (publisher)"),
        numberItem("Criar PedidosServiceBusPublisher"),
        numberItem("Publicar pedido.criado ao criar pedido na tela"),
        numberItem("Testar: criar pedido no CMSX e verificar se evento chega no Service Bus"),

        spacer(120),
        heading2("Fase 3 - Salematic (consumer)"),
        numberItem("Criar PedidoCriadoConsumer (BackgroundService)"),
        numberItem("Mapear evento para WebhookPedidoRequest e chamar PedidoService"),
        numberItem("Testar: publicar evento manual no top-pedidos e ver se Salematic processa"),

        spacer(120),
        heading2("Fase 4 - CMSX (enriquecer consumer)"),
        numberItem("Ampliar PedidoStatusMsg com novos campos"),
        numberItem("Tratar campo evento com switch"),
        numberItem("Ampliar modelo Pedido (migration)"),
        numberItem("Implementar baixa de estoque no caso confirmado"),

        spacer(120),
        heading2("Fase 5 - Teste ponta a ponta"),
        numberItem("Criar pedido no CMSX"),
        numberItem("Verificar se Salematic processa e publica retorno"),
        numberItem("Verificar se CMSX atualiza status e baixa estoque"),
        numberItem("Testar cenarios de falha (recusado, timeout, erro)"),

        // ═══ 8. VERIFICACAO / TESTES ═══
        new Paragraph({ children: [new PageBreak()] }),
        heading1("8. Verificacao / Testes"),
        para("Checklist para validacao da integracao completa:"),
        spacer(80),
        checkItem("Publicar evento manual no top-pedidos e verificar se Salematic consome"),
        checkItem("Processar pagamento mock e verificar se evento chega no top-status-pedidos"),
        checkItem("CMSX consome evento e atualiza Pedido + Statuspedido"),
        checkItem("Timeline do pedido (/Pedidos/{id}/timeline) mostra historico completo"),
        checkItem("Cenario: pagamento.confirmado - status atualizado + estoque baixado"),
        checkItem("Cenario: pagamento.recusado - status atualizado, estoque intacto"),
        checkItem("Cenario: pagamento.pendente - link/codigo exibido ao cliente"),
        checkItem("Cenario: pagamento.timeout - status erro_timeout registrado"),
        checkItem("Cenario: pagamento.erro - status erro registrado, operador notificado"),
        checkItem("Mock frontend (?mock=payment) simula todos os comportamentos"),
      ]
    }
  ]
});

Packer.toBuffer(doc).then(buffer => {
  fs.writeFileSync("T:/Developer/Salematic/dev-requests/DOC-SAGA-integracao-cmsx-salematic.docx", buffer);
  console.log("OK: DOC-SAGA-integracao-cmsx-salematic.docx gerado");
});
