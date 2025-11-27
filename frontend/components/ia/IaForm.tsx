"use client";

import { useState } from "react";
import { Send } from "lucide-react";

interface Props {
  loading: boolean;
  onAsk: (question: string) => void;
}

export function IaForm({ loading, onAsk }: Props) {
  const [question, setQuestion] = useState("");

  function handleSend() {
    if (!question.trim()) return;
    onAsk(question);
    setQuestion("");
  }

  return (
    <div className="flex gap-3">
      <input
        value={question}
        onChange={(e) => setQuestion(e.target.value)}
        className="flex-1 border rounded-lg px-4 py-2 text-sm"
        placeholder="Digite sua pergunta..."
      />

      <button
        onClick={handleSend}
        disabled={loading}
        className="
          flex items-center gap-2 px-4 py-2 rounded-lg
          bg-blue-600 text-white text-sm font-medium
          hover:bg-blue-700 transition disabled:opacity-50
        "
      >
        <Send className="h-4 w-4" />
        Perguntar
      </button>
    </div>
  );
}
