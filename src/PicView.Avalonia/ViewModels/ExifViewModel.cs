using System.Windows.Input;
using Avalonia.Media;
using PicView.Core.Localization;
using ReactiveUI;

namespace PicView.Avalonia.ViewModels;

public class ExifViewModel : ViewModelBase
{
    public void UpdateTranslations()
    {
    }

    public ExifViewModel()
    {
        UpdateTranslations();
        SetExifRating1Command = ReactiveCommand.CreateFromTask(async () =>
        {
            await Core.ImageDecoding.EXIFHelper.SetEXIFRating(FileInfo.FullName, 1);
        });
        SetExifRating2Command = ReactiveCommand.CreateFromTask(async () =>
        {
            await Core.ImageDecoding.EXIFHelper.SetEXIFRating(FileInfo.FullName, 2);
        });
        SetExifRating3Command = ReactiveCommand.CreateFromTask(async () =>
        {
            await Core.ImageDecoding.EXIFHelper.SetEXIFRating(FileInfo.FullName, 3);
        });
        SetExifRating4Command = ReactiveCommand.CreateFromTask(async () =>
        {
            await Core.ImageDecoding.EXIFHelper.SetEXIFRating(FileInfo.FullName, 4);
        });
        SetExifRating5Command = ReactiveCommand.CreateFromTask(async () =>
        {
            await Core.ImageDecoding.EXIFHelper.SetEXIFRating(FileInfo.FullName, 5);
        });
    }

    public ICommand? SetExifRating1Command { get; }
    public ICommand? SetExifRating2Command { get; }
    public ICommand? SetExifRating3Command { get; }
    public ICommand? SetExifRating4Command { get; }
    public ICommand? SetExifRating5Command { get; }
}