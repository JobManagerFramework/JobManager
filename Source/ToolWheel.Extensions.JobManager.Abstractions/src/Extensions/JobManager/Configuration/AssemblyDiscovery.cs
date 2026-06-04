using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyModel;

namespace ToolWheel.Extensions.JobManager.Configuration;

/// <summary>
/// Provides assembly discovery for <see cref="IAutoFeatureConfigurator"/> scanning.
/// </summary>
/// <remarks>
/// <para>
/// The primary source is <see cref="DependencyContext.Default"/>, which reads the
/// application's <c>.deps.json</c> file and therefore includes every runtime library –
/// even those that are deployed alongside the host but never directly referenced in
/// user code. This is the exact scenario that causes auto-discovery to silently miss
/// plug-in packages when only
/// <c>Assembly.GetEntryAssembly().GetReferencedAssemblies()</c> is used.
/// </para>
/// <para>
/// If <see cref="DependencyContext.Default"/> is <see langword="null"/> – for example
/// in single-file publish or heavily-trimmed builds where <c>.deps.json</c> is absent –
/// the method falls back to loading every <c>*.dll</c> found in
/// <see cref="AppContext.BaseDirectory"/>.
/// </para>
/// <para>
/// In both paths, assemblies that are already loaded into the current
/// <see cref="AppDomain"/> are returned immediately without a second load attempt.
/// </para>
/// </remarks>
public static class AssemblyDiscovery
{
    /// <summary>
    /// Returns all assemblies that are candidates for <see cref="IAutoFeatureConfigurator"/> discovery.
    /// </summary>
    /// <returns>
    /// A read-only list of <see cref="Assembly"/> instances. The list always contains
    /// every assembly that is currently loaded; additional assemblies are appended
    /// depending on the discovery strategy described in the class remarks.
    /// </returns>
    public static IReadOnlyList<Assembly> GetCandidateAssemblies()
    {
        var assemblies = new List<Assembly>(AppDomain.CurrentDomain.GetAssemblies());
        var seen = BuildSeenSet(assemblies);

        if (!TryLoadFromDependencyContext(assemblies, seen))
        {
            LoadFromBaseDirectory(assemblies, seen);
        }

        return assemblies;
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private static HashSet<string> BuildSeenSet(IEnumerable<Assembly> assemblies)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var assembly in assemblies)
        {
            var name = assembly.GetName().Name;
            if (name is not null)
            {
                seen.Add(name);
            }
        }

        return seen;
    }

    /// <summary>
    /// Attempts to load assemblies via <see cref="DependencyContext.Default"/>.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when <see cref="DependencyContext.Default"/> was available
    /// and used as the discovery source; <see langword="false"/> when it is
    /// <see langword="null"/> and the caller should fall back to base-directory probing.
    /// </returns>
    private static bool TryLoadFromDependencyContext(List<Assembly> assemblies, HashSet<string> seen)
    {
        var context = DependencyContext.Default;
        if (context is null)
        {
            return false;
        }

        foreach (var library in context.RuntimeLibraries)
        {
            foreach (var assemblyName in library.GetDefaultAssemblyNames(context))
            {
                if (assemblyName.Name is null || !seen.Add(assemblyName.Name))
                {
                    continue;
                }

                try
                {
                    assemblies.Add(Assembly.Load(assemblyName));
                }
                catch
                {
                    // Platform-specific, native or otherwise unloadable assemblies are skipped.
                }
            }
        }

        return true;
    }

    private static void LoadFromBaseDirectory(List<Assembly> assemblies, HashSet<string> seen)
    {
        try
        {
            foreach (var dllPath in Directory.GetFiles(AppContext.BaseDirectory, "*.dll"))
            {
                var dllName = Path.GetFileNameWithoutExtension(dllPath);
                if (!seen.Add(dllName))
                {
                    continue;
                }

                try
                {
                    assemblies.Add(AssemblyLoadContext.Default.LoadFromAssemblyPath(dllPath));
                }
                catch
                {
                    // Unloadable DLL (native, already loaded under a different path, etc.) – skip.
                }
            }
        }
        catch
        {
            // Cannot enumerate the base directory – ignore.
        }
    }
}
