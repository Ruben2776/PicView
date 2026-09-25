using Avalonia.Controls;
using PicView.Avalonia.CustomControls;

namespace PicView.Avalonia.Input;

public record KeybindCategoryGroup(
    TextBlock Header,
    ItemsControl Container,
    List<(KeybindBox Box, string FunctionName)> Entries);