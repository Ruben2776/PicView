namespace PicView.Tests;

/// <summary>
/// Tests that share process-wide state — <see cref="PicView.Core.Config.SettingsManager.Settings"/>, static keybinding
/// and favorites managers, or the headless Avalonia session — join this collection. Parallelization is disabled so the
/// collection never runs alongside any other collection, which would otherwise see that state replaced mid-test.
/// </summary>
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class SequentialCollection;
