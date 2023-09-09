// <copyright file="TimeControlCompatibility.cs" company="dymanoid">
// Copyright (c) dymanoid. All rights reserved.
// </copyright>

namespace RealTime.Simulation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using ColossalFramework.Plugins;
    using HarmonyLib;
    using SkyTools.Tools;

    /// <summary>
    /// A special class that handles compatibility with other time-changing mods by patching their methods.
    /// </summary>
    [HarmonyPatch]
    internal static class TimeControlCompatibility
    {
        private const string SimulationStepMethodName = "SimulationStep";
        private const string TimeOfDayPropertySetter = "set_TimeOfDay";
        private const string TimeWarpManagerType = "TimeWarpMod.SunManager";
        private const string UltimateEyecandyManagerType = "UltimateEyecandy.DayNightCycleManager";
        private const ulong TimeWarpWorkshopId = 814698320ul;
        private const ulong UltimateEyecandyWorkshopId = 672248733ul;

        private static Type GetManagerType(ulong modId, string typeName)
        {
            var mod = PluginManager.instance.GetPluginsInfo().FirstOrDefault(pi => pi.publishedFileID.AsUInt64 == modId);

            if (mod?.isEnabled != true)
            {
                return null;
            }

            var assembly = mod.GetAssemblies()?.FirstOrDefault();
            if (assembly == null)
            {
                Log.Warning($"'Real Time' compatibility check: the mod {modId} has no assemblies.");
                return null;
            }

            try
            {
                return assembly.GetType(typeName);
            }
            catch (Exception ex)
            {
                Log.Warning($"'Real Time' compatibility check: the mod {modId} doesn't contain the '{typeName}' type: {ex}");
                return null;
            }
        }


        [HarmonyPatch]
        private sealed class SimulationStepPatch
        {
            private static bool Prepare() => TargetMethods().Any();

            private static IEnumerable<MethodBase> TargetMethods()
            {
                var timeWarpType = GetManagerType(TimeWarpWorkshopId, TimeWarpManagerType);
                var ultimateEyecandyType = GetManagerType(UltimateEyecandyWorkshopId, UltimateEyecandyManagerType);

                if (timeWarpType != null)
                {
                    var timeWarpSimulationStep = AccessTools.Method(timeWarpType, "SimulationStep");
                    if (timeWarpSimulationStep != null)
                    {
                        yield return timeWarpSimulationStep;
                    }
                }
                if (ultimateEyecandyType != null)
                {
                    var ultimateEyecandySimulationStep = AccessTools.Method(ultimateEyecandyType, "SimulationStep");
                    if (ultimateEyecandySimulationStep != null)
                    {
                        yield return ultimateEyecandySimulationStep;
                    }

                }
            }

            [HarmonyPrefix]
            private static bool Prefix(ref uint ___dayOffsetFrames)
            {
                bool result = SimulationManager.instance.SimulationPaused;
                if (!result)
                {
                    ___dayOffsetFrames = SimulationManager.instance.m_dayTimeOffsetFrames;
                }

                return result;
            }
        }

        [HarmonyPatch]
        private sealed class SetTimeOfDayPatch
        {
            private static bool Prepare() => TargetMethods().Any();

            private static IEnumerable<MethodBase> TargetMethods()
            {
                var timeWarpType = GetManagerType(TimeWarpWorkshopId, TimeWarpManagerType);
                var ultimateEyecandyType = GetManagerType(UltimateEyecandyWorkshopId, UltimateEyecandyManagerType);

                if (timeWarpType != null)
                {
                    var timeWarpTimeOfDay = AccessTools.PropertySetter(timeWarpType, "TimeOfDay");
                    if (timeWarpTimeOfDay != null)
                    {
                        yield return timeWarpTimeOfDay;
                    }

                }
                if (ultimateEyecandyType != null)
                {
                    var ultimateEyecandyTimeOfDay = AccessTools.PropertySetter(ultimateEyecandyType, "TimeOfDay");
                    if (ultimateEyecandyTimeOfDay != null)
                    {
                        yield return ultimateEyecandyTimeOfDay;
                    }
                }
            }

            [HarmonyPrefix]
            private static void Prefix(float value)
            {
                if (Math.Abs(value - SimulationManager.instance.m_currentDayTimeHour) >= 0.03f)
                {
                    SimulationManager.instance.SimulationPaused = true;
                }
            }
        }
    }
}
