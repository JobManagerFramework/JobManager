using System;
using System.Threading;
using System.Threading.Tasks;

namespace ToolWheel.Extensions.JobManager.Middleware
{
    /// <summary>
    /// Middleware component invoked during job execution pipeline.
    /// Implementations can inspect or modify the <see cref="IJobTaskContextBuilder"/>, perform work before/after the next delegate,
    /// and honor the provided <see cref="CancellationToken"/>.
    /// </summary>
    public interface IExecutionMiddleware
    {
        /// <summary>
        /// Invokes the middleware logic.
        /// </summary>
        /// <param name="context">A builder that provides the execution context for the current job task.</param>
        /// <param name="next">A delegate representing the next middleware or the final execution step. Middleware should await this to continue the pipeline.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that should be observed to support cancellation.</param>
        /// <returns>A <see cref="Task"/> that completes when the middleware and the rest of the pipeline have finished executing.</returns>
        Task InvokeAsync(IJobTaskContextBuilder context, Func<Task> next, CancellationToken cancellationToken);
    }
}
