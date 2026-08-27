#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Arkanoid.StudioEditor
{
    /// <summary>Создаёт URP asset и вешает в Graphics/Quality — иначе Game View «розовый/фиолетовый».</summary>
    public static class UrpSetupMenu
    {
        private const string Folder = "Assets/_Project/Settings";
        private const string RendererPath = Folder + "/URP_Renderer.asset";
        private const string PipelinePath = Folder + "/URP_Pipeline.asset";

        [MenuItem("Arkanoid/Project/Setup URP Pipeline (fix pink screen)")]
        public static void SetupUrp()
        {
            EnsureFolder("Assets/_Project");
            EnsureFolder(Folder);

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }
            else
            {
                // Убедиться что renderer привязан
                var so = new SerializedObject(pipeline);
                var list = so.FindProperty("m_RendererDataList");
                if (list != null && list.arraySize > 0)
                {
                    list.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            EditorUtility.SetDirty(pipeline);
            AssetDatabase.SaveAssets();

            Debug.Log("[Arkanoid] URP Pipeline назначен: " + PipelinePath +
                      ". Перезапусти Play — розовый экран должен уйти.");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
#endif
