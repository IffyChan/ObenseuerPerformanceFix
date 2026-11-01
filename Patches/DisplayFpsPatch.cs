using System;
using HarmonyLib;
using UnityEngine;

namespace ObenseuerPerformanceFix.Patches
{
    [HarmonyPatch(typeof(DisplaySave), nameof(DisplaySave.ApplyFrameRateLimit))]
    public static class DisplayFpsPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(int preferredRefreshRate)
        {
            if (!Plugin.Enabled.Value || !Plugin.FixVsyncConflict.Value)
            {
                return true; // Run vanilla code
            }

            try
            {
                if (QualitySettings.vSyncCount > 0)
                {
                    // VSync is ON: Unity MUST NOT use software thread sleeping (targetFrameRate).
                    // Setting targetFrameRate = -1 allows hardware VSync (VBlank) to govern presentation timing smoothly
                    // without missing the deadline and dropping down to 30 FPS in borderless mode.
                    Application.targetFrameRate = -1;
                    Plugin.Log?.LogInfo($"[DisplayFpsPatch] VSync is ON (vSyncCount={QualitySettings.vSyncCount}). Applied targetFrameRate = -1 (disabling conflicting engine sleeper).");
                }
                else
                {
                    // VSync is OFF: apply configured target limit or unlocked (-1)
                    int targetFps = Plugin.TargetFpsWithVsyncOff.Value;
                    if (targetFps == 0) targetFps = -1;
                    Application.targetFrameRate = targetFps;
                    Plugin.Log?.LogInfo($"[DisplayFpsPatch] VSync is OFF. Applied configured targetFrameRate = {targetFps}.");
                }

                // Also ensure queued frames setting is maintained
                if (Plugin.MaxQueuedFrames.Value > 0)
                {
                    QualitySettings.maxQueuedFrames = Plugin.MaxQueuedFrames.Value;
                }

                return false; // Skip vanilla method
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[DisplayFpsPatch] Error in ApplyFrameRateLimit prefix: {ex}");
                return true;
            }
        }
    }
}
