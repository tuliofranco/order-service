interface Props {
  history: { question: string; answer: string }[];
}

export function IaHistory({ history }: Props) {
  return (
    <div className="space-y-4">
      {history.length === 0 && (
        <p className="text-sm text-gray-500">
          Nenhuma pergunta feita ainda.
        </p>
      )}

      {history.map((item, idx) => (
        <div
          key={idx}
          className="border rounded-lg p-4 bg-gray-50 shadow-sm"
        >
          <p className="font-semibold text-gray-800">
            Pergunta: {item.question}
          </p>
          <p className="mt-2 text-gray-700 whitespace-pre-wrap">
            {item.answer}
          </p>
        </div>
      ))}
    </div>
  );
}
