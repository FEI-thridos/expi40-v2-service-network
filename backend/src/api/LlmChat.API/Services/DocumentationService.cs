using LlmChat.API.DomainModels;
using LlmChat.API.Features.Chat;
using System.Text;

namespace LlmChat.API.Services;

public static class DocumentationService
{
    public static async Task<PostAnswerQuestionResponse> RetrieveHandoverDocumentation(string aasShortId)
    {
        // ------------------------------------------------------------------------
        // Finds Handover Documentation .pdf file of specified AAS by it's shortId
        // ------------------------------------------------------------------------

        // Config
        const string submodelSemanticId = "0173-1#01-AHF578#003";   // Semantic ID of HandoverDocumentation submodels
        const string downloadedPdfFilePath = "/app/HandoverPDFs";
        Directory.CreateDirectory(downloadedPdfFilePath);
        var httpClient = new HttpClient();

        // Step 1 --> Get list of all shells
        var shellsResponse = await httpClient.GetFromJsonAsync<ShellsResponse>("http://aas-env:8081/shells");

        // Step 2 --> Get specific shell from the list of shells by idShort
        if (shellsResponse?.Result.FirstOrDefault(s => s.IdShort == aasShortId) is not { } conveyorShell)
        {
            Console.WriteLine($"AAS with shortId {aasShortId} not found.");
            return new PostAnswerQuestionResponse("Failed", $"AAS with shortId {aasShortId} not found.");
        }
        Console.WriteLine($"\nAAS {aasShortId} found! ID: {conveyorShell.Id}");

        if (conveyorShell.Submodels.Count == 0)     // Check for submodels in AAS
        {
            Console.WriteLine("AAS has no submodels.");
            return new PostAnswerQuestionResponse("Failed", $"AAS has no submodels.");
        }

        // Step 3 --> For each Submodel ID, until matched / end
        //        --> Base64 encode Submodel ID --> Get Submodel info by ID
        //        --> Semantic IDs match ??? --> if yes --> VOILA, use Id of the matched Submodel in Step 4

        string? handoverDocuSubmodelId = null;

        foreach (var submodelRef in conveyorShell.Submodels)     // Loop through submodel references until found / end
        {
            string? submodelId = submodelRef.Keys.FirstOrDefault()?.Value;      // Find submodelId
            if (string.IsNullOrEmpty(submodelId)) continue;                     // Check if empty
            string submodelIdBase64 = Convert.ToBase64String(                   // Encode submodelId
                Encoding.UTF8.GetBytes(submodelId));
            var submodel = await httpClient.GetFromJsonAsync<Submodel>(         // Get submodel by submodelId
                $"http://aas-env:8081/submodels/{submodelIdBase64}");

            string? semanticValue = submodel?.SemanticId.Keys.FirstOrDefault()?.Value;  // Find SemanticId of the submodel
            if (semanticValue == submodelSemanticId)                                    // Identify HandoverDocu by SemanticId
            {
                handoverDocuSubmodelId = submodelId;    // Store REGULAR Id of the HandoverDocu submodel
                Console.WriteLine($"Found Handover Documentation submodel! ID: {handoverDocuSubmodelId}");
                break;                                  // When found stop searching
            }
        }

        if (handoverDocuSubmodelId == null)    // If no HandoverDocumentation submodel found
        {
            Console.WriteLine($"No submodel found with the semantic ID {submodelSemanticId} for HandoverDocumentation.");
            return new PostAnswerQuestionResponse("Failed", $"No submodel found with the semantic ID {submodelSemanticId} for HandoverDocumentation.");
        }

        // Step 4 --> Encode submodelId --> Download the file by submodelId and elementPath
        // ToDo --> look into dynamically retrieving elementPath if needed (for example if there are multiple docs stored)

        string submodelIdBase64String =                                                       // Encode handoverDocu Id
            Convert.ToBase64String(Encoding.UTF8.GetBytes(handoverDocuSubmodelId));
        string elementPath = "Documents%5B0%5D.DocumentVersions%5B0%5D.DigitalFiles%5B0%5D";  // Documents[0].DocVer[0].DigFiles[0]
        string submodelElementUrl =                                                           // Get URL to stored .pdf
            $"http://aas-env:8081/submodels/{submodelIdBase64String}/submodel-elements/{elementPath}/attachment";

        HttpResponseMessage fileResponse = await httpClient.GetAsync(submodelElementUrl);     // Download the file

        if (fileResponse.IsSuccessStatusCode)
        {
            byte[] fileBytes = await fileResponse.Content.ReadAsByteArrayAsync();                   // Read file in bites
            string filePath = Path.Combine(downloadedPdfFilePath, "HandoverDocumentation.pdf");     // Create path
            await File.WriteAllBytesAsync(filePath, fileBytes);                                     // Save file at this path
            Console.WriteLine("PDF downloaded successfully.");
            return new PostAnswerQuestionResponse("Success", $"PDF downloaded successfully.");
        }
        else
        {
            Console.WriteLine("Failed to download the PDF file. Status code: " + fileResponse.StatusCode);
            return new PostAnswerQuestionResponse("Failed", $"Failed to download the PDF file. Status code: " + fileResponse.StatusCode);
        }
    }
}