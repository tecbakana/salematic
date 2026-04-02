import { ChatRequest, ChatResponse } from './types'

const BASE_URL = '/api'

export async function enviarMensagem(request: ChatRequest): Promise<ChatResponse> {
  const response = await fetch(`${BASE_URL}/chat/mensagem`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request)
  })

  if (!response.ok) {
    const erro = await response.json().catch(() => ({ erro: 'Erro desconhecido' }))
    throw new Error(erro.erro ?? 'Erro ao processar mensagem')
  }

  return response.json()
}
