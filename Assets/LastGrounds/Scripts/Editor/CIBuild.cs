using System;
using System.Collections.Generic;
using System.IO;
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
            if (target == BuildTarget.Android) ConfigureAndroid(args);

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

        /// <summary>
        /// Version code, APK vs AAB and signing from the arguments unity-builder passes.
        /// Without a keystore the build is signed with the debug key (fine for itch.io / sideloading).
        /// </summary>
        static void ConfigureAndroid(string[] args)
        {
            int code;
            if (int.TryParse(Arg(args, "-androidVersionCode"), out code) && code > 0)
                PlayerSettings.Android.bundleVersionCode = code;

            EditorUserBuildSettings.buildAppBundle = Arg(args, "-androidExportType") == "androidAppBundle";

            string keystore = Arg(args, "-androidKeystoreName");
            string keystorePass = Arg(args, "-androidKeystorePass");
            bool hasKeystore = !string.IsNullOrEmpty(keystore) && File.Exists(keystore)
                && new FileInfo(keystore).Length > 0 && !string.IsNullOrEmpty(keystorePass);
            if (hasKeystore)
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = Path.GetFullPath(keystore);
                PlayerSettings.Android.keystorePass = keystorePass;
                PlayerSettings.Android.keyaliasName = Arg(args, "-androidKeyaliasName");
                PlayerSettings.Android.keyaliasPass = Arg(args, "-androidKeyaliasPass");
                Debug.Log("[CI] Signing with keystore " + keystore);
            }
            else
            {
                PlayerSettings.Android.useCustomKeystore = false;
                Debug.Log("[CI] No keystore provided: signing with the debug key.");
            }
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
