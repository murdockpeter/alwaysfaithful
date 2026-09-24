using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;

namespace AlwaysFaithful.Editor
{
    public static class PrototypeSceneBuilder
    {
        public static void CreatePrototypeScene()
        {
            const string directory = "Assets/Scenes";
            const string scenePath = directory + "/HexAndCounterPrototype.unity";
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
            AssetDatabase.SaveAssets();
        }

        // Command-line entry point used by the repository's regression flow:
        // Unity -batchmode -quit -projectPath AlwaysFaithful
        //   -executeMethod AlwaysFaithful.Editor.PrototypeSceneBuilder.BuildWindowsPlayer
        public static void BuildWindowsPlayer()
        {
            const string scenePath = "Assets/Scenes/HexAndCounterPrototype.unity";
            const string outputPath = "Builds/Windows/AlwaysFaithful.exe";
            Directory.CreateDirectory("Builds/Windows");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { scenePath },
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"Always Faithful Windows build failed: {report.summary.result}");
        }
    }
}
