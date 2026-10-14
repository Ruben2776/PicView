using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Headless;
using Xunit.Sdk;
using Xunit.v3;

namespace PicView.Tests;

// Avalonia.Headless.XUnit's [AvaloniaFact]/[AvaloniaTheory] are compiled against xunit.v3 3.x and fail discovery
// with a MissingMethodException on xunit.v3 4.x. These attributes do the same job on top of Avalonia.Headless:
// the whole test (constructor, body and Dispose) runs on the dispatcher thread of the shared HeadlessUnitTestSession.
// Use them for every test that creates Avalonia objects or touches Dispatcher.UIThread, because Avalonia binds its
// dispatcher to a single thread while plain [Fact] tests run on whichever xUnit worker thread is free.

/// <summary>
/// A <see cref="FactAttribute"/> that runs the test on the headless Avalonia dispatcher thread.
/// </summary>
[XunitTestCaseDiscoverer(typeof(AvaloniaFactDiscoverer))]
[AttributeUsage(AttributeTargets.Method)]
public sealed class AvaloniaFactAttribute(
    [CallerFilePath] string? sourceFilePath = null,
    [CallerLineNumber] int sourceLineNumber = -1)
    : FactAttribute(sourceFilePath, sourceLineNumber);

/// <summary>
/// A <see cref="TheoryAttribute"/> that runs each data row on the headless Avalonia dispatcher thread.
/// </summary>
[XunitTestCaseDiscoverer(typeof(AvaloniaTheoryDiscoverer))]
[AttributeUsage(AttributeTargets.Method)]
public sealed class AvaloniaTheoryAttribute(
    [CallerFilePath] string? sourceFilePath = null,
    [CallerLineNumber] int sourceLineNumber = -1)
    : TheoryAttribute(sourceFilePath, sourceLineNumber);

public sealed class AvaloniaFactDiscoverer : FactDiscoverer
{
    protected override IXunitTestCase CreateTestCase(
        ITestFrameworkDiscoveryOptions discoveryOptions,
        IXunitTestMethod testMethod,
        IFactAttribute factAttribute) =>
        AvaloniaTestCase.From(base.CreateTestCase(discoveryOptions, testMethod, factAttribute));
}

public sealed class AvaloniaTheoryDiscoverer : TheoryDiscoverer
{
    protected override async ValueTask<IReadOnlyCollection<IXunitTestCase>> CreateTestCasesForDataRow(
        ITestFrameworkDiscoveryOptions discoveryOptions,
        IXunitTestMethod testMethod,
        ITheoryAttribute theoryAttribute,
        ITheoryDataRow dataRow,
        object?[] testMethodArguments,
        string? label)
    {
        var testCases = await base.CreateTestCasesForDataRow(discoveryOptions, testMethod, theoryAttribute, dataRow,
            testMethodArguments, label);
        return testCases.Select(AvaloniaTestCase.From).ToArray();
    }

    protected override async ValueTask<IReadOnlyCollection<IXunitTestCase>> CreateTestCasesForTheory(
        ITestFrameworkDiscoveryOptions discoveryOptions,
        IXunitTestMethod testMethod,
        ITheoryAttribute theoryAttribute)
    {
        var testCases = await base.CreateTestCasesForTheory(discoveryOptions, testMethod, theoryAttribute);
        return testCases.Select(AvaloniaTestCase.From).ToArray();
    }
}

internal static class AvaloniaTestCase
{
    private static readonly Lazy<HeadlessUnitTestSession> Session =
        new(() => HeadlessUnitTestSession.GetOrStartForAssembly(typeof(AvaloniaTestCase).Assembly));

    public static IXunitTestCase From(IXunitTestCase testCase) => testCase switch
    {
        XunitDelayEnumeratedTheoryTestCase theory => new AvaloniaDelayEnumeratedTheoryTestCase(theory),
        XunitTestCase test => new AvaloniaFactTestCase(test),
        // Error or skip placeholders produced during discovery never touch Avalonia
        _ => testCase
    };

    public static ValueTask<RunSummary> Run(
        IXunitTestCase testCase,
        ExplicitOption explicitOption,
        IMessageBus messageBus,
        object?[] constructorArguments,
        ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource,
        ParallelMode parallelMode,
        ExecutionScheduler scheduler,
        FixtureMappingManager methodFixtureMappings) =>
        new(Session.Value.Dispatch(
            () => XunitRunnerHelper.RunXunitTestCase(testCase, messageBus, cancellationTokenSource, parallelMode,
                scheduler, aggregator, explicitOption, constructorArguments, methodFixtureMappings).AsTask(),
            cancellationTokenSource.Token));
}

public sealed class AvaloniaFactTestCase : XunitTestCase, ISelfExecutingXunitTestCase
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
    public AvaloniaFactTestCase() { }

    public AvaloniaFactTestCase(XunitTestCase testCase)
        : base(testCase.TestMethod, testCase.TestCaseDisplayName, testCase.UniqueID, testCase.Explicit,
            testCase.TestLabel, testCase.DisableParallelization, testCase.SkipExceptions, testCase.SkipReason,
            testCase.SkipType, testCase.SkipUnless, testCase.SkipWhen,
            testCase.Traits.ToDictionary(t => t.Key, t => new HashSet<string>(t.Value)),
            testCase.TestMethodArguments, testCase.SourceFilePath, testCase.SourceLineNumber, testCase.Timeout)
    {
    }

    public ValueTask<RunSummary> Run(ExplicitOption explicitOption, IMessageBus messageBus,
        object?[] constructorArguments, ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource, ParallelMode parallelMode, ExecutionScheduler scheduler,
        FixtureMappingManager methodFixtureMappings) =>
        AvaloniaTestCase.Run(this, explicitOption, messageBus, constructorArguments, aggregator,
            cancellationTokenSource, parallelMode, scheduler, methodFixtureMappings);
}

public sealed class AvaloniaDelayEnumeratedTheoryTestCase : XunitDelayEnumeratedTheoryTestCase,
    ISelfExecutingXunitTestCase
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
    public AvaloniaDelayEnumeratedTheoryTestCase() { }

    public AvaloniaDelayEnumeratedTheoryTestCase(XunitDelayEnumeratedTheoryTestCase testCase)
        : base(testCase.TestMethod, testCase.TestCaseDisplayName, testCase.UniqueID, testCase.Explicit,
            testCase.SkipTestWithoutData, testCase.SkipExceptions, testCase.SkipReason, testCase.SkipType,
            testCase.SkipUnless, testCase.SkipWhen,
            testCase.Traits.ToDictionary(t => t.Key, t => new HashSet<string>(t.Value)),
            testCase.SourceFilePath, testCase.SourceLineNumber, testCase.Timeout)
    {
    }

    public ValueTask<RunSummary> Run(ExplicitOption explicitOption, IMessageBus messageBus,
        object?[] constructorArguments, ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource, ParallelMode parallelMode, ExecutionScheduler scheduler,
        FixtureMappingManager methodFixtureMappings) =>
        AvaloniaTestCase.Run(this, explicitOption, messageBus, constructorArguments, aggregator,
            cancellationTokenSource, parallelMode, scheduler, methodFixtureMappings);
}
