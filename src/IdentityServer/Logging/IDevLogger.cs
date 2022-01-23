namespace Duende.IdentityServer.Logging;

#pragma warning disable 1591

public interface IDevLogger<T>
{
    void DevLogDebug(string message);
    void DevLogDebug<T0>(string message, T0 arg0);
    void DevLogDebug<T0, T1>(string message, T0 arg0, T1 arg1);
    void DevLogDebug<T0, T1, T2>(string message, T0 arg0, T1 arg1, T2 arg2);
    void DevLogDebug<T0, T1, T2, T3>(string message, T0 arg0, T1 arg1, T2 arg2, T3 arg3);
    
    void DevLogTrace(string message);
    void DevLogTrace<T0>(string message, T0 arg0);
    void DevLogTrace<T0, T1>(string message, T0 arg0, T1 arg1);
    void DevLogTrace<T0, T1, T2>(string message, T0 arg0, T1 arg1, T2 arg2);
    void DevLogTrace<T0, T1, T2, T3>(string message, T0 arg0, T1 arg1, T2 arg2, T3 arg3);
}