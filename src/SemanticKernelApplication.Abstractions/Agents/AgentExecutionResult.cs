using System.Text.Json.Serialization;

namespace SemanticKernelApplication.Abstractions.Agents;

/// <summary>
/// Describes the outcome of an agent execution attempt.
/// </summary>
public sealed class AgentExecutionResult
{
    [JsonConstructor]
    private AgentExecutionResult(
        string operationId,
        AgentExecutionStatus status,
        string? output,
        string? summary,
        IReadOnlyDictionary<string, string>? metadata,
        DateTimeOffset? startedAtUtc,
        DateTimeOffset? completedAtUtc,
        string? failureReason)
    {
        OperationId = operationId;
        Status = status;
        Output = output;
        Summary = summary;
        Metadata = metadata;
        StartedAtUtc = startedAtUtc;
        CompletedAtUtc = completedAtUtc;
        FailureReason = failureReason;
    }

    public static AgentExecutionResultBuilder Builder => new();

    public string OperationId { get; }
    public AgentExecutionStatus Status { get; }
    public string? Output { get; }
    public string? Summary { get; }
    public IReadOnlyDictionary<string, string>? Metadata { get; }
    public DateTimeOffset? StartedAtUtc { get; }
    public DateTimeOffset? CompletedAtUtc { get; }
    public string? FailureReason { get; }

    public sealed class AgentExecutionResultBuilder
    {
        private string? _operationId;
        private AgentExecutionStatus? _status;
        private string? _output;
        private string? _summary;
        private IReadOnlyDictionary<string, string>? _metadata;
        private DateTimeOffset? _startedAtUtc;
        private DateTimeOffset? _completedAtUtc;
        private string? _failureReason;

        public AgentExecutionResultBuilder WithOperationId(string operationId) { _operationId = operationId; return this; }
        public AgentExecutionResultBuilder WithStatus(AgentExecutionStatus status) { _status = status; return this; }
        public AgentExecutionResultBuilder WithOutput(string? output) { _output = output; return this; }
        public AgentExecutionResultBuilder WithSummary(string? summary) { _summary = summary; return this; }
        public AgentExecutionResultBuilder WithMetadata(IReadOnlyDictionary<string, string>? metadata) { _metadata = metadata; return this; }
        public AgentExecutionResultBuilder WithStartedAtUtc(DateTimeOffset? startedAtUtc) { _startedAtUtc = startedAtUtc; return this; }
        public AgentExecutionResultBuilder WithCompletedAtUtc(DateTimeOffset? completedAtUtc) { _completedAtUtc = completedAtUtc; return this; }
        public AgentExecutionResultBuilder WithFailureReason(string? failureReason) { _failureReason = failureReason; return this; }

        public AgentExecutionResult Build()
        {
            if (string.IsNullOrWhiteSpace(_operationId)) throw new InvalidOperationException("Operation id is required.");
            if (_status is null) throw new InvalidOperationException("Execution status is required.");

            return new AgentExecutionResult(_operationId, _status.Value, _output, _summary, _metadata, _startedAtUtc, _completedAtUtc, _failureReason);
        }
    }
}
