import { useState, useEffect } from "react";

const initialMethods = [
  {
    id: "pix",
    name: "Pix",
    type: "pix",
    icon: "⚡",
    enabled: true,
    behavior: "approved",
    rejectionReason: "",
    delay: 1200,
    description: "Pagamento instantâneo via chave Pix",
    mockKey: "salematica@pix.com.br",
    color: "#00BDAE",
  },
  {
    id: "credit_visa",
    name: "Cartão Visa",
    type: "credit",
    icon: "💳",
    enabled: true,
    behavior: "approved",
    rejectionReason: "",
    delay: 2000,
    description: "Crédito Visa — aprovação imediata simulada",
    mockNumber: "4111 1111 1111 1111",
    color: "#1A1F71",
  },
  {
    id: "credit_master",
    name: "Cartão Mastercard",
    type: "credit",
    icon: "💳",
    enabled: true,
    behavior: "approved",
    rejectionReason: "",
    delay: 2000,
    description: "Crédito Mastercard — aprovação imediata simulada",
    mockNumber: "5500 0000 0000 0004",
    color: "#EB001B",
  },
  {
    id: "boleto",
    name: "Boleto Bancário",
    type: "boleto",
    icon: "🧾",
    enabled: true,
    behavior: "pending",
    rejectionReason: "",
    delay: 800,
    description: "Boleto com vencimento em 3 dias úteis",
    mockBarcode: "34191.09008 63521.560315 71444.160005 6 92690000012000",
    color: "#F59E0B",
  },
  {
    id: "debit",
    name: "Débito em Conta",
    type: "debit",
    icon: "🏦",
    enabled: false,
    behavior: "rejected",
    rejectionReason: "Saldo insuficiente simulado para testes de rejeição",
    delay: 1500,
    description: "Débito automático em conta corrente",
    color: "#6366F1",
  },
];

const behaviorOptions = [
  { value: "approved", label: "✅ Aprovado", color: "#22C55E" },
  { value: "rejected", label: "❌ Rejeitado", color: "#EF4444" },
  { value: "pending", label: "⏳ Pendente", color: "#F59E0B" },
  { value: "timeout", label: "⏱ Timeout", color: "#8B5CF6" },
  { value: "error", label: "💥 Erro gateway", color: "#EC4899" },
];

const rejectionReasons = [
  "Saldo insuficiente",
  "Cartão bloqueado pelo banco",
  "Limite de crédito excedido",
  "Cartão expirado",
  "CVV inválido",
  "Suspeita de fraude",
  "Transação não autorizada pelo titular",
  "Banco emissor indisponível",
  "Chave Pix não encontrada",
  "Timeout na autenticação",
];

const mockEvents = [];

function generateEventId() {
  return "evt_" + Math.random().toString(36).substr(2, 9);
}

function simulatePayment(method, amount) {
  return new Promise((resolve) => {
    setTimeout(() => {
      const event = {
        id: generateEventId(),
        methodId: method.id,
        methodName: method.name,
        amount,
        timestamp: new Date().toISOString(),
        behavior: method.behavior,
        reason: method.rejectionReason || null,
      };

      if (method.behavior === "timeout") {
        event.status = "timeout";
        event.serviceBusEvent = "pagamento.timeout";
      } else if (method.behavior === "error") {
        event.status = "error";
        event.serviceBusEvent = "pagamento.erro";
      } else if (method.behavior === "rejected") {
        event.status = "rejected";
        event.serviceBusEvent = "pagamento.recusado";
      } else if (method.behavior === "pending") {
        event.status = "pending";
        event.serviceBusEvent = "pagamento.pendente";
      } else {
        event.status = "approved";
        event.serviceBusEvent = "pagamento.confirmado";
      }

      resolve(event);
    }, method.delay);
  });
}

export default function SalematicaMock() {
  const [methods, setMethods] = useState(initialMethods);
  const [selected, setSelected] = useState(null);
  const [events, setEvents] = useState([]);
  const [simulating, setSimulating] = useState(null);
  const [simAmount, setSimAmount] = useState("150.00");
  const [tab, setTab] = useState("methods");
  const [editingReason, setEditingReason] = useState("");

  useEffect(() => {
    if (selected) {
      const m = methods.find((m) => m.id === selected.id);
      if (m) setSelected(m);
    }
  }, [methods]);

  useEffect(() => {
    fetch('api/mock/pagamento')
    .then(r=>r.json())
    .then(configs =>{
      // para cada method no state, procura o config correspondente pelo id
      // se achar, faz merge: { ...method, ...config }
      // se não achar, mantém o method como está
      setMethods(prev => prev.map(
        m => {
          const cfg = configs.find(c => c.id === m.id);
          return cfg ? {...m,...cfg}:m;
        }
      ))
    })
    .catch(()=>{}) //
  },[]);

  function updateMethod(id, patch) {
    const atualizado = {...methods.find(m => m.id === id), ...patch};//cria um objeto novo substituindo o que foi alterado no caso patch

    setMethods(prev => prev.map(m => m.id === id ? atualizado : m)); //atualiza o objeto com os novos dados

    fetch(`/api/mock/pagamento/${id}`, { //enviando para a API .Net para atualizar o objeto
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(atualizado),
    }).catch(() => {}); 
  }

  function toggleMethod(id) {
    const m = methods.find(m => m.id === id);
    updateMethod(id,{enabled: !m.enabled});
  }

  async function runSimulation(method) {
    if (simulating) return;
    setSimulating(method.id);
    const amt = parseFloat(simAmount) || 150;
    const event = await simulatePayment(method, amt);
    setEvents((prev) => [event, ...prev].slice(0, 50));
    setSimulating(null);
  }

  const statusColor = {
    approved: "#22C55E",
    rejected: "#EF4444",
    pending: "#F59E0B",
    timeout: "#8B5CF6",
    error: "#EC4899",
  };

  const statusLabel = {
    approved: "Aprovado",
    rejected: "Rejeitado",
    pending: "Pendente",
    timeout: "Timeout",
    error: "Erro",
  };

  return (
    <div style={styles.root}>
      {/* Header */}
      <div style={styles.header}>
        <div>
          <div style={styles.logoRow}>
            <span style={styles.logoIcon}>⚙</span>
            <span style={styles.logoText}>Salematica</span>
            <span style={styles.logoBadge}>Mock Engine</span>
          </div>
          <div style={styles.headerSub}>Simulador de Meios de Pagamento</div>
        </div>
        <div style={styles.headerStats}>
          <div style={styles.stat}>
            <span style={styles.statNum}>{methods.filter((m) => m.enabled).length}</span>
            <span style={styles.statLabel}>ativos</span>
          </div>
          <div style={styles.stat}>
            <span style={styles.statNum}>{events.length}</span>
            <span style={styles.statLabel}>eventos</span>
          </div>
          <div style={{ ...styles.stat, color: "#22C55E" }}>
            <span style={styles.statNum}>
              {events.filter((e) => e.status === "approved").length}
            </span>
            <span style={styles.statLabel}>aprovados</span>
          </div>
        </div>
      </div>

      {/* Tabs */}
      <div style={styles.tabs}>
        {["methods", "events", "simulate"].map((t) => (
          <button
            key={t}
            onClick={() => setTab(t)}
            style={{ ...styles.tab, ...(tab === t ? styles.tabActive : {}) }}
          >
            {t === "methods" && "💳 Meios"}
            {t === "simulate" && "🧪 Simular"}
            {t === "events" && `📨 Eventos ${events.length > 0 ? `(${events.length})` : ""}`}
          </button>
        ))}
      </div>

      <div style={styles.body}>
        {/* === TAB: METHODS === */}
        {tab === "methods" && (
          <div style={styles.twocol}>
            {/* Lista */}
            <div style={styles.list}>
              {methods.map((m) => (
                <div
                  key={m.id}
                  onClick={() => { setSelected(m); setEditingReason(m.rejectionReason); }}
                  style={{
                    ...styles.methodCard,
                    ...(selected?.id === m.id ? styles.methodCardActive : {}),
                    opacity: m.enabled ? 1 : 0.5,
                  }}
                >
                  <div style={styles.methodLeft}>
                    <div style={{ ...styles.methodDot, background: m.color }} />
                    <span style={styles.methodIcon}>{m.icon}</span>
                    <div>
                      <div style={styles.methodName}>{m.name}</div>
                      <div style={styles.methodType}>{m.type}</div>
                    </div>
                  </div>
                  <div style={styles.methodRight}>
                    <span
                      style={{
                        ...styles.behaviorBadge,
                        background: behaviorOptions.find((b) => b.value === m.behavior)?.color + "22",
                        color: behaviorOptions.find((b) => b.value === m.behavior)?.color,
                      }}
                    >
                      {behaviorOptions.find((b) => b.value === m.behavior)?.label}
                    </span>
                    <button
                      onClick={(e) => { e.stopPropagation(); toggleMethod(m.id); }}
                      style={{ ...styles.toggle, background: m.enabled ? "#22C55E" : "#374151" }}
                    >
                      {m.enabled ? "ON" : "OFF"}
                    </button>
                  </div>
                </div>
              ))}
            </div>

            {/* Detalhe */}
            {selected ? (
              <div style={styles.detail}>
                <div style={styles.detailHeader}>
                  <span style={{ fontSize: 28 }}>{selected.icon}</span>
                  <div>
                    <div style={styles.detailTitle}>{selected.name}</div>
                    <div style={styles.detailDesc}>{selected.description}</div>
                  </div>
                </div>

                {/* Mock data */}
                <div style={styles.section}>
                  <div style={styles.sectionTitle}>Dados Mock</div>
                  {selected.mockNumber && (
                    <div style={styles.mockData}>
                      <span style={styles.mockLabel}>Número</span>
                      <span style={styles.mockValue}>{selected.mockNumber}</span>
                    </div>
                  )}
                  {selected.mockKey && (
                    <div style={styles.mockData}>
                      <span style={styles.mockLabel}>Chave Pix</span>
                      <span style={styles.mockValue}>{selected.mockKey}</span>
                    </div>
                  )}
                  {selected.mockBarcode && (
                    <div style={styles.mockData}>
                      <span style={styles.mockLabel}>Código</span>
                      <span style={{ ...styles.mockValue, fontSize: 10, letterSpacing: 0.5 }}>
                        {selected.mockBarcode}
                      </span>
                    </div>
                  )}
                </div>

                {/* Comportamento */}
                <div style={styles.section}>
                  <div style={styles.sectionTitle}>Comportamento</div>
                  <div style={styles.behaviorGrid}>
                    {behaviorOptions.map((b) => (
                      <button
                        key={b.value}
                        onClick={() => updateMethod(selected.id, { behavior: b.value })}
                        style={{
                          ...styles.behaviorBtn,
                          background:
                            selected.behavior === b.value
                              ? b.color + "33"
                              : "transparent",
                          borderColor:
                            selected.behavior === b.value ? b.color : "#374151",
                          color:
                            selected.behavior === b.value ? b.color : "#9CA3AF",
                        }}
                      >
                        {b.label}
                      </button>
                    ))}
                  </div>
                </div>

                {/* Motivo de rejeição */}
                {(selected.behavior === "rejected" || selected.behavior === "error") && (
                  <div style={styles.section}>
                    <div style={styles.sectionTitle}>Motivo da Rejeição</div>
                    <div style={styles.reasonGrid}>
                      {rejectionReasons.map((r) => (
                        <button
                          key={r}
                          onClick={() => {
                            setEditingReason(r);
                            updateMethod(selected.id, { rejectionReason: r });
                          }}
                          style={{
                            ...styles.reasonBtn,
                            background:
                              selected.rejectionReason === r
                                ? "#EF444422"
                                : "transparent",
                            borderColor:
                              selected.rejectionReason === r
                                ? "#EF4444"
                                : "#374151",
                            color:
                              selected.rejectionReason === r
                                ? "#EF4444"
                                : "#9CA3AF",
                          }}
                        >
                          {r}
                        </button>
                      ))}
                    </div>
                    <input
                      value={editingReason}
                      onChange={(e) => {
                        setEditingReason(e.target.value);
                        updateMethod(selected.id, { rejectionReason: e.target.value });
                      }}
                      placeholder="Ou escreva um motivo personalizado..."
                      style={styles.input}
                    />
                  </div>
                )}

                {/* Delay */}
                <div style={styles.section}>
                  <div style={styles.sectionTitle}>
                    Delay de resposta: <span style={{ color: "#60A5FA" }}>{selected.delay}ms</span>
                  </div>
                  <input
                    type="range"
                    min={200}
                    max={8000}
                    step={100}
                    value={selected.delay}
                    onChange={(e) =>
                      updateMethod(selected.id, { delay: parseInt(e.target.value) })
                    }
                    style={styles.range}
                  />
                  <div style={styles.rangeLabels}>
                    <span>200ms</span><span>8s</span>
                  </div>
                </div>

                <button
                  onClick={() => runSimulation(selected)}
                  disabled={!selected.enabled || !!simulating}
                  style={{
                    ...styles.simBtn,
                    opacity: !selected.enabled || !!simulating ? 0.4 : 1,
                  }}
                >
                  {simulating === selected.id ? "⏳ Processando..." : `🧪 Testar R$ ${simAmount}`}
                </button>
              </div>
            ) : (
              <div style={styles.emptyDetail}>
                <span style={{ fontSize: 40 }}>💳</span>
                <div>Selecione um meio de pagamento para configurar</div>
              </div>
            )}
          </div>
        )}

        {/* === TAB: SIMULATE === */}
        {tab === "simulate" && (
          <div style={styles.simulateTab}>
            <div style={styles.section}>
              <div style={styles.sectionTitle}>Valor da transação</div>
              <div style={styles.amountRow}>
                <span style={styles.amountPrefix}>R$</span>
                <input
                  value={simAmount}
                  onChange={(e) => setSimAmount(e.target.value)}
                  style={styles.amountInput}
                  placeholder="0,00"
                />
              </div>
            </div>

            <div style={styles.sectionTitle}>Disparar simulação</div>
            <div style={styles.simGrid}>
              {methods.map((m) => (
                <button
                  key={m.id}
                  onClick={() => runSimulation(m)}
                  disabled={!m.enabled || !!simulating}
                  style={{
                    ...styles.simCard,
                    borderColor: m.color + "55",
                    opacity: !m.enabled || !!simulating ? 0.35 : 1,
                  }}
                >
                  <div style={{ ...styles.simCardDot, background: m.color }} />
                  <span style={{ fontSize: 24 }}>{m.icon}</span>
                  <div style={styles.simCardName}>{m.name}</div>
                  <div
                    style={{
                      ...styles.simCardBehavior,
                      color: behaviorOptions.find((b) => b.value === m.behavior)?.color,
                    }}
                  >
                    {simulating === m.id
                      ? "⏳ processando..."
                      : behaviorOptions.find((b) => b.value === m.behavior)?.label}
                  </div>
                  {!m.enabled && (
                    <div style={styles.disabledOverlay}>DESABILITADO</div>
                  )}
                </button>
              ))}
            </div>
          </div>
        )}

        {/* === TAB: EVENTS === */}
        {tab === "events" && (
          <div style={styles.eventsTab}>
            {events.length === 0 ? (
              <div style={styles.emptyDetail}>
                <span style={{ fontSize: 40 }}>📨</span>
                <div>Nenhum evento ainda. Execute uma simulação.</div>
              </div>
            ) : (
              <>
                <div style={styles.eventHeader}>
                  <span style={styles.sectionTitle}>Log de Eventos Service Bus</span>
                  <button onClick={() => setEvents([])} style={styles.clearBtn}>
                    🗑 Limpar
                  </button>
                </div>
                <div style={styles.eventList}>
                  {events.map((e) => (
                    <div key={e.id} style={styles.eventCard}>
                      <div style={styles.eventTop}>
                        <div style={styles.eventLeft}>
                          <span
                            style={{
                              ...styles.eventStatus,
                              background: statusColor[e.status] + "22",
                              color: statusColor[e.status],
                            }}
                          >
                            {statusLabel[e.status]}
                          </span>
                          <span style={styles.eventMethod}>{e.methodName}</span>
                        </div>
                        <div style={styles.eventRight}>
                          <span style={styles.eventAmount}>
                            R$ {e.amount.toFixed(2)}
                          </span>
                          <span style={styles.eventTime}>
                            {new Date(e.timestamp).toLocaleTimeString("pt-BR")}
                          </span>
                        </div>
                      </div>
                      <div style={styles.eventBus}>
                        <span style={styles.eventBusLabel}>ServiceBus →</span>
                        <code style={styles.eventBusEvent}>{e.serviceBusEvent}</code>
                      </div>
                      {e.reason && (
                        <div style={styles.eventReason}>⚠ {e.reason}</div>
                      )}
                      <div style={styles.eventId}>{e.id}</div>
                    </div>
                  ))}
                </div>
              </>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

const styles = {
  root: {
    background: "#0A0F1A",
    minHeight: "100vh",
    color: "#E5E7EB",
    fontFamily: "'DM Mono', 'Fira Code', 'Courier New', monospace",
    display: "flex",
    flexDirection: "column",
  },
  header: {
    padding: "20px 24px 16px",
    borderBottom: "1px solid #1F2937",
    display: "flex",
    justifyContent: "space-between",
    alignItems: "flex-start",
    background: "linear-gradient(180deg, #0D1525 0%, #0A0F1A 100%)",
  },
  logoRow: {
    display: "flex",
    alignItems: "center",
    gap: 10,
    marginBottom: 4,
  },
  logoIcon: { fontSize: 20, color: "#60A5FA" },
  logoText: {
    fontSize: 20,
    fontWeight: 700,
    letterSpacing: 2,
    color: "#F3F4F6",
    textTransform: "uppercase",
  },
  logoBadge: {
    fontSize: 10,
    background: "#1E40AF22",
    color: "#60A5FA",
    border: "1px solid #1E40AF",
    borderRadius: 4,
    padding: "2px 8px",
    letterSpacing: 1,
    textTransform: "uppercase",
  },
  headerSub: { fontSize: 12, color: "#6B7280" },
  headerStats: { display: "flex", gap: 20 },
  stat: {
    display: "flex",
    flexDirection: "column",
    alignItems: "flex-end",
    color: "#9CA3AF",
  },
  statNum: { fontSize: 22, fontWeight: 700, color: "#E5E7EB", lineHeight: 1 },
  statLabel: { fontSize: 10, letterSpacing: 1, textTransform: "uppercase" },
  tabs: {
    display: "flex",
    borderBottom: "1px solid #1F2937",
    padding: "0 24px",
    background: "#0D1525",
  },
  tab: {
    background: "none",
    border: "none",
    color: "#6B7280",
    padding: "12px 20px",
    cursor: "pointer",
    fontSize: 13,
    borderBottom: "2px solid transparent",
    fontFamily: "inherit",
    transition: "all 0.15s",
  },
  tabActive: {
    color: "#60A5FA",
    borderBottom: "2px solid #60A5FA",
  },
  body: { flex: 1, padding: 24 },
  twocol: {
    display: "grid",
    gridTemplateColumns: "1fr 1.4fr",
    gap: 20,
    height: "100%",
  },
  list: { display: "flex", flexDirection: "column", gap: 8 },
  methodCard: {
    background: "#111827",
    border: "1px solid #1F2937",
    borderRadius: 10,
    padding: "12px 16px",
    cursor: "pointer",
    display: "flex",
    justifyContent: "space-between",
    alignItems: "center",
    transition: "all 0.15s",
  },
  methodCardActive: {
    borderColor: "#3B82F6",
    background: "#111D2E",
  },
  methodLeft: { display: "flex", alignItems: "center", gap: 12 },
  methodDot: { width: 6, height: 6, borderRadius: "50%" },
  methodIcon: { fontSize: 20 },
  methodName: { fontSize: 13, fontWeight: 600, color: "#E5E7EB" },
  methodType: { fontSize: 11, color: "#6B7280", textTransform: "uppercase", letterSpacing: 1 },
  methodRight: { display: "flex", alignItems: "center", gap: 10 },
  behaviorBadge: {
    fontSize: 11,
    padding: "3px 8px",
    borderRadius: 4,
    fontWeight: 600,
  },
  toggle: {
    border: "none",
    borderRadius: 4,
    padding: "4px 10px",
    fontSize: 11,
    fontWeight: 700,
    cursor: "pointer",
    color: "#fff",
    fontFamily: "inherit",
    letterSpacing: 1,
  },
  detail: {
    background: "#111827",
    border: "1px solid #1F2937",
    borderRadius: 12,
    padding: 20,
    display: "flex",
    flexDirection: "column",
    gap: 16,
    overflowY: "auto",
  },
  detailHeader: { display: "flex", alignItems: "flex-start", gap: 14 },
  detailTitle: { fontSize: 16, fontWeight: 700, color: "#F3F4F6" },
  detailDesc: { fontSize: 12, color: "#6B7280", marginTop: 2 },
  emptyDetail: {
    display: "flex",
    flexDirection: "column",
    alignItems: "center",
    justifyContent: "center",
    gap: 12,
    color: "#374151",
    fontSize: 13,
    height: 300,
  },
  section: { display: "flex", flexDirection: "column", gap: 8 },
  sectionTitle: { fontSize: 11, color: "#6B7280", textTransform: "uppercase", letterSpacing: 1.5 },
  mockData: {
    display: "flex",
    justifyContent: "space-between",
    alignItems: "center",
    background: "#0A0F1A",
    borderRadius: 6,
    padding: "8px 12px",
  },
  mockLabel: { fontSize: 11, color: "#6B7280" },
  mockValue: { fontSize: 12, color: "#60A5FA", letterSpacing: 1 },
  behaviorGrid: { display: "flex", flexWrap: "wrap", gap: 6 },
  behaviorBtn: {
    border: "1px solid",
    borderRadius: 6,
    padding: "6px 12px",
    fontSize: 12,
    cursor: "pointer",
    fontFamily: "inherit",
    transition: "all 0.15s",
  },
  reasonGrid: { display: "flex", flexWrap: "wrap", gap: 6 },
  reasonBtn: {
    border: "1px solid",
    borderRadius: 6,
    padding: "5px 10px",
    fontSize: 11,
    cursor: "pointer",
    fontFamily: "inherit",
    transition: "all 0.15s",
  },
  input: {
    background: "#0A0F1A",
    border: "1px solid #374151",
    borderRadius: 6,
    padding: "8px 12px",
    color: "#E5E7EB",
    fontSize: 12,
    fontFamily: "inherit",
    width: "100%",
    boxSizing: "border-box",
  },
  range: { width: "100%", accentColor: "#3B82F6" },
  rangeLabels: {
    display: "flex",
    justifyContent: "space-between",
    fontSize: 10,
    color: "#6B7280",
  },
  simBtn: {
    background: "linear-gradient(135deg, #1D4ED8, #3B82F6)",
    border: "none",
    borderRadius: 8,
    color: "#fff",
    padding: "12px 20px",
    fontSize: 13,
    fontWeight: 700,
    cursor: "pointer",
    fontFamily: "inherit",
    letterSpacing: 0.5,
    transition: "opacity 0.15s",
  },
  simulateTab: { display: "flex", flexDirection: "column", gap: 20 },
  amountRow: {
    display: "flex",
    alignItems: "center",
    background: "#111827",
    border: "1px solid #374151",
    borderRadius: 8,
    padding: "0 16px",
    width: 220,
  },
  amountPrefix: { fontSize: 18, color: "#6B7280", marginRight: 8 },
  amountInput: {
    background: "none",
    border: "none",
    color: "#F3F4F6",
    fontSize: 24,
    fontWeight: 700,
    fontFamily: "inherit",
    width: 140,
    outline: "none",
    padding: "12px 0",
  },
  simGrid: {
    display: "grid",
    gridTemplateColumns: "repeat(auto-fill, minmax(160px, 1fr))",
    gap: 12,
  },
  simCard: {
    background: "#111827",
    border: "1px solid",
    borderRadius: 10,
    padding: 16,
    cursor: "pointer",
    display: "flex",
    flexDirection: "column",
    alignItems: "center",
    gap: 6,
    position: "relative",
    fontFamily: "inherit",
    transition: "all 0.15s",
  },
  simCardDot: {
    position: "absolute",
    top: 10,
    right: 10,
    width: 8,
    height: 8,
    borderRadius: "50%",
  },
  simCardName: { fontSize: 12, fontWeight: 600, color: "#E5E7EB", textAlign: "center" },
  simCardBehavior: { fontSize: 11, textAlign: "center" },
  disabledOverlay: {
    position: "absolute",
    inset: 0,
    display: "flex",
    alignItems: "center",
    justifyContent: "center",
    background: "#0A0F1A99",
    borderRadius: 10,
    fontSize: 10,
    letterSpacing: 2,
    color: "#6B7280",
    fontWeight: 700,
  },
  eventsTab: { display: "flex", flexDirection: "column", gap: 12 },
  eventHeader: { display: "flex", justifyContent: "space-between", alignItems: "center" },
  clearBtn: {
    background: "none",
    border: "1px solid #374151",
    borderRadius: 6,
    color: "#6B7280",
    padding: "6px 12px",
    cursor: "pointer",
    fontSize: 12,
    fontFamily: "inherit",
  },
  eventList: { display: "flex", flexDirection: "column", gap: 8 },
  eventCard: {
    background: "#111827",
    border: "1px solid #1F2937",
    borderRadius: 10,
    padding: "12px 16px",
    display: "flex",
    flexDirection: "column",
    gap: 6,
  },
  eventTop: { display: "flex", justifyContent: "space-between", alignItems: "center" },
  eventLeft: { display: "flex", alignItems: "center", gap: 8 },
  eventStatus: { fontSize: 11, padding: "2px 8px", borderRadius: 4, fontWeight: 700 },
  eventMethod: { fontSize: 13, color: "#E5E7EB", fontWeight: 600 },
  eventRight: { display: "flex", alignItems: "center", gap: 12 },
  eventAmount: { fontSize: 14, fontWeight: 700, color: "#F3F4F6" },
  eventTime: { fontSize: 11, color: "#6B7280" },
  eventBus: { display: "flex", alignItems: "center", gap: 8 },
  eventBusLabel: { fontSize: 10, color: "#6B7280", textTransform: "uppercase", letterSpacing: 1 },
  eventBusEvent: {
    fontSize: 12,
    color: "#60A5FA",
    background: "#1E3A5F22",
    padding: "2px 8px",
    borderRadius: 4,
    border: "1px solid #1E40AF44",
  },
  eventReason: { fontSize: 12, color: "#EF4444", paddingLeft: 2 },
  eventId: { fontSize: 10, color: "#374151", letterSpacing: 1 },
};
