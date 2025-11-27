using System.Net.Http.Json;
using OpenAI;
using OpenAI.Chat;
using Microsoft.Extensions.Http;
using Order.Ia.Application.Entities;
using OpenAI.Models;
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
    private async  Task<(string Sql, ChatTokenUsage Usage, string Model)> GenerateSqlAsync(string question)
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
        var completion = result.Value;
        var sql = completion.Content[0].Text.Trim();
        var model = completion.Model;
        var usage = completion.Usage;
        return (sql, usage, model);
    }

    public async Task<IaResponse> AnswerAsync(string question)
    {

        var (sql, usageSqlStep, modelFromSqlStep)  = await GenerateSqlAsync(question);
        var rawResult  = await ExecuteSqlAsync(sql);

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

        var summaryResult = await _chat.CompleteChatAsync(summarizePrompt);
        var summaryCompletion = summaryResult.Value;

        var finalAnswer = summaryCompletion.Content[0].Text.Trim();
        var modelFromSummaryStep = summaryCompletion.Model;
        var usageSummaryStep = summaryCompletion.Usage;

        var totalTokens =
            (usageSqlStep?.TotalTokenCount ?? 0) +
            (usageSummaryStep?.TotalTokenCount ?? 0);

        var finalModel = modelFromSummaryStep ?? modelFromSqlStep;

        return new IaResponse(
            Answer: finalAnswer,
            TokensUsed: totalTokens,
            Model: finalModel
        );
    }

}


