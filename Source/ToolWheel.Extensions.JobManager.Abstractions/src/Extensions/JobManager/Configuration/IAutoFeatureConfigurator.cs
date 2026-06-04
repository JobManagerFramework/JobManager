namespace ToolWheel.Extensions.JobManager.Configuration;

/// <summary>
/// Defines a contract for automatically configuring job manager features using the provided configuration builder.
/// </summary>
/// <remarks>Implementations of this interface can be used to apply feature-specific configuration logic to an
/// IJobManagerConfigurationBuilder instance. This is typically used to enable or customize features during job manager
/// setup in a modular or extensible manner.</remarks>
public interface IAutoFeatureConfigurator
{
    /// <summary>
    /// Configures the specified job manager builder with default or recommended settings.
    /// </summary>
    /// <param name="builder">The job manager configuration builder to be configured. Cannot be null.</param>
    void AutoConfigure(IJobManagerConfigurationBuilder builder);
}
