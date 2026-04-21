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
    private List<ConversationMessage> Messages { get; set; } = [];
    private string ChatMessage { get; set; } = string.Empty;
    private string AgentDescription { get; set; } = string.Empty;
    private string WorkspacePathInput { get; set; } = string.Empty;
    private string ActiveWorkspacePath { get; set; } = string.Empty;
    private string ApiKeyInput { get; set; } = string.Empty;
    private string SetupFeedback { get; set; } = string.Empty;
    private string? ActiveConversationId { get; set; }
    private string? SelectedProviderId { get; set; }
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

    private bool CanApplyModel =>
        !IsModelBusy
        && !string.IsNullOrWhiteSpace(SelectedProviderId);

    private IEnumerable<string> WorkspaceSuggestions =>
        new[] { ActiveWorkspacePath, WorkspacePathInput }
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path.Trim())
            .Distinct(StringComparer.Ordinal);

    private string CurrentModelLabel =>
        Providers.FirstOrDefault(provider => provider.Id == SelectedProviderId)?.DisplayName ?? "None";

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
    }

    private async Task RefreshSnapshotAsync()
    {
        var snapshot = await Workbench.GetSnapshotAsync();
        Agents = snapshot.Agents.ToList();
        Providers = snapshot.Providers.ToList();
        ActiveWorkspacePath = snapshot.WorkspacePath;
        WorkspacePathInput = string.IsNullOrWhiteSpace(WorkspacePathInput)
            ? snapshot.WorkspacePath
            : WorkspacePathInput;
        SelectedProviderId = snapshot.ModelConfiguration.SelectedProviderId;
        ApiKeyConfigured = snapshot.ModelConfiguration.ApiKeyConfigured;
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
                new GlobalModelConfigurationRequest(SelectedProviderId!, ApiKeyInput));
            SelectedProviderId = configuration.SelectedProviderId;
            ApiKeyConfigured = configuration.ApiKeyConfigured;
            SetupFeedback = "Global model settings updated for this app session.";
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
                    envelope.Timestamp)
            ]);

        return InvokeAsync(StateHasChanged);
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
        new(entry.Sequence, entry.Title, entry.Message, entry.TimestampUtc);

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

    private sealed record ActivityViewModel(long Sequence, string Title, string Message, DateTimeOffset Timestamp);
}
