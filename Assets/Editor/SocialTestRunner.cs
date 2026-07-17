using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

/// <summary>
/// Headless-friendly runner for the FULL EditMode suite (social ecosystem + everything else),
/// mirroring <see cref="CareerTestRunner"/>: menu-triggered, plain-text result log that
/// survives domain reloads so tooling can poll it.
/// </summary>
public static class SocialTestRunner
{
    public static readonly string OutPath =
        Path.Combine(Path.GetTempPath(), "claude", "editmode_test_results.txt");

    [MenuItem("Tools/Prison/Social/Run All EditMode Tests")]
    public static void Run()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(OutPath));
        if (File.Exists(OutPath)) File.Delete(OutPath);

        var api = ScriptableObject.CreateInstance<TestRunnerApi>();
        var filter = new Filter { testMode = TestMode.EditMode };
        api.Execute(new ExecutionSettings(filter));
        Debug.Log($"[SocialTestRunner] Full EditMode test run started; results → {OutPath}");
    }
}

/// <summary>Re-registers the result writer after every domain reload so no run goes unlogged.</summary>
[InitializeOnLoad]
public static class SocialTestResultWriter
{
    static SocialTestResultWriter()
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
            if (result.TestStatus == TestStatus.Failed)
            {
                _sb.AppendLine($"{result.TestStatus}: {result.FullName}");
                _sb.AppendLine("   " + (result.Message ?? "").Replace("\n", " | "));
                Flush();
            }
        }

        private void Flush()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SocialTestRunner.OutPath));
            File.WriteAllText(SocialTestRunner.OutPath, _sb.ToString());
        }
    }
}
