export interface Mensagem {
  role: 'user' | 'assistant'
  content: string
}

export interface ChatRequest {
  mensagem: string
  clienteId: number
  historico: Mensagem[]
}

export interface ChatResponse {
  resposta: string
  ferramentaUsada?: string
  historico: Mensagem[]
}
