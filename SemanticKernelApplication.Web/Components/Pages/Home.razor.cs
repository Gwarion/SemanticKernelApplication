using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SemanticKernelApplication.Abstractions.Activities;
using SemanticKernelApplication.Abstractions.Agents;
using SemanticKernelApplication.Abstractions.Conversations;
using SemanticKernelApplication.Abstractions.Providers;
using SemanticKernelApplication.Abstractions.Workbench;
using SemanticKernelApplication.Web.Services;

namespace SemanticKernelApplication.Web.Components.Pages;

public partial class Home : IAsyncDisposable
{
    [Inject] private IAgentWorkbenchService Workbench { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private readonly List<ActivityViewModel> ActivityFeed = [];

    private List<AgentDefinition> Agents { get; set; } = [];
    private List<ModelProviderDefinition> Providers { get; set; } = [];
    private List<string> KnownWorkspacePaths { get; set; } = [];
    private List<ConversationMessage> Messages { get; set; } = [];
    private string ChatMessage { get; set; } = string.Empty;
    private string AgentDescription { get; set; } = string.Empty;
    private string WorkspacePathInput { get; set; } = string.Empty;
    private string NewWorkspacePathInput { get; set; } = string.Empty;
    private string ActiveWorkspacePath { get; set; } = string.Empty;
    private string ApiKeyInput { get; set; } = string.Empty;
    private string SetupFeedback { get; set; } = string.Empty;
    private string? ActiveConversationId { get; set; }
    private string? SelectedProviderId { get; set; }
    private string? SelectedModelId { get; set; }
    private bool ApiKeyConfigured { get; set; }
    private bool IsBusy { get; set; }
    private bool IsWorkspaceBusy { get; set; }
    private bool IsModelBusy { get; set; }

    private IJSObjectReference? _module;
    private IJSObjectReference? _subscription;
    private DotNetObjectReference<Home>? _objectReference;

    private bool CanApplyWorkspace =>
        !IsWorkspaceBusy
        && !string.IsNullOrWhiteSpace(WorkspacePathInput)
        && !string.Equals(WorkspacePathInput.Trim(), ActiveWorkspacePath, StringComparison.Ordinal);

    private bool CanAddWorkspace =>
        !IsWorkspaceBusy
        && !string.IsNullOrWhiteSpace(NewWorkspacePathInput);

    private bool CanApplyModel =>
        !IsModelBusy
        && !string.IsNullOrWhiteSpace(SelectedProviderId)
        && !string.IsNullOrWhiteSpace(SelectedModelId);

    private IEnumerable<string> WorkspaceSuggestions =>
        KnownWorkspacePaths
            .Concat(new[] { ActiveWorkspacePath, WorkspacePathInput, NewWorkspacePathInput })
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path.Trim())
            .Distinct(StringComparer.Ordinal);

    private IEnumerable<ActivityViewModel> ProgressFeed =>
        ActivityFeed.Where(item => !item.IsError);

    private IEnumerable<ActivityViewModel> ExceptionFeed =>
        ActivityFeed.Where(item => item.IsError);

    private string CurrentModelLabel =>
        SelectedProvider is null
            ? "None"
            : $"{SelectedProvider.DisplayName} / {SelectedProvider.Models.FirstOrDefault(model => model.Id == SelectedModelId)?.DisplayName ?? SelectedModelId}";

    private ModelProviderDefinition? SelectedProvider =>
        Providers.FirstOrDefault(provider => provider.Id == SelectedProviderId);

    private IReadOnlyList<ModelDefinition> AvailableModels =>
        SelectedProvider?.Models ?? [];

    protected override async Task OnInitializedAsync()
    {
        await RefreshSnapshotAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        _module = await JS.InvokeAsync<IJSObjectReference>("import", "./js/activity-stream.js");
        _objectReference = DotNetObjectReference.Create(this);
        _subscription = await _module.InvokeAsync<IJSObjectReference>(
            "startActivityStream",
            _objectReference,
            "/api/activity/stream?replay=30");
        ActiveConversationId = await _module.InvokeAsync<string?>("getActiveConversationId");
        if (!string.IsNullOrWhiteSpace(ActiveConversationId))
        {
            await RefreshSnapshotAsync(ActiveConversationId);
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task RefreshSnapshotAsync(string? conversationId = null)
    {
        var snapshot = await Workbench.GetSnapshotAsync(conversationId);
        Agents = snapshot.Agents.ToList();
        Providers = snapshot.Providers.ToList();
        KnownWorkspacePaths = snapshot.KnownWorkspacePaths.ToList();
        ActiveWorkspacePath = snapshot.WorkspacePath;
        if (string.IsNullOrWhiteSpace(WorkspacePathInput)
            || KnownWorkspacePaths.All(path => !string.Equals(path, WorkspacePathInput, StringComparison.OrdinalIgnoreCase)))
        {
            WorkspacePathInput = snapshot.WorkspacePath;
        }
        SelectedProviderId = snapshot.ModelConfiguration.SelectedProviderId;
        SelectedModelId = snapshot.ModelConfiguration.SelectedModelId;
        ApiKeyConfigured = snapshot.ModelConfiguration.ApiKeyConfigured;
        ApiKeyInput = snapshot.ModelConfiguration.ApiKey ?? string.Empty;
        Messages = snapshot.ActiveConversation?.Messages.OrderBy(message => message.CreatedAtUtc).ToList() ?? [];
        ActiveConversationId = snapshot.ActiveConversation?.ThreadId;
        MergeActivity(snapshot.RecentActivity.Select(ToViewModel));
    }

    private async Task ApplyWorkspaceAsync()
    {
        if (!CanApplyWorkspace)
        {
            return;
        }

        IsWorkspaceBusy = true;
        SetupFeedback = string.Empty;

        try
        {
            ActiveWorkspacePath = await Workbench.SetWorkspaceAsync(new WorkspaceSelectionRequest(WorkspacePathInput.Trim()));
            WorkspacePathInput = ActiveWorkspacePath;
            SetupFeedback = "Workspace updated for the current workbench session.";
            await RefreshSnapshotAsync();
        }
        catch (Exception ex)
        {
            SetupFeedback = ex.Message;
        }
        finally
        {
            IsWorkspaceBusy = false;
        }
    }

    private async Task AddWorkspaceAsync()
    {
        if (!CanAddWorkspace)
        {
            return;
        }

        WorkspacePathInput = NewWorkspacePathInput.Trim();
        await ApplyWorkspaceAsync();

        if (string.Equals(ActiveWorkspacePath, WorkspacePathInput, StringComparison.OrdinalIgnoreCase))
        {
            NewWorkspacePathInput = string.Empty;
            SetupFeedback = "Workspace added to the saved list and selected for the current bench.";
        }
    }

    private async Task ApplyModelAsync()
    {
        if (!CanApplyModel)
        {
            return;
        }

        IsModelBusy = true;
        SetupFeedback = string.Empty;

        try
        {
            var configuration = await Workbench.SetGlobalModelConfigurationAsync(
                new GlobalModelConfigurationRequest(SelectedProviderId!, SelectedModelId!, ApiKeyInput));
            SelectedProviderId = configuration.SelectedProviderId;
            SelectedModelId = configuration.SelectedModelId;
            ApiKeyConfigured = configuration.ApiKeyConfigured;
            ApiKeyInput = configuration.ApiKey ?? string.Empty;
            SetupFeedback = "Provider settings were saved locally. You can override this key any time from the setup form.";
            await RefreshSnapshotAsync();
        }
        catch (Exception ex)
        {
            SetupFeedback = ex.Message;
        }
        finally
        {
            IsModelBusy = false;
        }
    }

    private async Task CreateAgentAsync()
    {
        if (string.IsNullOrWhiteSpace(AgentDescription))
        {
            return;
        }

        IsBusy = true;

        try
        {
            await Workbench.CreateAgentFromTextAsync(new PlainTextAgentCreationRequest(AgentDescription.Trim()));
            AgentDescription = string.Empty;
            await RefreshSnapshotAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(ChatMessage))
        {
            return;
        }

        IsBusy = true;

        try
        {
            var response = await Workbench.SendCoordinatorMessageAsync(
                new CoordinatorChatRequest(ChatMessage.Trim(), ActiveConversationId));
            ActiveConversationId = response.ConversationId;
            if (_module is not null)
            {
                await _module.InvokeVoidAsync("setActiveConversationId", ActiveConversationId);
            }
            Messages = response.Result.Thread.Messages.OrderBy(message => message.CreatedAtUtc).ToList();
            MergeActivity(response.Activity.Select(ToViewModel));
            ChatMessage = string.Empty;
            await RefreshSnapshotAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [JSInvokable]
    public Task OnActivityEvent(ActivityEventEnvelope envelope)
    {
        var title = envelope.Data?["title"]?.GetValue<string?>()
            ?? envelope.Source
            ?? envelope.Type;

        MergeActivity(
            [
                new ActivityViewModel(
                    envelope.Sequence,
                    title,
                    envelope.Message ?? string.Empty,
                    envelope.Timestamp,
                    envelope.Data?["severity"]?.GetValue<string?>() ?? "Information",
                    envelope.Status,
                    envelope.Data?["failureReason"]?.GetValue<string?>(),
                    envelope.Data?["providerId"]?.GetValue<string?>(),
                    envelope.Data?["providerKind"]?.GetValue<string?>(),
                    envelope.Data?["modelId"]?.GetValue<string?>(),
                    envelope.Data?["exceptionType"]?.GetValue<string?>(),
                    envelope.Data?["exceptionMessage"]?.GetValue<string?>(),
                    envelope.Data?["exceptionDetails"]?.GetValue<string?>())
            ]);

        return InvokeAsync(StateHasChanged);
    }

    private void OnProviderChanged(ChangeEventArgs args)
    {
        SelectedProviderId = args.Value?.ToString();
        var provider = Providers.FirstOrDefault(item => item.Id == SelectedProviderId);
        SelectedModelId = provider?.Models.FirstOrDefault(model => model.IsDefault)?.Id
            ?? provider?.Models.FirstOrDefault()?.Id;
        ApiKeyInput = provider?.SavedApiKey ?? string.Empty;
        ApiKeyConfigured = !string.IsNullOrWhiteSpace(provider?.SavedApiKey);
    }

    private void MergeActivity(IEnumerable<ActivityViewModel> events)
    {
        foreach (var item in events.OrderBy(item => item.Sequence))
        {
            if (ActivityFeed.All(existing => existing.Sequence != item.Sequence))
            {
                ActivityFeed.Add(item);
            }
        }

        ActivityFeed.Sort((left, right) => right.Sequence.CompareTo(left.Sequence));
        if (ActivityFeed.Count > 80)
        {
            ActivityFeed.RemoveRange(80, ActivityFeed.Count - 80);
        }
    }

    private static ActivityViewModel ToViewModel(ActivityLogEntry entry) =>
        new(
            entry.Sequence,
            entry.Title,
            entry.Message,
            entry.TimestampUtc,
            entry.Severity.ToString(),
            entry.Status.ToString(),
            entry.Metadata?.TryGetValue("failureReason", out var failureReason) == true ? failureReason : null,
            entry.Metadata?.TryGetValue("providerId", out var providerId) == true ? providerId : null,
            entry.Metadata?.TryGetValue("providerKind", out var providerKind) == true ? providerKind : null,
            entry.Metadata?.TryGetValue("modelId", out var modelId) == true ? modelId : null,
            entry.Metadata?.TryGetValue("exceptionType", out var exceptionType) == true ? exceptionType : null,
            entry.Metadata?.TryGetValue("exceptionMessage", out var exceptionMessage) == true ? exceptionMessage : null,
            entry.Metadata?.TryGetValue("exceptionDetails", out var exceptionDetails) == true ? exceptionDetails : null);

    private string ResolveAuthor(string authorId)
    {
        return authorId switch
        {
            "user" => "You",
            "coordinator" => "Coordinator",
            _ => Agents.FirstOrDefault(agent => agent.Id == authorId)?.Name ?? authorId
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (_subscription is not null)
        {
            try
            {
                await _subscription.InvokeVoidAsync("stop");
                await _subscription.DisposeAsync();
            }
            catch
            {
            }
        }

        if (_module is not null)
        {
            await _module.DisposeAsync();
        }

        _objectReference?.Dispose();
    }

    private sealed record ActivityViewModel(
        long Sequence,
        string Title,
        string Message,
        DateTimeOffset Timestamp,
        string Severity,
        string? Status,
        string? Details,
        string? ProviderId,
        string? ProviderKind,
        string? ModelId,
        string? ExceptionType,
        string? ExceptionMessage,
        string? ExceptionDetails)
    {
        public bool IsError => string.Equals(Severity, "Error", StringComparison.OrdinalIgnoreCase);
    }
}
