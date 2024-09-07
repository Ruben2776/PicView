using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Threading;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views;
using PicView.Avalonia.Views.UC;
using PicView.Avalonia.Views.UC.Menus;
using PicView.Core.Config;
using PicView.Core.Localization;

namespace PicView.Avalonia.UI
{
    public static class UIHelper
    {
        #region Add Controls

        public static void AddToolTipMessage()
        {
            var mainView = GetMainView;
            var toolTipMessage = new ToolTipMessage
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Opacity = 0
            };

            mainView.MainGrid.Children.Add(toolTipMessage);
        }

        #endregion

        #region GetControls

        public static MainView? GetMainView { get; private set; }
        public static Control? GetTitlebar { get; private set; }
        public static EditableTitlebar? GetEditableTitlebar { get; private set; }
        public static GalleryAnimationControlView? GetGalleryView { get; private set; }

        public static BottomBar? GetBottomBar { get; private set; }

        public static void SetControls(IClassicDesktopStyleApplicationLifetime desktop)
        {
            GetMainView = desktop.MainWindow.FindControl<MainView>("MainView");
            GetTitlebar = desktop.MainWindow.FindControl<Control>("Titlebar");
            GetEditableTitlebar = GetTitlebar.FindControl<EditableTitlebar>("EditableTitlebar");
            GetGalleryView = GetMainView.MainGrid.GetControl<GalleryAnimationControlView>("GalleryView");
            GetBottomBar = desktop.MainWindow.FindControl<BottomBar>("BottomBar");
        }

        #endregion

        #region Menus

        public static void AddMenus()
        {
            var mainView = GetMainView;
            var fileMenu = new FileMenu
            {
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 120, 0),
                IsVisible = false
            };

            mainView.MainGrid.Children.Add(fileMenu);

            var imageMenu = new ImageMenu
            {
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 63, 0),
                IsVisible = false
            };

            mainView.MainGrid.Children.Add(imageMenu);

            var settingsMenu = new SettingsMenu
            {
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, -75, 0),
                IsVisible = false
            };

            mainView.MainGrid.Children.Add(settingsMenu);

            var toolsMenu = new ToolsMenu
            {
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(80, 0, 0, 0),
                IsVisible = false
            };

            mainView.MainGrid.Children.Add(toolsMenu);
        }

        public static void CloseMenus(MainViewModel vm)
        {
            vm.IsFileMenuVisible = false;
            vm.IsImageMenuVisible = false;
            vm.IsSettingsMenuVisible = false;
            vm.IsToolsMenuVisible = false;
        }

        public static bool IsAnyMenuOpen(MainViewModel vm)
        {
            return vm.IsFileMenuVisible || vm.IsImageMenuVisible || vm.IsSettingsMenuVisible || vm.IsToolsMenuVisible;
        }

        public static void ToggleFileMenu(MainViewModel vm)
        {
            vm.IsFileMenuVisible = !vm.IsFileMenuVisible;
            vm.IsImageMenuVisible = false;
            vm.IsSettingsMenuVisible = false;
            vm.IsToolsMenuVisible = false;
        }

        public static void ToggleImageMenu(MainViewModel vm)
        {
            vm.IsFileMenuVisible = false;
            vm.IsImageMenuVisible = !vm.IsImageMenuVisible;
            vm.IsSettingsMenuVisible = false;
            vm.IsToolsMenuVisible = false;
        }

        public static void ToggleSettingsMenu(MainViewModel vm)
        {
            vm.IsFileMenuVisible = false;
            vm.IsImageMenuVisible = false;
            vm.IsSettingsMenuVisible = !vm.IsSettingsMenuVisible;
            vm.IsToolsMenuVisible = false;
        }

        public static void ToggleToolsMenu(MainViewModel vm)
        {
            vm.IsFileMenuVisible = false;
            vm.IsImageMenuVisible = false;
            vm.IsSettingsMenuVisible = false;
            vm.IsToolsMenuVisible = !vm.IsToolsMenuVisible;
        }

        #endregion Menus

        #region Settings

        public static async Task SideBySide(MainViewModel vm)
        {
            if (vm is null)
            {
                return;
            }

            if (SettingsHelper.Settings.ImageScaling.ShowImageSideBySide)
            {
                SettingsHelper.Settings.ImageScaling.ShowImageSideBySide = false;
                vm.IsShowingSideBySide = false;
                vm.SecondaryImageSource = null;
                WindowHelper.SetSize(vm);
            }
            else
            {
                SettingsHelper.Settings.ImageScaling.ShowImageSideBySide = true;
                vm.IsShowingSideBySide = true;
                if (NavigationHelper.CanNavigate(vm))
                {
                    var preloadValue = await vm.ImageIterator?.GetNextPreLoadValueAsync();
                    vm.SecondaryImageSource = preloadValue?.ImageModel.Image;
                    WindowHelper.SetSize(vm.ImageWidth, vm.ImageHeight, preloadValue.ImageModel.PixelWidth,
                        preloadValue.ImageModel.PixelHeight, vm.RotationAngle, vm);
                }
            }
            await SettingsHelper.SaveSettingsAsync();
        }

        public static void ToggleScroll(MainViewModel vm)
        {
            if (SettingsHelper.Settings.Zoom.ScrollEnabled)
            {
                vm.ToggleScrollBarVisibility = ScrollBarVisibility.Disabled;
                vm.GetScrolling = TranslationHelper.Translation.ScrollingDisabled;
                vm.IsScrollingEnabled = false;
                SettingsHelper.Settings.Zoom.ScrollEnabled = false;
            }
            else
            {
                vm.ToggleScrollBarVisibility = ScrollBarVisibility.Visible;
                vm.GetScrolling = TranslationHelper.Translation.ScrollingEnabled;
                vm.IsScrollingEnabled = true;
                SettingsHelper.Settings.Zoom.ScrollEnabled = true;
            }

            WindowHelper.SetSize(vm);
        }

        public static async Task Flip(MainViewModel vm)
        {
            if (!NavigationHelper.CanNavigate(vm))
            {
                return;
            }

            if (vm.ScaleX == 1)
            {
                vm.ScaleX = -1;
                vm.GetFlipped = vm.UnFlip;
            }
            else
            {
                vm.ScaleX = 1;
                vm.GetFlipped = vm.Flip;
            }

            await Dispatcher.UIThread.InvokeAsync(() => { vm.ImageViewer.Flip(true); });
        }

        public static async Task ToggleLooping(MainViewModel vm)
        {
            var value = !SettingsHelper.Settings.UIProperties.Looping;
            SettingsHelper.Settings.UIProperties.Looping = value;
            vm.GetLooping = value
                ? TranslationHelper.Translation.LoopingEnabled
                : TranslationHelper.Translation.LoopingDisabled;
            vm.IsLooping = value;

            var msg = value
                ? TranslationHelper.Translation.LoopingEnabled
                : TranslationHelper.Translation.LoopingDisabled;
            await TooltipHelper.ShowTooltipMessageAsync(msg);

            await SettingsHelper.SaveSettingsAsync();
        }

        public static async Task ChangeCtrlZoom(MainViewModel vm)
        {
            SettingsHelper.Settings.Zoom.CtrlZoom = !SettingsHelper.Settings.Zoom.CtrlZoom;
            vm.GetCtrlZoom = SettingsHelper.Settings.Zoom.CtrlZoom
                ? TranslationHelper.Translation.CtrlToZoom
                : TranslationHelper.Translation.ScrollToZoom;
            await SettingsHelper.SaveSettingsAsync().ConfigureAwait(false);
        }

        public static async Task ToggleSubdirectories(MainViewModel vm)
        {
            if (SettingsHelper.Settings.Sorting.IncludeSubDirectories)
            {
                vm.IsIncludingSubdirectories = false;
                SettingsHelper.Settings.Sorting.IncludeSubDirectories = false;
            }
            else
            {
                vm.IsIncludingSubdirectories = true;
                SettingsHelper.Settings.Sorting.IncludeSubDirectories = true;
            }

            await vm.ImageIterator.ReloadFileList();
            SetTitleHelper.SetTitle(vm);
            await SettingsHelper.SaveSettingsAsync();
        }

        #endregion
    }
}