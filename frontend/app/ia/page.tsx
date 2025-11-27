"use client";

import { useState } from "react";
import AppSidebar from "@/components/layout/AppSidebar";
import { Sparkles } from "lucide-react";
import { useToast } from "@/hooks/use-toast";
import { iaService } from "@/lib/services/ia";

import { IaForm } from "@/components/ia/IaForm";
import { IaHistory } from "@/components/ia/IaHistory";

export default function IaPage() {
  const [loading, setLoading] = useState(false);
  const [history, setHistory] = useState<
    { question: string; answer: string }[]
  >([]);

  const { toast } = useToast();

  async function handleAsk(question: string) {
    setLoading(true);

    try {
      const res = await iaService.ask(question);

      setHistory((prev) => [
        { question, answer: res.answer },
        ...prev,
      ]);

      toast({
        title: "Resposta recebida!",
        description: "A IA retornou uma resposta com sucesso.",
      });
    } catch (err: any) {
      toast({
        title: "Erro",
        description: err.message,
        variant: "destructive",
      });
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="min-h-screen grid bg-gray-50 lg:grid-cols-[280px_1fr]">
      <AppSidebar />

      <main className="flex items-start justify-center p-3 sm:p-6">
        <div className="w-full max-w-5xl">
          <div className="border-0 shadow-md rounded-2xl overflow-hidden bg-white">
            
            <header className="p-6 border-b bg-gray-50 flex items-center gap-3">
              <Sparkles className="h-6 w-6 text-blue-600" />
              <div>
                <h2 className="text-xl font-bold">Inteligência Artificial</h2>
                <p className="text-sm text-gray-500">
                  Faça perguntas sobre os pedidos do sistema
                </p>
              </div>
            </header>

            <div className="p-6 space-y-6">
              <IaForm loading={loading} onAsk={handleAsk} />
              <IaHistory history={history} />
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}
