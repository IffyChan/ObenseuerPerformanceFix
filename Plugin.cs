using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.Mono;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ObenseuerPerformanceFix
{
    [BepInPlugin(Guid, Name, Version)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "obenseuer.ifchan.performancefix";
        public const string Name = "Obenseuer Performance Fix";
        public const string Version = "1.0.0";

        internal static ManualLogSource Log;
        private Harmony harmony;

        // Master switch
        internal static ConfigEntry<bool> Enabled;

        // Display & FPS fixes
        internal static ConfigEntry<bool> FixVsyncConflict;
        internal static ConfigEntry<int> TargetFpsWithVsyncOff;
        internal static ConfigEntry<int> MaxQueuedFrames;

        // Shadows optimizations
        internal static ConfigEntry<bool> SmartShadowCulling;
        internal static ConfigEntry<float> MinShadowMeshVolume;
        internal static ConfigEntry<bool> OptimizeShadowCascades;
        internal static ConfigEntry<float> MaxShadowDistance;

        // Reflection & Lighting optimizations
        internal static ConfigEntry<bool> ReflectionProbeTimeSlicing;
        internal static ConfigEntry<bool> AutoGpuInstancing;
        internal static ConfigEntry<float> LodBiasCap;

        private void Awake()
        {
            Log = Logger;

            // [Mod]
            Enabled = Config.Bind("Mod", "Enabled", true,
                "Master switch. If false, all performance patches and overrides are inactive.");

            // [Display]
            FixVsyncConflict = Config.Bind("Display", "FixVsyncConflict", true,
                "Fixes Unity 30 FPS lock in Borderless window by setting Application.targetFrameRate = -1 when VSync is ON.");

            TargetFpsWithVsyncOff = Config.Bind("Display", "TargetFpsWithVsyncOff", -1,
                new ConfigDescription("Target FPS limit when VSync is OFF. -1 means Unlocked, or set 60, 120, 144, 240.",
                    new AcceptableValueRange<int>(-1, 360)));

            MaxQueuedFrames = Config.Bind("Display", "MaxQueuedFrames", 3,
                new ConfigDescription("DirectX swapchain queued frames. Setting to 3 prevents half-refresh rate drops during frame time spikes.",
                    new AcceptableValueRange<int>(1, 4)));

            // [Shadows]
            SmartShadowCulling = Config.Bind("Shadows", "SmartShadowCulling", true,
                "Disables shadow casting on micro-props (cans, debris, cigarette butts, small trash) saving up to 60% shadow draw calls.");

            MinShadowMeshVolume = Config.Bind("Shadows", "MinShadowMeshVolume", 0.08f,
                new ConfigDescription("Minimum bounding box volume (cubic meters) for an object to cast shadows. Default 0.08 m^3.",
                    new AcceptableValueRange<float>(0.001f, 1.0f)));

            OptimizeShadowCascades = Config.Bind("Shadows", "OptimizeCascades", true,
                "Clamps shadow cascades to 2 instead of 4, cutting shadow map render passes in half while preserving crisp shadows.");

            MaxShadowDistance = Config.Bind("Shadows", "MaxShadowDistance", 50f,
                new ConfigDescription("Maximum shadow distance in meters (clamped from vanilla 150m down to 50m to avoid drawing distant shadows).",
                    new AcceptableValueRange<float>(10f, 150f)));

            // [ReflectionProbes]
            ReflectionProbeTimeSlicing = Config.Bind("ReflectionProbes", "TimeSlicing", true,
                "Enables individual face time-slicing and removes shadow passes on reflection probes to eliminate periodic 36ms outdoor hitches.");

            // [Materials]
            AutoGpuInstancing = Config.Bind("Materials", "AutoGpuInstancing", true,
                "Enables GPU Instancing on supported scene materials to batch identical meshes into single draw calls.");

            // [LOD]
            LodBiasCap = Config.Bind("LOD", "LodBiasCap", 1.5f,
                new ConfigDescription("Clamps maximum LOD bias (vanilla sets 3.0 on Ultra, which overloads the GPU with millions of distant triangles).",
                    new AcceptableValueRange<float>(1.0f, 3.0f)));

            if (!Enabled.Value)
            {
                Log.LogInfo($"{Name} is disabled in config.");
                return;
            }

            try
            {
                harmony = new Harmony(Guid);
                harmony.PatchAll(typeof(Plugin).Assembly);
                Log.LogInfo($"{Name} {Version} successfully loaded and patched.");
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to apply Harmony patches: {ex}");
            }

            // Apply direct runtime settings
            ApplyDirectQualitySettings();

            // Hook scene load for geometry / material optimizations
            SceneManager.sceneLoaded += Patches.SceneOptimizer.OnSceneLoaded;
        }

        internal static void ApplyDirectQualitySettings()
        {
            if (!Enabled.Value) return;

            try
            {
                if (MaxQueuedFrames.Value > 0)
                {
                    QualitySettings.maxQueuedFrames = MaxQueuedFrames.Value;
                }

                if (OptimizeShadowCascades.Value && QualitySettings.shadowCascades > 2)
                {
                    QualitySettings.shadowCascades = 2;
                }

                if (QualitySettings.shadowDistance > MaxShadowDistance.Value)
                {
                    QualitySettings.shadowDistance = MaxShadowDistance.Value;
                }

                if (QualitySettings.lodBias > LodBiasCap.Value)
                {
                    QualitySettings.lodBias = LodBiasCap.Value;
                }

                // If VSync is currently ON, enforce targetFrameRate = -1 immediately
                if (FixVsyncConflict.Value && QualitySettings.vSyncCount > 0)
                {
                    Application.targetFrameRate = -1;
                }
            }
            catch (Exception ex)
            {
                Log?.LogWarning($"Error applying quality overrides: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= Patches.SceneOptimizer.OnSceneLoaded;
            harmony?.UnpatchSelf();
            Log?.LogInfo($"{Name} unloaded.");
        }
    }
}
