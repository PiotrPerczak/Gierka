using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace AnkleBreaker.EnvironmentRocks.Editor.Welcome
{
    [InitializeOnLoad]
    internal static class EnvironmentRocksWelcomeGuideRehome
    {
        /// <summary>The guide folder, then each file shipped beside it outside the guide (the
        /// CLICKME at the AB_Stylized root): GUID, shipped path, path relative to the art root.</summary>
        private static readonly string[] Moves =
        {
            "843a28cc9b6fbca4b8e292bea71655b2", "Assets/AB_Stylized/4_PackageGuides/EnvironmentRocks", "../../4_PackageGuides/EnvironmentRocks",
            "ef7cd159aad80338c0fe45bf99f516b6", "Assets/AB_Stylized/ClickMe-EnvironmentRocks.asset", "../../ClickMe-EnvironmentRocks.asset",
        };
        static EnvironmentRocksWelcomeGuideRehome()
        {
            EditorApplication.update += WaitForIdle;
        }
        private static void WaitForIdle()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            EditorApplication.update -= WaitForIdle;
            Run();
        }
        internal static void Run()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer) return;
            string anchor = AssetDatabase.GUIDToAssetPath("8d4189cd65d2cc94b806a380f62095cf");
            if (!AssetDatabase.IsValidFolder(anchor)) return;
            for (int i = 0; i < Moves.Length; i += 3)
                Move(anchor, Moves[i], Moves[i + 1], Moves[i + 2], i == 0);
        }
        private static void Move(string anchor, string guid, string original, string relative, bool folder)
        {
            string source = AssetDatabase.GUIDToAssetPath(guid);
            if (folder ? !AssetDatabase.IsValidFolder(source) : AssetDatabase.LoadMainAssetAtPath(source) == null) return;
            string target = EnvironmentRocksWelcomeServices.Combine(anchor, relative);
            if (source == target || source != original) return;
            if (!target.StartsWith("Assets/", StringComparison.Ordinal) ||
                target.StartsWith(source + "/", StringComparison.Ordinal) ||
                AssetDatabase.LoadMainAssetAtPath(target) != null || Directory.Exists(target) || File.Exists(target)) return;
            string parent = Path.GetDirectoryName(target)?.Replace('\\', '/');
            if (!EnsureFolder(parent)) return;
            string error = AssetDatabase.MoveAsset(source, target);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogWarning("Stylized Environment Rocks: guide relocation failed: " + error);
                return;
            }
            string oldParent = Path.GetDirectoryName(source)?.Replace('\\', '/');
            while (!string.IsNullOrEmpty(oldParent) && oldParent.StartsWith("Assets/", StringComparison.Ordinal) &&
                   Directory.Exists(oldParent) && Directory.GetFileSystemEntries(oldParent).Length == 0)
            {
                if (!AssetDatabase.DeleteAsset(oldParent)) break;
                oldParent = Path.GetDirectoryName(oldParent)?.Replace('\\', '/');
            }
            Debug.Log("Stylized Environment Rocks: moved " + source + " -> " + target + " (art root resolved by GUID)");
        }
        private static bool EnsureFolder(string path)
        {
            if (path == "Assets") return true;
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal)) return false;
            if (AssetDatabase.IsValidFolder(path)) return true;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            return EnsureFolder(parent) && !string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, Path.GetFileName(path)));
        }
    }
}
