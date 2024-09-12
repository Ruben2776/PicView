using System.Runtime.InteropServices;

namespace PicView.Windows.Taskbar;
public partial class TaskbarProgress
{
    public TaskbarProgress()
    {
        var hr = CoCreateInstance(ref _clsidTaskbarList, IntPtr.Zero, 1 /*CLSCTX_INPROC_SERVER*/, ref _iidITaskbarList3, out _taskbarInstance);
        if (hr != 0 || _taskbarInstance == null)
        {
            throw new InvalidOperationException("Failed to create TaskbarList COM object.");
        }

        // Initialize the taskbar instance
        _taskbarInstance.HrInit();
    }
    
    // CLSID for TaskbarList
    private static Guid _clsidTaskbarList = new("56FDF344-FD6D-11d0-958A-006097C9A090");

    // IID for ITaskbarList3
    private static Guid _iidITaskbarList3 = new("EA1AFB91-9E28-4B86-90E9-9E9F8A5EEFAF");

    [LibraryImport("ole32.dll", EntryPoint = "CoCreateInstance")]
    private static partial int CoCreateInstance(
        ref Guid clsid,
        IntPtr pUnkOuter,
        uint dwClsContext,
        ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out ITaskbarList3? ppv);

    private readonly ITaskbarList3? _taskbarInstance;

    public void SetProgress(IntPtr windowHandle, ulong progressValue, ulong progressMax)
    {
        _taskbarInstance.SetProgressState(windowHandle, TaskbarStates.Normal);
        _taskbarInstance.SetProgressValue(windowHandle, progressValue, progressMax);
    }
    
    public void StopProgress(IntPtr windowHandle)
    {
        _taskbarInstance.SetProgressState(windowHandle, TaskbarStates.NoProgress);
    }
}
