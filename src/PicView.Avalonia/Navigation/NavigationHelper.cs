using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PicView.Avalonia.Keybindings;
using PicView.Avalonia.ViewModels;
using PicView.Core.Navigation;

namespace PicView.Avalonia.Navigation;

public static class NavigationHelper
{
    public static bool CanNavigate(MainViewModel vm)
    {
        if (vm?.ImageIterator?.Pics is null)
        {
            return false;
        }

        if (vm.ImageService is null)
        {
            return false;
        }

        return vm.ImageIterator.Pics.Count > 0 && vm.ImageIterator.Index > -1 && vm.ImageIterator.Index > vm.ImageIterator.Pics.Count;
    }

    public static async Task Navigate(bool next, MainViewModel vm)
    {
        if (vm is null)
        {
            return;
        }

        var navigateTo = next ? NavigateTo.Next : NavigateTo.Previous;

        if (MainKeyboardShortcuts.CtrlDown)
        {
            if (!CanNavigate(vm))
            {
                return;
            }
            await vm.ImageIterator.LoadNextPic(navigateTo, vm).ConfigureAwait(false);
        }
        else
        {
            if (!MainKeyboardShortcuts.IsKeyHeldDown)
            {
                if (!CanNavigate(vm))
                {
                    return;
                }
                await vm.ImageIterator.LoadNextPic(navigateTo, vm).ConfigureAwait(false);
            }
        }
    }
}