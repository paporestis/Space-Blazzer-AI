using UnityEditor;

[InitializeOnLoad]
internal static class ShipAgentPlayModeRecorder
{
    static ShipAgentPlayModeRecorder()
    {
        ShipAgent.ModelInferenceAgentDied -= StopPlayMode;
        ShipAgent.ModelInferenceAgentDied += StopPlayMode;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void StopPlayMode()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
        }
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            ShipAgent.SavePlaySessionReport();
        }
    }
}
