export default function StatusBadge({ status }: { status: 'Pendente'|'Processando'|'Finalizado' }) {
  const cls =
    status === 'Pendente' ? 'bg-yellow-100 text-yellow-800' :
    status === 'Processando' ? 'bg-blue-100 text-blue-800 animate-pulse' :
    'bg-green-100 text-green-800';

  return (
    <span className={`inline-flex items-center px-2 py-1 rounded-xl text-xs font-medium ${cls}`}>
      {status}
    </span>
  );
}
