using OpenAI;
using DotNetEnv;
using Order.Ia.Api.DTOs;
using Order.Ia.Application;

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("default");

app.MapPost("/ask", async (AskRequest request, IAService iaService) =>
{
    var response = await iaService.AnswerAsync(request.Pergunta);
    return Results.Ok(response);
})
.WithName("AskIa")
.WithOpenApi();

app.Run();
