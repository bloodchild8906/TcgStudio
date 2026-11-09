using System;
using UnityEditor;
using UnityEngine;

namespace CardForge.Editor
{
    /// <summary>
    /// CI integration, without creating additional Unity menu items.
    /// unity -batchmode -quit -projectPath ... -buildTarget StandaloneWindows64
    ///       -executeMethod CardForge.Editor.CardForgeBatchBuild.BuildAllActive
    /// </summary>
    public static class CardForgeBatchBuild
    {
        public static void BuildAllActive()
        {
            var store = new CardForgeStore();
            store.Reload();
            var target = EditorUserBuildSettings.activeBuildTarget;
            int complete = 0;
            foreach (var block in store.Blocks.Values)
            {
                try
                {
                    string destination = store.ExportBlock(block, target);
                    Debug.Log("CardForge exported " + block.code + " -> " + destination);
                    complete++;
                }
                catch (Exception ex)
                {
                    Debug.LogError("CardForge failed exporting " + block.code + ": " + ex);
                    if (Application.isBatchMode) EditorApplication.Exit(1);
                    throw;
                }
            }
            Debug.Log("CardForge complete: " + complete + " block bundles for " + target);
        }
    }
}
