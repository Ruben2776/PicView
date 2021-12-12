﻿using System.Reactive;
using Avalonia.Controls.ApplicationLifetimes;
using ReactiveUI;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Media;

namespace PicView.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel(Window window)
        {
            ExitCommand = ReactiveCommand.Create(window.Close);

            MinimizeCommand = ReactiveCommand.Create(() => window.WindowState = WindowState.Minimized);
            
            LoadCommand = ReactiveCommand.Create(async () =>
            {
                var args = Environment.GetCommandLineArgs();
                if (args.Length < 1) { return; }
                
                FileInfo fileInfo = new(args[1]);
                var pic = await Data.Imaging.ImageDecoder.ReturnPicAsync(fileInfo).ConfigureAwait(false);
                if (pic is not null)
                {
                    Pic = pic;
                    Title = "Loaded";
                }
                else
                {
                    Title = "No image loaded";
                }
            });
        }
        public ICommand ExitCommand { get; }
        public ICommand MinimizeCommand { get; }
        
        public ICommand LoadCommand { get; }
        
        private string _title = "Loading...";
        public string Title
        {
            get => _title;
            set => this.RaiseAndSetIfChanged(ref _title, value);
        }
        
        private IImage _pic;
        public IImage Pic
        {
            get => _pic;
            set => this.RaiseAndSetIfChanged(ref _pic, value);
        }



    }
}
