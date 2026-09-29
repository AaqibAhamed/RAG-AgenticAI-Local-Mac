using Microsoft.SemanticKernel;

namespace RAGOnMyMac.Agents;

public sealed class SemanticKernelAgentOrchestrator
{
  private readonly Kernel _kernel = Kernel.CreateBuilder().Build();

  public async Task<TResult> ExecuteAsync<TResult>(
      Func<Task<TResult>> workflow,
      CancellationToken cancellationToken = default)
  {
    var function = KernelFunctionFactory.CreateFromMethod(
        async () => await workflow(),
        "executeAgentWorkflow",
        "Runs the typed developer knowledge agent workflow.");

    var result = await _kernel.InvokeAsync(
        function,
        cancellationToken: cancellationToken);

    return result.GetValue<TResult>()
        ?? throw new InvalidOperationException(
            "Semantic Kernel returned no workflow result.");
  }
}