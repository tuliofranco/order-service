using OpenAI;
using DotNetEnv;
using Order.Ia.Api.DTOs;
using Order.Ia.Application;
using Order.Ia.Application.Services;

Env.Load("../../../../.env");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddSingleton(sp =>
{
    var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new Exception("OPENAI_API_KEY not found.");

    return new OpenAIClient(apiKey);
});


builder.Services.AddHttpClient("order-api", client =>
{
    client.BaseAddress = new Uri(Environment.GetEnvironmentVariable("ORDER_API_URL")
        ?? throw new Exception("ORDER_API_URL missing"));
});

builder.Services.AddApplicationServices();

builder.Services.AddCors(options =>
{
    options.AddPolicy("default", p =>
        p.AllowAnyOrigin()
         .AllowAnyHeader()
         .AllowAnyMethod());
});

var app = builder.Build();


app.UseSwagger();
app.UseSwaggerUI(o =>
{
    o.SwaggerEndpoint("/swagger/v1/swagger.json", "Order IA API v1");
    o.RoutePrefix = "swagger";
});

app.UseCors("default");

app.MapPost("/ask", async (AskRequest request, IAService iaService, IAHistoryService historyService) =>
{
    var response = await iaService.AnswerAsync(request.Pergunta);
    await historyService.SaveAsync(
        question: request.Pergunta,
        response: response
    );

    return Results.Ok(response);
})
.WithName("AskIa")
.WithOpenApi();

app.MapGet("/history", async (IAHistoryService history) =>
{
    var items = await history.ListAsync();
    return Results.Ok(items);
})
.WithName("IaHistory")
.WithOpenApi();;

app.Run();
