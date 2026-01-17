using System.Collections.ObjectModel;
using PicView.Core.Models;
using R3;

namespace PicView.Core.ViewModels;

public class KeybindingsViewModel : IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    
    public BindableReactiveProperty<string> FilterText { get; } = new(string.Empty);
    public BindableReactiveProperty<bool> IsFiltering { get; } = new();
    public BindableReactiveProperty<ObservableCollection<KeyBindingsModel>> FilteredKeys { get; } = new([]);
    public ReactiveCommand? ClearFilteringCommand { get; }
    
    public ReactiveCommand? ResetKeybindingsCommand { get; set; }
    
    
    public BindableReactiveProperty<ObservableCollection<KeyBindingsModel>> NavigationKeys { get; } = new([]);
    public BindableReactiveProperty<ObservableCollection<KeyBindingsModel>> ScrollAndRotateKeys { get; } = new([]);
    public BindableReactiveProperty<ObservableCollection<KeyBindingsModel>> ZoomKeys { get; } = new([]);
    public BindableReactiveProperty<ObservableCollection<KeyBindingsModel>> ImageControlKeys { get; } = new([]);
    public BindableReactiveProperty<ObservableCollection<KeyBindingsModel>> InterfaceConfigurationKeys { get; } = new([]);
    public BindableReactiveProperty<ObservableCollection<KeyBindingsModel>> FileManagementKeys { get; } = new([]);
    public BindableReactiveProperty<ObservableCollection<KeyBindingsModel>> SortFilesKeys { get; } = new([]);
    public BindableReactiveProperty<ObservableCollection<KeyBindingsModel>> CopyKeys { get; } = new([]);
    public BindableReactiveProperty<ObservableCollection<KeyBindingsModel>> ToolWindowsKeys { get; } = new([]);
    public BindableReactiveProperty<ObservableCollection<KeyBindingsModel>> WindowManagementKeys { get; } = new([]);
    public BindableReactiveProperty<ObservableCollection<KeyBindingsModel>> WindowScalingKeys { get; } = new([]);
    public BindableReactiveProperty<ObservableCollection<KeyBindingsModel>> StarRatingKeys { get; } = new([]);

    public KeybindingsViewModel()
    {
       Observable.EveryValueChanged(FilterText, _ => IsFiltering.Value = !string.IsNullOrWhiteSpace(FilterText.Value))
           .Subscribe(x => { }).AddTo(_disposables);
       
       // Subscribe to FilterText changes to update FilteredKeys
       FilterText
           .Subscribe(filterText =>
           {
               FilteredKeys.Value.Clear();
               
               if (string.IsNullOrWhiteSpace(filterText))
               {
                   return;
               }
               
               // Get all keybinding collections
               var allCollections = new[]
               {
                   NavigationKeys.Value,
                   ScrollAndRotateKeys.Value,
                   ZoomKeys.Value,
                   ImageControlKeys.Value,
                   InterfaceConfigurationKeys.Value,
                   FileManagementKeys.Value,
                   SortFilesKeys.Value,
                   CopyKeys.Value,
                   ToolWindowsKeys.Value,
                   WindowManagementKeys.Value,
                   WindowScalingKeys.Value,
                   StarRatingKeys.Value
               };
               
               // Search through all collections and add matches
               foreach (var collection in allCollections)
               {
                   foreach (var binding in collection)
                   {
                       if (MatchesFilter(binding, filterText))
                       {
                           FilteredKeys.Value.Add(binding);
                       }
                   }
               }
           })
           .AddTo(_disposables);
       
       ClearFilteringCommand = new ReactiveCommand(_ => { FilterText.Value = string.Empty; });
       
       _disposables.Add(FilteredKeys);
       _disposables.Add(NavigationKeys);
       _disposables.Add(ScrollAndRotateKeys);
       _disposables.Add(ZoomKeys);
       _disposables.Add(ImageControlKeys);
       _disposables.Add(InterfaceConfigurationKeys);
       _disposables.Add(FileManagementKeys);
       _disposables.Add(SortFilesKeys);
       _disposables.Add(CopyKeys);
       _disposables.Add(ToolWindowsKeys);
       _disposables.Add(WindowManagementKeys);
       _disposables.Add(WindowScalingKeys);
       _disposables.Add(StarRatingKeys);
    }
    
    private static bool MatchesFilter(KeyBindingsModel binding, string searchTerm)
    {
        // Search in friendly name, method name, key, and alt key
        return (binding.FriendlyMethodName?.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase) ?? false) ||
               (binding.MethodName?.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase) ?? false) ||
               (binding.Key?.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase) ?? false) ||
               (binding.AltKey?.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase) ?? false);
    }

    
    
    public void Dispose()
    {
        _disposables.Dispose();
        
        GC.SuppressFinalize(this);
    }
}