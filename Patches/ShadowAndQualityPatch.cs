using System;
using HarmonyLib;
using UnityEngine;

namespace ObenseuerPerformanceFix.Patches
{
    [HarmonyPatch(typeof(OptionsMenu), nameof(OptionsMenu.applyQualitySettings))]
    public static class ShadowAndQualityPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (!Plugin.Enabled.Value) return;

            try
            {
                Plugin.ApplyDirectQualitySettings();
                Plugin.Log?.LogInfo($"[ShadowAndQualityPatch] Applied quality overrides: maxQueuedFrames={QualitySettings.maxQueuedFrames}, shadowCascades={QualitySettings.shadowCascades}, shadowDistance={QualitySettings.shadowDistance}, lodBias={QualitySettings.lodBias}.");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[ShadowAndQualityPatch] Error in applyQualitySettings postfix: {ex}");
            }
        }
    }
}
