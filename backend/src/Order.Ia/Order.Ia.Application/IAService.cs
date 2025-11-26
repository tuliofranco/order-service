using System.Net.Http.Json;
using OpenAI;
using OpenAI.Chat;
using Microsoft.Extensions.Http;

namespace Order.Ia.Application;

public class IAService
{
    private readonly HttpClient _http;
    private readonly ChatClient _chat;
    public IAService (IHttpClientFactory factory, OpenAIClient client)
    {
        _http = factory.CreateClient("order-api");
        _chat = client.GetChatClient("gpt-4o-mini");
    }

    public async Task<string> ExecuteSqlAsync(string sql)
    {
        var response = await _http.PostAsJsonAsync("/internal/sql", new { query = sql });

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }
    private async Task<string> GenerateSqlAsync(string question)
    {
        var schema   = File.ReadAllText("Prompts/SchemaPrompt.txt");
        var rules    = File.ReadAllText("Prompts/RulesPrompt.txt");
        var examples = File.ReadAllText("Prompts/ExamplesPrompt.txt");
        var task     = File.ReadAllText("Prompts/TaskPrompt.txt");

        var finalPrompt = $@"
{schema}

{rules}

{examples}

{task}

=== PERGUNTA DO USUÁRIO ===
{question}

Responda SOMENTE com o SQL, sem explicação, sem markdown, sem crases.
";

        var result = await _chat.CompleteChatAsync(finalPrompt);
        return result.Value.Content[0].Text.Trim();
    }

    public async Task<IaResponse> AnswerAsync(string question)
    {

        var sql = await GenerateSqlAsync(question);
        var rawResult = await ExecuteSqlAsync(sql);

        var summarizePrompt = $@"
Você é uma assistente que responde perguntas sobre pedidos.

PERGUNTA DO USUÁRIO:
{question}

SQL EXECUTADO:
{sql}

RESULTADO DA API (JSON):
{rawResult}

TAREFA:
Explique o resultado para o usuário em português, de forma clara e resumida,
sem mostrar o SQL e sem mostrar o JSON bruto.
Se não houver resultados, explique isso de forma educada.
";

        var summary = await _chat.CompleteChatAsync(summarizePrompt);
        var finalAnswer = summary.Value.Content[0].Text.Trim();
        return new IaResponse(finalAnswer);
    }

}

public record IaResponse(string Answer);
