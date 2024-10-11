using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using PicView.Core.Localization;

namespace PicView.Avalonia.CustomControls
{
    public class FuncTextBox : TextBox
    {
        public FuncTextBox()
        {
            if (!Application.Current.TryGetResource("MainTextColor", ThemeVariant.Default, out var mainTextColor))
            {
                return;
            }

            var iconBrush = new SolidColorBrush((Color)(mainTextColor ?? Brushes.White));
            if (!Application.Current.TryGetResource("CopyGeometry", ThemeVariant.Default, out var copyGeometry))
            {
                return;
            }

            if (!Application.Current.TryGetResource("CutGeometry", ThemeVariant.Default, out var cutGeometry))
            {
                return;
            }

            if (!Application.Current.TryGetResource("RecycleGeometry", ThemeVariant.Default, out var recycleGeometry))
            {
                return;
            }

            if (!Application.Current.TryGetResource("PasteGeometry", ThemeVariant.Default, out var pasteGeometry))
            {
                return;
            }

            if (!Application.Current.TryGetResource("CheckboxOutlineImage", ThemeVariant.Default,
                    out var checkboxOutlineImage))
            {
                return;
            }

            var contextMenu = new ContextMenu();
            var selectAllMenuItem = new MenuItem
            {
                Header = TranslationHelper.GetTranslation("SelectAll"), // TODO: Add localization
                Icon = new Image
                {
                    Width = 12,
                    Height = 12,
                    Source = checkboxOutlineImage as DrawingImage ?? null
                }
            };
            selectAllMenuItem.Click += (_, _) => SelectAll();
            contextMenu.Items.Add(selectAllMenuItem);

            var cutMenuItem = new MenuItem
            {
                Header = TranslationHelper.GetTranslation("Cut"), // TODO: "Cut file" should be replaced with "Cut"
                Icon = new PathIcon
                {
                    Width = 12,
                    Height = 12,
                    Foreground = iconBrush,
                    Data = cutGeometry as Geometry ?? null
                }
            };
            cutMenuItem.Click += (_, _) => Cut();
            contextMenu.Items.Add(cutMenuItem);

            var copyMenuItem = new MenuItem
            {
                Header = TranslationHelper.Translation.Copy,
                Icon = new PathIcon
                {
                    Width = 12,
                    Height = 12,
                    Foreground = iconBrush,
                    Data = copyGeometry as Geometry ?? null
                }
            };
            copyMenuItem.Click += (_, _) => Copy();
            contextMenu.Items.Add(copyMenuItem);

            var pasteMenuItem = new MenuItem
            {
                Header = TranslationHelper
                    .GetTranslation("Paste"), // TODO: "Paste file" should be replaced with "Paste"
                Icon = new PathIcon
                {
                    Width = 12,
                    Height = 12,
                    Foreground = iconBrush,
                    Data = pasteGeometry as Geometry ?? null
                }
            };
            pasteMenuItem.Click += (_, _) => Paste();
            contextMenu.Items.Add(pasteMenuItem);

            var deleteMenuItem = new MenuItem
            {
                Header = TranslationHelper.GetTranslation("Delete"),
                Icon = new PathIcon
                {
                    Width = 12,
                    Height = 12,
                    Foreground = iconBrush,
                    Data = recycleGeometry as Geometry ?? null
                }
            };
            deleteMenuItem.Click += (_, _) => Clear();
            contextMenu.Items.Add(deleteMenuItem);

            contextMenu.Opened += delegate
            {
                if (IsReadOnly)
                {
                    deleteMenuItem.IsEnabled = false;
                    cutMenuItem.IsEnabled = false;
                    pasteMenuItem.IsEnabled = false;
                }
                else
                {
                    deleteMenuItem.IsEnabled = true;
                    cutMenuItem.IsEnabled = true;
                    pasteMenuItem.IsEnabled = true;
                }
            };

            ContextMenu = contextMenu;

            PointerPressed += (_, e) =>
            {
                if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
                {
                    contextMenu.Open(this);
                }
            };
        }

        protected override Type StyleKeyOverride => typeof(TextBox);
    }
}