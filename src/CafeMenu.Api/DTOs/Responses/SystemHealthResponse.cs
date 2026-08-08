namespace CafeMenu.Api.DTOs.Responses;

public sealed record SystemHealthResponse(string Status, DateTimeOffset CheckedAt);
