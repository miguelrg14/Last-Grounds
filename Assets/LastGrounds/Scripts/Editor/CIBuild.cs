using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LastGrounds.EditorTools
{
    /// <summary>
    /// Entry point for game-ci/unity-builder (buildMethod: LastGrounds.EditorTools.CIBuild.Build).
    /// Generates the project with <see cref="LastGroundsBuilder"/> and builds the player.
    /// Reads the arguments unity-builder passes: -buildTarget, -customBuildPath, -buildVersion.
    /// </summary>
    public static class CIBuild
    {
        public static void Build()
        {
            string[] args = Environment.GetCommandLineArgs();
            string targetName = Arg(args, "-buildTarget");
            string outputPath = Arg(args, "-customBuildPath");
            string version = Arg(args, "-buildVersion");

            if (string.IsNullOrEmpty(targetName) || string.IsNullOrEmpty(outputPath))
            {
                Fail("Missing -buildTarget or -customBuildPath.");
                return;
            }

            BuildTarget target;
            try
            {
                target = (BuildTarget)Enum.Parse(typeof(BuildTarget), targetName, true);
            }
            catch (ArgumentException)
            {
                Fail("Unknown build target '" + targetName + "'.");
                return;
            }

            if (!LastGroundsBuilder.Build())
            {
                Fail("Generating the project failed.");
                return;
            }

            if (!string.IsNullOrEmpty(version) && version != "none") PlayerSettings.bundleVersion = version;

            List<string> scenes = new List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
                if (scene.enabled) scenes.Add(scene.path);

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = outputPath,
                target = target,
                targetGroup = BuildPipeline.GetBuildTargetGroup(target),
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log("[CI] " + target + " build " + summary.result + ": " + summary.totalSize / (1024 * 1024) +
                      " MB, " + summary.totalErrors + " errors, " + summary.totalWarnings + " warnings, " +
                      summary.totalTime.TotalSeconds.ToString("0") + " s -> " + outputPath);

            if (summary.result != BuildResult.Succeeded)
            {
                Fail("Build result: " + summary.result);
                return;
            }
            EditorApplication.Exit(0);
        }

        static string Arg(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }

        static void Fail(string message)
        {
            Debug.LogError("[CI] " + message);
            EditorApplication.Exit(1);
        }
    }
}
