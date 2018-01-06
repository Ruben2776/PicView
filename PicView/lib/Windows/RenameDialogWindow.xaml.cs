using PicView.lib;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static PicView.lib.Variables;

namespace PicView.Windows
{
    public partial class RenameialogWindow : Window
    {
        string RbtnName;
        public string NameForRename { get { return TxtBox.Text; } }
        public string Counter2 { get; set; }
        public int NewPicWidth { get; set; }
        public int NewPicHeight { get; set; }
        public string ChosenRbtn { get { return RbtnName; } }

        public RenameialogWindow()
        {
            InitializeComponent();
            var filename = Path.GetFileName(PicPath);
            RenameLabel.Text += filename + " to: ";
            TxtBox.Text = filename;
            TxtBox.SelectAll();
            TxtBox.Focus();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            CloseButton.Click += (s, x) => Close();

            CloseButton.MouseEnter += (s, x) =>
            {
                AnimationHelper.MouseEnterColorEvent(0, 0, 0, 0, CloseButtonBrush, true);
            };

            CloseButton.MouseLeave += (s, x) =>
            {
                AnimationHelper.MouseLeaveColorEvent(0, 0, 0, 0, CloseButtonBrush, true);
            };

            CloseButton.PreviewMouseLeftButtonDown += (s, x) =>
            {
                AnimationHelper.PreviewMouseLeftButtonDownColorEvent(CloseButtonBrush, true);
            };

            ApplyButton.Click += (s, x) => Apply(s, x);

            ApplyButton.MouseEnter += (s, x) =>
            {
                AnimationHelper.MouseEnterColorEvent(0, 0, 0, 0, ApplyButtonBrush, false);
            };

            ApplyButton.MouseLeave += (s, x) =>
            {
                AnimationHelper.MouseLeaveColorEvent(0, 0, 0, 0, ApplyButtonBrush, false);
            };

            ApplyButton.PreviewMouseLeftButtonDown += (s, x) =>
            {
                AnimationHelper.PreviewMouseLeftButtonDownColorEvent(ApplyButtonBrush, false);
            };
        }

        #region Eventhandlers

        // Close Button
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Closing -= Window_Closing;
            
            AnimationHelper.FadeWindow(this, 0, TimeSpan.FromSeconds(.5));
            FocusManager.SetFocusedElement(Application.Current.MainWindow, this);
        }

        private void Apply(object sender, RoutedEventArgs e)
        {
            
            if(rbRename.IsChecked == true)
            {
                DialogResult = true;
                RbtnName = "rbRename";
                Close();
            }
            else if(rbBulkRename.IsChecked == true)
            {
                DialogResult = true;
                RbtnName = "rbBulkRename";
                Counter2 = txtcounter.Text;
                Close();
            }
            else if (rbBulkRenameEx.IsChecked == true)
            {
                DialogResult = true;
                RbtnName = "rbBulkRenameEx";
                Close();
            }
            else if(rbBulkResize.IsChecked == true)
            {
                DialogResult = true;
                RbtnName = "rbBulkResize";
                NewPicHeight = int.Parse(txtHeight.Text);
                NewPicWidth = int.Parse(txtWidth.Text);
                Close();
            }
         
        }


        #endregion

        private void rbRename_Click(object sender, RoutedEventArgs e)
        {
            RadioButton rbtn = sender as RadioButton;

            if(rbtn != null && rbtn.IsChecked == true)
            {
                switch(rbtn.Name)
                {
                    case "rbRename":
                        //RenameLabel.Text = Message;
                        TxtBox.ToolTip = "Write what you wanna rename the file to.";
                        TxtBox.Visibility = Visibility.Visible;
                        txtcounter.Visibility = Visibility.Collapsed;
                        lbCounter.Visibility = Visibility.Collapsed;
                        lbHeight.Visibility = Visibility.Collapsed;
                        lbWidth.Visibility = Visibility.Collapsed;
                        txtHeight.Visibility = Visibility.Collapsed;
                        txtWidth.Visibility = Visibility.Collapsed;
                        break;

                    case "rbBulkRename":
                        RenameLabel.Text = "Are you sure you wanna rename all \r\n" + " files in the folder.";
                        TxtBox.ToolTip = "Write what you wanna call all files in the folder with extension.";
                        TxtBox.Visibility = Visibility.Visible;
                        txtcounter.Visibility = Visibility.Visible;
                        lbCounter.Visibility = Visibility.Visible;
                        lbHeight.Visibility = Visibility.Collapsed;
                        lbWidth.Visibility = Visibility.Collapsed;
                        txtHeight.Visibility = Visibility.Collapsed;
                        txtWidth.Visibility = Visibility.Collapsed;
                        break;

                    case "rbBulkRenameEx":
                        RenameLabel.Text = "Are you sure you wanna change all \r\n" + " extension in the folder.";
                        TxtBox.ToolTip = "Write the extension you wanna convert all files in folder to";
                        TxtBox.Visibility = Visibility.Visible;
                        txtcounter.Visibility = Visibility.Collapsed;
                        lbCounter.Visibility = Visibility.Collapsed;
                        lbHeight.Visibility = Visibility.Collapsed;
                        lbWidth.Visibility = Visibility.Collapsed;
                        txtHeight.Visibility = Visibility.Collapsed;
                        txtWidth.Visibility = Visibility.Collapsed;
                        break;

                    case "rbBulkResize":
                        RenameLabel.Text = "Are you sure you wanna resize all \r\n" + " pictures in the folder. (This may take time.)";
                        txtWidth.ToolTip = "Write the width you wanna resize all images in folder to";
                        txtHeight.ToolTip = "Write the height you wanna resize all images in folder to";
                        txtcounter.Visibility = Visibility.Collapsed;
                        lbCounter.Visibility = Visibility.Collapsed;
                        TxtBox.Visibility = Visibility.Collapsed;
                        lbHeight.Visibility = Visibility.Visible;
                        lbWidth.Visibility = Visibility.Visible;
                        txtHeight.Visibility = Visibility.Visible;
                        txtWidth.Visibility = Visibility.Visible;
                        break;
                }


            }
        }

        
    }
}
