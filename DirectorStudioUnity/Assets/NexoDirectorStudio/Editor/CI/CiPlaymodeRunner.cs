#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace NexoDirectorStudio.Editor.CI
{
    public static class CiPlaymodeRunner
    {
        // Usage (batch):
        // -executeMethod NexoDirectorStudio.Editor.CI.CiPlaymodeRunner.Run
        // Optional: -testResults /abs/path/results.json
        public static void Run()
        {
            var resultsPath = GetCmdArgValue("-testResults") ?? Path.GetFullPath("playmode-results.json");
            
            Debug.Log($"[CiPlaymodeRunner] Starting test run. Results will be saved to: {resultsPath}");

            try
            {
                var api = ScriptableObject.CreateInstance<TestRunnerApi>();
                var filter = new Filter { testMode = TestMode.PlayMode };

                var summary = new Summary();
                api.RegisterCallbacks(new Callbacks(summary, resultsPath));
                api.Execute(new ExecutionSettings(filter));
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CiPlaymodeRunner] Error during test execution: {ex.Message}");
                Debug.LogError($"[CiPlaymodeRunner] Stack trace: {ex.StackTrace}");
                
                // Create a minimal results file indicating failure
                var summary = new Summary
                {
                    total = 0,
                    passed = 0,
                    failed = 1,
                    skipped = 0,
                    inconclusive = 0,
                    duration = 0
                };
                
                Directory.CreateDirectory(Path.GetDirectoryName(resultsPath) ?? ".");
                File.WriteAllText(resultsPath, JsonUtility.ToJson(summary, true));
                Debug.Log($"[CiPlaymodeRunner] Created failure results file: {resultsPath}");
                
                EditorApplication.Exit(1);
            }
        }

        static string GetCmdArgValue(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            return null;
        }

        class Summary
        {
            public int total, passed, failed, skipped, inconclusive;
            public double duration;
        }

        class Callbacks : ICallbacks
        {
            readonly Summary _s; readonly string _path;
            public Callbacks(Summary s, string path) { _s = s; _path = path; }

            public void RunStarted(ITestAdaptor testsToRun)
            {
                _s.total = testsToRun?.TestCaseCount ?? 0;
                Debug.Log($"[CiPlaymodeRunner] RunStarted. Total PlayMode tests: {_s.total}");
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result == null || result.Test == null) return;
                _s.duration += result.Duration;
                switch (result.TestStatus)
                {
                    case TestStatus.Passed:       _s.passed++; break;
                    case TestStatus.Failed:       _s.failed++; break;
                    case TestStatus.Skipped:      _s.skipped++; break;
                    case TestStatus.Inconclusive: _s.inconclusive++; break;
                }
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path) ?? ".");
                File.WriteAllText(_path, JsonUtility.ToJson(_s, true));
                Debug.Log($"[CiPlaymodeRunner] RunFinished. Passed={_s.passed} Failed={_s.failed} " +
                          $"Skipped={_s.skipped} Inconclusive={_s.inconclusive} Duration={_s.duration:0.00}s " +
                          $"Results: {_path}");

                // Exit 0 if all pass; 2 if any failures (similar to NUnit)
                EditorApplication.Exit(_s.failed == 0 ? 0 : 2);
            }

            public void TestStarted(ITestAdaptor test) { }
        }
    }
}
#endif
