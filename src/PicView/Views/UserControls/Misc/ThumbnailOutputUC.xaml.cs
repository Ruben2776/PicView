﻿using PicView.FileHandling;
using PicView.UILogic;
using PicView.Views.Windows;

namespace PicView.Views.UserControls.Misc;

// ReSharper disable once InconsistentNaming
public partial class ThumbnailOutputUC
{
    public ThumbnailOutputUC(int i, string folderPath, string filename, string value)
    {
        InitializeComponent();

        OutPutString.Text = $"Thumb {i}";
        OutPutStringBox.Text = folderPath + @"\" + filename;
        ValueBox.Text = value;

        OutputFolderButton.FileMenuButton.Click += (_, _) =>
        {
            var newFolder = OpenSave.SelectAndReturnFolder();
            if (string.IsNullOrWhiteSpace(newFolder) == false)
            {
                OutPutStringBox.Text = newFolder;
            }
            ConfigureWindows.GetResizeWindow.Focus();
        };

        ResizeWindow.SetTextBoxDragEvent(OutPutStringBox);
    }
}