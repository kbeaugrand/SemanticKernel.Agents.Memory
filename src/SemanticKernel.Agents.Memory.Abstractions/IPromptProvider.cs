namespace SemanticKernel.Agents.Memory;

public interface IPromptProvider
{
    /// <summary>
    /// Return a prompt content
    /// </summary>
    /// <param name="promptName">Prompt name</param>
    /// <returns>Prompt string</returns>
    public string ReadPrompt(string promptName);
}
