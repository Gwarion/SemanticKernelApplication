using SemanticKernelApplication.Abstractions.Activities;
using SemanticKernelApplication.Abstractions.Conversations;

namespace SemanticKernelApplication.Abstractions.Orchestration;

/// <summary>
/// Describes the outcome of a completed coordination workflow.
/// </summary>
public sealed class CoordinationResult
{
    private CoordinationResult(
        Guid operationId,
        ActivityStatus status,
        ConversationThread thread,
        IReadOnlyList<CoordinationRound> rounds,
        string? summary,
        string? completionReason,
        UsageSnapshot? usage,
        IReadOnlyDictionary<string, string>? metadata)
    {
        OperationId = operationId;
        Status = status;
        Thread = thread;
        Rounds = rounds;
        Summary = summary;
        CompletionReason = completionReason;
        Usage = usage;
        Metadata = metadata;
    }

    /// <summary>
    /// Gets a new builder used to create a <see cref="CoordinationResult"/>.
    /// </summary>
    public static CoordinationResultBuilder Builder => new();

    /// <summary>
    /// Gets the unique identifier for the coordination run.
    /// </summary>
    public Guid OperationId { get; }

    /// <summary>
    /// Gets the final activity status of the run.
    /// </summary>
    public ActivityStatus Status { get; }

    /// <summary>
    /// Gets the updated conversation thread after execution.
    /// </summary>
    public ConversationThread Thread { get; }

    /// <summary>
    /// Gets the rounds completed during the run.
    /// </summary>
    public IReadOnlyList<CoordinationRound> Rounds { get; }

    /// <summary>
    /// Gets the optional user-facing summary of the result.
    /// </summary>
    public string? Summary { get; }

    /// <summary>
    /// Gets the optional reason the workflow stopped.
    /// </summary>
    public string? CompletionReason { get; }

    /// <summary>
    /// Gets optional aggregate usage metrics.
    /// </summary>
    public UsageSnapshot? Usage { get; }

    /// <summary>
    /// Gets optional run metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; }

    /// <summary>
    /// Builds <see cref="CoordinationResult"/> instances while keeping call sites readable.
    /// </summary>
    public sealed class CoordinationResultBuilder
    {
        private Guid? _operationId;
        private ActivityStatus? _status;
        private ConversationThread? _thread;
        private IReadOnlyList<CoordinationRound>? _rounds;
        private string? _summary;
        private string? _completionReason;
        private UsageSnapshot? _usage;
        private IReadOnlyDictionary<string, string>? _metadata;

        /// <summary>
        /// Sets the unique identifier for the coordination run.
        /// </summary>
        public CoordinationResultBuilder WithOperationId(Guid operationId)
        {
            _operationId = operationId;
            return this;
        }

        /// <summary>
        /// Sets the final activity status of the run.
        /// </summary>
        public CoordinationResultBuilder WithStatus(ActivityStatus status)
        {
            _status = status;
            return this;
        }

        /// <summary>
        /// Sets the updated conversation thread after execution.
        /// </summary>
        public CoordinationResultBuilder WithThread(ConversationThread thread)
        {
            _thread = thread;
            return this;
        }

        /// <summary>
        /// Sets the rounds completed during the run.
        /// </summary>
        public CoordinationResultBuilder WithRounds(IReadOnlyList<CoordinationRound> rounds)
        {
            _rounds = rounds;
            return this;
        }

        /// <summary>
        /// Sets an optional user-facing summary of the result.
        /// </summary>
        public CoordinationResultBuilder WithSummary(string? summary)
        {
            _summary = summary;
            return this;
        }

        /// <summary>
        /// Sets an optional reason the workflow stopped.
        /// </summary>
        public CoordinationResultBuilder WithCompletionReason(string? completionReason)
        {
            _completionReason = completionReason;
            return this;
        }

        /// <summary>
        /// Sets optional aggregate usage metrics.
        /// </summary>
        public CoordinationResultBuilder WithUsage(UsageSnapshot? usage)
        {
            _usage = usage;
            return this;
        }

        /// <summary>
        /// Sets optional run metadata.
        /// </summary>
        public CoordinationResultBuilder WithMetadata(IReadOnlyDictionary<string, string>? metadata)
        {
            _metadata = metadata;
            return this;
        }

        /// <summary>
        /// Validates the configured values and creates the final result.
        /// </summary>
        public CoordinationResult Build()
        {
            if (_operationId is null || _operationId == Guid.Empty)
            {
                throw new InvalidOperationException("Coordination operation id is required.");
            }

            if (_status is null)
            {
                throw new InvalidOperationException("Coordination status is required.");
            }

            if (_thread is null)
            {
                throw new InvalidOperationException("Coordination thread is required.");
            }

            if (_rounds is null)
            {
                throw new InvalidOperationException("Coordination rounds are required.");
            }

            return new CoordinationResult(
                _operationId.Value,
                _status.Value,
                _thread,
                _rounds,
                _summary,
                _completionReason,
                _usage,
                _metadata);
        }
    }
}
