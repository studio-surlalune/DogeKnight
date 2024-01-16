public sealed class GlobalConfig
{
    public static readonly DevHud.Layout DevHudLayout = DevHud.Layout.BottomLeft;

    /// For dynamic resolution control.
    public static readonly float TargetFrameTimeMS = 0.032f; // 32 milliseconds

    /// For dynamic resolution control.
    public static readonly float TargetFrameDeltaMS = 0.001f; // 1 millisecond
    
}
