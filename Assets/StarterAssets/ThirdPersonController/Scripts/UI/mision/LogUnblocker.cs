// LogUnblocker.cs (»õ ÆÄÀÏ)
using UnityEngine;

public static class LogUnblocker
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    static void EnsureLogs() { Debug.unityLogger.logEnabled = true; }
}
