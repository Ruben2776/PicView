using System.Runtime.InteropServices;

namespace PicView.WindowsNT.Taskbar;
public partial class TaskbarProgress
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TaskbarProgress"/> class for the specified window handle.
    /// </summary>
    /// <param name="windowHandle">The handle of the window for which the taskbar progress is controlled.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the TaskbarList COM object cannot be created.
    /// </exception>
    public TaskbarProgress(IntPtr windowHandle)
    {
        var hr = CoCreateInstance(ref _clsidTaskbarList, IntPtr.Zero, 1 /*CLSCTX_INPROC_SERVER*/, ref _iidITaskbarList3, out _taskbarInstance);
        if (hr != 0 || _taskbarInstance == null)
        {
            throw new InvalidOperationException("Failed to create TaskbarList COM object.");
        }

        // Initialize the taskbar instance
        _taskbarInstance.HrInit();
            
        _windowHandle = windowHandle;
    }
    
    private static IntPtr _windowHandle;
    
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

    /// <summary>
    /// Updates the taskbar progress for the associated window.
    /// </summary>
    /// <param name="progressValue">The current progress value (completed).</param>
    /// <param name="progressMax">The maximum progress value (total).</param>
    /// <remarks>
    /// This method sets the taskbar progress state to normal and updates the progress value.
    /// </remarks>
    public void SetProgress(ulong progressValue, ulong progressMax)
    {
        _taskbarInstance.SetProgressState(_windowHandle, TaskbarStates.Normal);
        _taskbarInstance.SetProgressValue(_windowHandle, progressValue, progressMax);
    }
        
    /// <summary>
    /// Stops and clears the taskbar progress for the associated window.
    /// </summary>
    /// <remarks>
    /// This method sets the progress value to 0 and resets the taskbar progress state to indicate no progress.
    /// </remarks>
    public void StopProgress()
    {
        _taskbarInstance.SetProgressValue(_windowHandle, 0, 0);
        _taskbarInstance.SetProgressState(_windowHandle, TaskbarStates.NoProgress);
    }
}
