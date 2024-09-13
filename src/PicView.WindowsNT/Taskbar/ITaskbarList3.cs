using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace PicView.WindowsNT.Taskbar;

// ITaskbarList3 interface using P/Invoke
[GeneratedComInterface]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("EA1AFB91-9E28-4B86-90E9-9E9F8A5EEFAF")]
internal partial interface ITaskbarList3
{
    // ITaskbarList
    [PreserveSig]
    void HrInit();
    [PreserveSig]
    void AddTab(IntPtr hwnd);
    [PreserveSig]
    void DeleteTab(IntPtr hwnd);
    [PreserveSig]
    void ActivateTab(IntPtr hwnd);
    [PreserveSig]
    void SetActiveAlt(IntPtr hwnd);

    // ITaskbarList2
    [PreserveSig]
    void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);

    // ITaskbarList3
    [PreserveSig]
    void SetProgressValue(IntPtr hwnd, ulong ullCompleted, ulong ullTotal);
    [PreserveSig]
    void SetProgressState(IntPtr hwnd, TaskbarStates state);
}