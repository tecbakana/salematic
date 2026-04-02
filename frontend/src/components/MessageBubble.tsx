import { Mensagem } from '../types'

interface Props {
  mensagem: Mensagem
}

export function MessageBubble({ mensagem }: Props) {
  const isUser = mensagem.role === 'user'
  return (
    <div style={{
      display: 'flex',
      justifyContent: isUser ? 'flex-end' : 'flex-start',
      marginBottom: 8
    }}>
      <div style={{
        maxWidth: '70%',
        padding: '10px 14px',
        borderRadius: 12,
        background: isUser ? '#0070f3' : '#f0f0f0',
        color: isUser ? '#fff' : '#222',
        fontSize: 14,
        lineHeight: 1.5,
        whiteSpace: 'pre-wrap'
      }}>
        {mensagem.content}
      </div>
    </div>
  )
}
