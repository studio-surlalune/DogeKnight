public sealed class GlobalConfig
{
    public static readonly DevHud.Layout DevHudLayout = DevHud.Layout.BottomLeft;

    /// For dynamic resolution control.
    public static readonly float TargetFrameTimeMS = 32f; // 32 milliseconds

    /// For dynamic resolution control.
    public static readonly float TargetFrameDeltaMS = 1f; // 1 millisecond
    
}
