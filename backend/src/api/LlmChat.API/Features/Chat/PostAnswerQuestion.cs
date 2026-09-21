using LlmChat.API.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace LlmChat.API.Features.Chat;

public class PostAnswerQuestion
{
    public static async Task<Results<Ok<PostAnswerQuestionResponse>, BadRequest<PostAnswerQuestionResponse>>> AnswerQuestion(
        [FromServices] ILogger<PostAnswerQuestion> logger,
        [FromBody] PostAnswerQuestionRequest request)
    {
        // Downloads Handover Documentation from AAS
        Task<PostAnswerQuestionResponse> documentationResponse = DocumentationService.RetrieveHandoverDocumentation(request.ShortIdAas);
        if (documentationResponse.Result.Status != "Success")
        {
            logger.LogError("Handover documentation could not be retrieved from AAS for ShortIdAas: {ShortIdAas}", request.ShortIdAas);
            return TypedResults.BadRequest(new PostAnswerQuestionResponse(documentationResponse.Result.Status, documentationResponse.Result.Answer));
        }

        // Calls Python LLM service
        string answer = await PythonService.RunPythonCode(request.Question);

        // Build response & Returns result
        return TypedResults.Ok(new PostAnswerQuestionResponse("Handover documentation found", answer));
    }
}

public class PostAnswerQuestionRequest
{
    [JsonPropertyName("question")]
    public string Question { get; set; } = null!;

    [JsonPropertyName("short-id-aas")]
    public string ShortIdAas { get; set; } = null!;
}

public class PostAnswerQuestionResponse(string status, string answer)
{
    [JsonPropertyName("answer")]
    public string Answer { get; set; } = answer;

    [JsonPropertyName("status")]
    public string Status { get; set; } = status;
}