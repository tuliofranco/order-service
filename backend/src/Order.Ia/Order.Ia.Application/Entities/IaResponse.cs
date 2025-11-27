namespace Order.Ia.Application.Entities;

public record IaResponse(string Answer , int TokensUsed, string? Model);