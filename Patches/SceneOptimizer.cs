using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ObenseuerPerformanceFix.Patches
{
    public static class SceneOptimizer
    {
        private static readonly HashSet<int> ProtectedLayers = new HashSet<int>();
        private static bool layersInitialized = false;

        private static void InitProtectedLayers()
        {
            if (layersInitialized) return;
            layersInitialized = true;

            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer != -1) ProtectedLayers.Add(playerLayer);

            int charLayer = LayerMask.NameToLayer("Characters");
            if (charLayer != -1) ProtectedLayers.Add(charLayer);

            int npcLayer = LayerMask.NameToLayer("NPC");
            if (npcLayer != -1) ProtectedLayers.Add(npcLayer);
        }

        public static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!Plugin.Enabled.Value) return;

            try
            {
                InitProtectedLayers();
                OptimizeSceneObjects(scene);
                OptimizeReflectionProbes(scene);
                OptimizeMaterials(scene);

                Plugin.ApplyDirectQualitySettings();
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[SceneOptimizer] Error during scene optimization: {ex}");
            }
        }

        private static void OptimizeSceneObjects(Scene scene)
        {
            if (!Plugin.SmartShadowCulling.Value) return;

            float minVolume = Plugin.MinShadowMeshVolume.Value;
            int culledShadowsCount = 0;
            int totalMeshRenderers = 0;

            GameObject[] rootObjects = scene.GetRootGameObjects();
            foreach (GameObject root in rootObjects)
            {
                if (root == null) continue;

                MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
                totalMeshRenderers += renderers.Length;

                foreach (MeshRenderer mr in renderers)
                {
                    if (mr == null || mr.shadowCastingMode == ShadowCastingMode.Off) continue;

                    // Skip protected objects like player or characters
                    if (ProtectedLayers.Contains(mr.gameObject.layer)) continue;

                    Bounds bounds = mr.bounds;
                    Vector3 size = bounds.size;
                    float volume = size.x * size.y * size.z;

                    // Micro debris, trash, paper, small bottles/cans have tiny volume (< 0.08 m^3)
                    if (volume > 0f && volume < minVolume)
                    {
                        mr.shadowCastingMode = ShadowCastingMode.Off;
                        culledShadowsCount++;
                    }
                }
            }

            Plugin.Log?.LogInfo($"[SceneOptimizer] Micro-Prop Shadow Cull in '{scene.name}': disabled shadows on {culledShadowsCount} of {totalMeshRenderers} objects (volume < {minVolume:F3} m^3).");
        }

        private static void OptimizeReflectionProbes(Scene scene)
        {
            if (!Plugin.ReflectionProbeTimeSlicing.Value) return;

            int probeCount = 0;
            GameObject[] rootObjects = scene.GetRootGameObjects();

            foreach (GameObject root in rootObjects)
            {
                if (root == null) continue;

                ReflectionProbe[] probes = root.GetComponentsInChildren<ReflectionProbe>(true);
                foreach (ReflectionProbe probe in probes)
                {
                    if (probe == null) continue;

                    // Spread cubemap updates over individual frames instead of rendering 6 faces in one frame
                    probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;

                    // Probe internal shadows are the primary cause of 36ms outdoor hitches (as proven in periodic-rendering.md)
                    probe.shadowDistance = 0f;
                    probeCount++;
                }
            }

            if (probeCount > 0)
            {
                Plugin.Log?.LogInfo($"[SceneOptimizer] Time-slicing and shadow culling applied to {probeCount} ReflectionProbes in '{scene.name}'.");
            }
        }

        private static void OptimizeMaterials(Scene scene)
        {
            if (!Plugin.AutoGpuInstancing.Value) return;

            var processedMaterials = new HashSet<Material>();
            int instancedCount = 0;

            GameObject[] rootObjects = scene.GetRootGameObjects();
            foreach (GameObject root in rootObjects)
            {
                if (root == null) continue;

                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer r in renderers)
                {
                    if (r == null) continue;

                    Material[] mats = r.sharedMaterials;
                    if (mats == null) continue;

                    foreach (Material mat in mats)
                    {
                        if (mat == null || processedMaterials.Contains(mat)) continue;
                        processedMaterials.Add(mat);

                        try
                        {
                            if (mat.shader != null && !mat.enableInstancing)
                            {
                                mat.enableInstancing = true;
                                instancedCount++;
                            }
                        }
                        catch
                        {
                            // Some special shaders may disallow instancing; safely catch and proceed
                        }
                    }
                }
            }

            if (instancedCount > 0)
            {
                Plugin.Log?.LogInfo($"[SceneOptimizer] Enabled GPU Instancing on {instancedCount} materials in '{scene.name}'.");
            }
        }
    }
}
