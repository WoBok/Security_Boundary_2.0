public abstract class SecurityBoundaryVST
{
    public static SecurityBoundaryVST Instance;
    public static bool needSwitchVST = true;
    public static void SecurityBoundaryVSTBinder<T>() where T : SecurityBoundaryVST, new()
    {
        Instance = new T();
    }
    public static void SwitchVSTState(bool state)
    {
        if (Instance != null) { Instance.SwitchVSTStateHandle(state); }
    }
    public static void SwitchCameraState(bool state)
    {
        if (Instance != null) { Instance.SwitchCameraStateHandle(state); }
    }
    protected abstract void SwitchVSTStateHandle(bool state);
    protected abstract void SwitchCameraStateHandle(bool state);
}