// <copyright file="WeatherManagerPatch.cs" company="dymanoid">
// Copyright (c) dymanoid. All rights reserved.
// </copyright>

namespace RealTime.Patches
{
    using System;
    using HarmonyLib;

    /// <summary>
    /// A static class that provides the patch objects for the weather AI .
    /// </summary>
    [HarmonyPatch]
    internal static class WeatherManagerPatch
    {
        /// <summary>Gets the patch object for the simulation method.</summary>
        [HarmonyPatch]
        private sealed class WeatherManager_SimulationStepImpl
        {
            [HarmonyPatch(typeof(WeatherManager), "SimulationStepImpl")]
            [HarmonyPrefix]
            private static void Prefix(ref float ___m_temperatureSpeed, float ___m_targetTemperature, float ___m_currentTemperature)
            {
                // The maximum temperature change speed is now 1/20 of the original
                float delta = Math.Abs((___m_targetTemperature - ___m_currentTemperature) * 0.000_05f);
                delta = Math.Min(Math.Abs(___m_temperatureSpeed) + 0.000_01f, delta);
                ___m_temperatureSpeed = delta - 0.000_099f;
            }
        }
    }
}
