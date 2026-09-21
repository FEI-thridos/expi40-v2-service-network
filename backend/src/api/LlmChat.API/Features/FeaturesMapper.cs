using LlmChat.API.Features.Chat;

namespace LlmChat.API.Features;

public static class FeaturesMapper
{
    public static void MapEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/ask", PostAnswerQuestion.AnswerQuestion)
            .WithOpenApi();
    }
}