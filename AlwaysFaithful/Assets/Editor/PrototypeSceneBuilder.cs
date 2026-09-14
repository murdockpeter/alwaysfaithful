using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
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
    }
}
