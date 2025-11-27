import { apiIa } from "@/lib/api-ia";

export const iaService = {
  async ask(question: string): Promise<{ answer: string }> {
    const { data } = await apiIa.post("/ask", {
      pergunta: question,
    });

    return data;
  },
};
