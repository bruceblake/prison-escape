using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

/// <summary>
/// Headless-friendly runner for the career EditMode tests: kicks the run from a menu item and
/// writes a plain-text result log that survives the test framework's domain reloads
/// (results land next to the temp dir so tooling can poll them).
/// </summary>
public static class CareerTestRunner
{
    public static readonly string OutPath =
        Path.Combine(Path.GetTempPath(), "claude", "career_test_results.txt");

    [MenuItem("Tools/Prison/Career/Run Career EditMode Tests")]
    public static void Run()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(OutPath));
        if (File.Exists(OutPath)) File.Delete(OutPath);

        var api = ScriptableObject.CreateInstance<TestRunnerApi>();
        var filter = new Filter
        {
            testMode = TestMode.EditMode,
            groupNames = new[] { "Prison\\.Tests\\.Career.*" },
        };
        api.Execute(new ExecutionSettings(filter));
        Debug.Log($"[CareerTestRunner] EditMode career test run started; results → {OutPath}");
    }
}

/// <summary>Re-registers the result writer after every domain reload so no run goes unlogged.</summary>
[InitializeOnLoad]
public static class CareerTestResultWriter
{
    static CareerTestResultWriter()
    {
        var api = ScriptableObject.CreateInstance<TestRunnerApi>();
        api.RegisterCallbacks(new Callbacks());
    }

    private class Callbacks : ICallbacks
    {
        private readonly System.Text.StringBuilder _sb = new System.Text.StringBuilder();

        public void RunStarted(ITestAdaptor testsToRun) { }

        public void RunFinished(ITestResultAdaptor result)
        {
            _sb.AppendLine($"RUN FINISHED: {result.TestStatus} | passed={result.PassCount} failed={result.FailCount} skipped={result.SkipCount}");
            Flush();
        }

        public void TestStarted(ITestAdaptor test) { }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (result.Test.IsSuite) return;
            _sb.AppendLine($"{result.TestStatus}: {result.FullName}");
            if (result.TestStatus == TestStatus.Failed)
                _sb.AppendLine("   " + (result.Message ?? "").Replace("\n", " | "));
            Flush();
        }

        private void Flush()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CareerTestRunner.OutPath));
            File.WriteAllText(CareerTestRunner.OutPath, _sb.ToString());
        }
    }
}
