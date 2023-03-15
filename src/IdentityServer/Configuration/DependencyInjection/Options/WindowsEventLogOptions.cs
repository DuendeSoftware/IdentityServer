using System.Reflection;

namespace Duende.IdentityServer.Configuration;
/// <summary>
/// Configures windows event log 
/// </summary>
public class WindowsEventLogOptions
{
    /// <summary>
    /// The source of the event log
    /// </summary>
    public string Source { get; set; } = Assembly.GetExecutingAssembly()?.GetName()?.Name;
}