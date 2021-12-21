using Avalonia.Input;
using PicView.ViewModels;

namespace PicView.Shortcuts;

public static class MainShortcuts
{
    public static void HandleKeyDown(MainWindowViewModel? vm, KeyEventArgs e)
    {
        if (vm is null) { return; }
        
        switch (e.Key)
        {
            case Key.Escape:
                vm.ExitCommand?.Execute(null);
                break;
            case Key.D:
            case Key.Right:
                if (false) // TODO figure out when key is held down
                {
                    // TODO re-implement timer navigation
                }
                else if (e.KeyModifiers == KeyModifiers.Control)
                {
                    vm.Last.Execute();
                }
                else
                {
                    vm.Next.Execute();    
                }
                break;
            
            case Key.A:
            case Key.Left:
                if (false) // TODO figure out when key is held down
                {
                    // TODO re-implement timer navigation
                }
                else if (e.KeyModifiers == KeyModifiers.Control)
                {
                    vm.First.Execute();
                }
                else
                {
                    vm.Prev.Execute();    
                }
                break;
        }
    }
    
    public static void HandleKeyUp(MainWindowViewModel? vm, KeyEventArgs e)
    {
        if (vm is null) { return; }
        
        // TODO re-implement timer navigation
    }

    public static void HandlePointerWheel(MainWindowViewModel? vm, PointerWheelEventArgs e)
    {
        if (vm is null) { return; }
        
        if (e.Delta.Y > 0)
        {
            vm.Prev.Execute();
        }
        else
        {
            vm.Next.Execute();
        }
    }
}