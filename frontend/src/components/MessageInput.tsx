import { useState, KeyboardEvent } from 'react'

interface Props {
  onEnviar: (texto: string) => void
  disabled: boolean
}

export function MessageInput({ onEnviar, disabled }: Props) {
  const [texto, setTexto] = useState('')

  const handleEnviar = () => {
    const trimmed = texto.trim()
    if (!trimmed || disabled) return
    onEnviar(trimmed)
    setTexto('')
  }

  const handleKeyDown = (e: KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      handleEnviar()
    }
  }

  return (
    <div style={{ display: 'flex', gap: 8, padding: '12px 16px', borderTop: '1px solid #e0e0e0' }}>
      <textarea
        value={texto}
        onChange={e => setTexto(e.target.value)}
        onKeyDown={handleKeyDown}
        placeholder="Digite sua mensagem... (Enter para enviar)"
        disabled={disabled}
        rows={2}
        style={{
          flex: 1,
          resize: 'none',
          padding: '8px 12px',
          borderRadius: 8,
          border: '1px solid #ccc',
          fontSize: 14,
          fontFamily: 'inherit'
        }}
      />
      <button
        onClick={handleEnviar}
        disabled={disabled || !texto.trim()}
        style={{
          padding: '0 20px',
          borderRadius: 8,
          background: '#0070f3',
          color: '#fff',
          border: 'none',
          cursor: disabled ? 'not-allowed' : 'pointer',
          opacity: disabled ? 0.6 : 1,
          fontSize: 14
        }}
      >
        Enviar
      </button>
    </div>
  )
}
