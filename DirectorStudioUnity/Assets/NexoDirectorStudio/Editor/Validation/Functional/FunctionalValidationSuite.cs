#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NexoDirectorStudio.Editor.Validation.Functional
{
    [Serializable]
    public sealed class FunctionalAssertion
    {
        public string name;
        public bool pass;
        public string message;   // short, human-friendly
        public string details;   // optional longer context
    }

    [Serializable]
    public sealed class FunctionalReport
    {
        public List<FunctionalAssertion> assertions = new();
        public int total => assertions.Count;
        public int failures => assertions.Count(a => !a.pass);
        public int passes => assertions.Count(a => a.pass);

        public void Add(string name, bool pass, string message, string details = null)
        {
            assertions.Add(new FunctionalAssertion { name = name, pass = pass, message = message, details = details ?? string.Empty });
        }
    }

    public interface IFunctionalValidator
    {
        string Name { get; }
        void Run(FunctionalReport report);
    }

    public static class FunctionalValidationSuite
    {
        public static FunctionalReport RunAll(IEnumerable<IFunctionalValidator> validators)
        {
            var report = new FunctionalReport();
            foreach (var v in validators) {
                try { v.Run(report); }
                catch (Exception ex) {
                    report.Add($"{v.Name}.Crash", false, "Validator threw an exception", ex.ToString());
                }
            }
            // Generic missing-script scan (Editor-only)
            var missingCount = CountMissingScriptsInOpenScene();
            report.Add("Scene.MissingScripts", missingCount == 0, 
                missingCount == 0 ? "No missing scripts in scene" : $"Missing scripts found: {missingCount}",
                "Remove/repair missing MonoBehaviour references on GameObjects.");
            return report;
        }

        static int CountMissingScriptsInOpenScene()
        {
            int total = 0;
            foreach (var go in UnityEngine.Object.FindObjectsOfType<GameObject>(includeInactive: true))
            {
                var comps = go.GetComponents<Component>();
                foreach (var c in comps) if (c == null) total++;
            }
            return total;
        }
    }
}
#endif
