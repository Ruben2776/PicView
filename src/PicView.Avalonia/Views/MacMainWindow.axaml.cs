using Avalonia.Controls;
using System;

namespace PicView.Avalonia.Views;

public partial class MacMainWindow : Window
{
    public MacMainWindow()
    {
        InitializeComponent();
        Models.LoadSettings.StartLoading();
    }
}