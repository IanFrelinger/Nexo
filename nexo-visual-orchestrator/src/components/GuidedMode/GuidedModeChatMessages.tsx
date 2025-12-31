// src/components/GuidedMode/GuidedModeChatMessages.tsx

/**
 * GuidedModeChatMessages Component
 * 
 * Renders the chat message history in the guided mode interface. Displays
 * assistant messages (with avatar), user messages, and a typing indicator
 * when the assistant is "typing". Supports basic markdown formatting (bold).
 */

import { HiChat } from 'react-icons/hi';

interface ChatMessage {
  id: string;
  type: 'assistant' | 'user' | 'options';
  content: string;
}

interface GuidedModeChatMessagesProps {
  messages: ChatMessage[];
  isTyping: boolean;
}

export default function GuidedModeChatMessages({
  messages,
  isTyping,
}: GuidedModeChatMessagesProps) {
  return (
    <div className="flex-1 overflow-y-auto p-4 space-y-4">
      {messages.map(msg => (
        <div key={msg.id}>
          {msg.type === 'assistant' && (
            <div className="flex gap-3">
              <div className="w-8 h-8 rounded-full bg-purple-500/20 flex items-center justify-center flex-shrink-0">
                <HiChat className="w-4 h-4 text-purple-400" />
              </div>
              <div className="bg-slate-800 rounded-lg px-4 py-2 max-w-[85%]">
                <p 
                  className="text-sm text-slate-200"
                  dangerouslySetInnerHTML={{
                    __html: msg.content
                      .replace(/\*\*(.*?)\*\*/g, '<strong class="text-white">$1</strong>')
                      .replace(/\n/g, '<br />')
                  }}
                />
              </div>
            </div>
          )}

          {msg.type === 'user' && (
            <div className="flex justify-end">
              <div className="bg-purple-600 rounded-lg px-4 py-2 max-w-[80%]">
                <p className="text-sm text-white">{msg.content}</p>
              </div>
            </div>
          )}
        </div>
      ))}

      {isTyping && (
        <div className="flex gap-3">
          <div className="w-8 h-8 rounded-full bg-purple-500/20 flex items-center justify-center">
            <HiChat className="w-4 h-4 text-purple-400" />
          </div>
          <div className="bg-slate-800 rounded-lg px-4 py-2">
            <div className="flex gap-1">
              <div className="w-2 h-2 bg-slate-500 rounded-full animate-bounce" style={{ animationDelay: '0ms' }} />
              <div className="w-2 h-2 bg-slate-500 rounded-full animate-bounce" style={{ animationDelay: '150ms' }} />
              <div className="w-2 h-2 bg-slate-500 rounded-full animate-bounce" style={{ animationDelay: '300ms' }} />
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

