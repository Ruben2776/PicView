using Avalonia;
using PicView.Core.Config;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.Reactive;

namespace PicView.Avalonia.ViewModels
{
    public class MainViewModel : ReactiveObject
    {
        private readonly ISettingsManager settingsManager;

        public MainViewModel(ISettingsManager settingsManager)
        {
            this.settingsManager = settingsManager;

            // ReactiveCommand for loading settings
            LoadSettingsCommand = ReactiveCommand.CreateFromTask(LoadSettings);

            // Execute the command on ViewModel initialization
            LoadSettingsCommand.Execute().Subscribe();
        }

        private async Task LoadSettings()
        {
            try
            {
                // Load settings using the injected ISettingsManager
                await settingsManager.LoadSettingsAsync();

                // Reactively update the properties bound to the UI
                this.RaisePropertyChanged(nameof(AppSettings));
            }
            catch (Exception ex)
            {
                // Handle exceptions appropriately (logging, notifying the user, etc.)
                Trace.WriteLine($"{nameof(LoadSettings)}: error loading settings:\n {ex.Message}");
            }
        }

        // Expose AppSettings as a property for data binding in the View
        public AppSettings? AppSettings => settingsManager.AppSettings;

        // ReactiveCommand for loading settings
        public ReactiveCommand<Unit, Unit> LoadSettingsCommand { get; }
    }
}