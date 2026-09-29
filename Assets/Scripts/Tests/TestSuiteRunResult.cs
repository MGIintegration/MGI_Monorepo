#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;

public sealed class TestSuiteRunResult
{
    public int PassedCount { get; }
    public int FailedCount { get; }
    public IReadOnlyList<string> FailureMessages { get; }
    public bool Passed => FailedCount == 0;

    public TestSuiteRunResult(int passedCount, int failedCount, IEnumerable<string> failureMessages)
    {
        PassedCount = passedCount;
        FailedCount = failedCount;
        FailureMessages = new List<string>(failureMessages ?? Enumerable.Empty<string>());
    }
}
#endif
