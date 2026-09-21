namespace LlmChat.API.DomainModels;

public class Shell
{
    public string Id { get; set; } = null!;

    public string IdShort { get; set; } = null!;

    public List<SubmodelReference> Submodels { get; set; } = [];
}