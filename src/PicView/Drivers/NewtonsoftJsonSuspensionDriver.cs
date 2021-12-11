using System.Reactive;
using System.Reactive.Linq;
using Newtonsoft.Json;
using ReactiveUI;
using Formatting = System.Xml.Formatting;

namespace PicView.Drivers;

public sealed class NewtonsoftJsonSuspensionDriver : ISuspensionDriver
{
    private readonly string _stateFilePath;

    private readonly JsonSerializerSettings _settings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.All
    };

    public NewtonsoftJsonSuspensionDriver(string stateFilePath) => _stateFilePath = stateFilePath;

    public IObservable<Unit> InvalidateState()
    {
        if (File.Exists(_stateFilePath))
        {
            File.Delete(_stateFilePath);
        }

        return Observable.Return(Unit.Default);
    }

    public IObservable<object> LoadState()
    {
        var lines = File.ReadAllText(_stateFilePath);
        var state = JsonConvert.DeserializeObject<object>(lines, _settings);
        return Observable.Return(state!);
    }

    public IObservable<Unit> SaveState(object state)
    {
        var lines = JsonConvert.SerializeObject(state, (Newtonsoft.Json.Formatting) Formatting.Indented, _settings);
        File.WriteAllText(_stateFilePath, lines);
        return Observable.Return(Unit.Default);
    }
}