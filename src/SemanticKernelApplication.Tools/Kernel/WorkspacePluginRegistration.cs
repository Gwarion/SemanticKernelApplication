namespace SemanticKernelApplication.Tools.Kernel;

/// <summary>
/// Describes a plugin instance that should be registered with a kernel.
/// </summary>
/// <param name="Name">Plugin name used inside the kernel.</param>
/// <param name="Instance">Plugin object containing callable methods.</param>
public sealed record WorkspacePluginRegistration(string Name, object Instance);
