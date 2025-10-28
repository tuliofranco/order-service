export function formatDateTime(iso: string) {
  return new Intl.DateTimeFormat("pt-BR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  }).format(new Date(iso));
}
export function formatCurrency(value: number) {
  return new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: "BRL",
  }).format(value);
}


export function getStatusVariant(
  status: string
): "default" | "secondary" | "destructive" | "outline" {
  const s = status.toLowerCase();

  if (
    s.includes("finalizado") ||
    s.includes("entregue") ||
    s.includes("concluído")
  )
    return "default";

  if (s.includes("processando") || s.includes("trânsito")) return "secondary";

  if (s.includes("cancelado") || s.includes("erro")) return "destructive";

  return "outline";
}
