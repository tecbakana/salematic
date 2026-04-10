import { useState, useRef, useEffect } from 'react'
import { MessageBubble } from './components/MessageBubble'
import { MessageInput } from './components/MessageInput'
import { enviarMensagem } from './api'
import { Mensagem } from './types'
import SalematicaMock from './mocks/salematica-payment-mock'

// Em produção, o clienteId viria de autenticação JWT
const CLIENT_ID = 1

export default function App() {
  if (new URLSearchParams(window.location.search).get('mock') === 'payment') {
    return <SalematicaMock />
  }

  const [historico, setHistorico] = useState<Mensagem[]>([])
  const [carregando, setCarregando] = useState(false)
  const [erro, setErro] = useState<string | null>(null)
  const bottomRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [historico, carregando])

  const handleEnviar = async (texto: string) => {
    const novaMensagem: Mensagem = { role: 'user', content: texto }
    const historicoAtualizado = [...historico, novaMensagem]
    setHistorico(historicoAtualizado)
    setCarregando(true)
    setErro(null)

    try {
      const resposta = await enviarMensagem({
        mensagem: texto,
        clienteId: CLIENT_ID,
        historico: historico
      })
      setHistorico(resposta.historico)
    } catch (e) {
      setErro(e instanceof Error ? e.message : 'Erro inesperado')
      setHistorico(historicoAtualizado)
    } finally {
      setCarregando(false)
    }
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', height: '100vh', maxWidth: 720, margin: '0 auto' }}>
      <header style={{ padding: '16px 20px', borderBottom: '1px solid #e0e0e0', background: '#fff' }}>
        <h1 style={{ margin: 0, fontSize: 20, fontWeight: 600 }}>Salematic</h1>
        <span style={{ fontSize: 12, color: '#666' }}>Assistente de Vendas</span>
      </header>

      <main style={{ flex: 1, overflowY: 'auto', padding: '16px 20px' }}>
        {historico.length === 0 && (
          <p style={{ color: '#999', textAlign: 'center', marginTop: 40 }}>
            Olá! Como posso ajudar com seus pedidos hoje?
          </p>
        )}
        {historico.map((m, i) => <MessageBubble key={i} mensagem={m} />)}
        {carregando && (
          <div style={{ color: '#999', fontSize: 13, padding: '4px 0' }}>Processando...</div>
        )}
        {erro && (
          <div style={{ color: '#c00', fontSize: 13, padding: '4px 0' }}>{erro}</div>
        )}
        <div ref={bottomRef} />
      </main>

      <MessageInput onEnviar={handleEnviar} disabled={carregando} />
    </div>
  )
}
