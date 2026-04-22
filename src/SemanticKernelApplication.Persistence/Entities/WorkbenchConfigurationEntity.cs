namespace SemanticKernelApplication.Persistence.Entities;

internal sealed class WorkbenchConfigurationEntity
{
    public string ConfigurationId { get; set; } = string.Empty;
    public string WorkspacePath { get; set; } = string.Empty;
    public string SelectedProviderId { get; set; } = string.Empty;
    public string SelectedModelId { get; set; } = string.Empty;
}
