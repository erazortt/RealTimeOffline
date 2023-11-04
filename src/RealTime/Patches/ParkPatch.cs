// <copyright file="ParkPatch.cs" company="dymanoid">
// Copyright (c) dymanoid. All rights reserved.
// </copyright>

namespace RealTime.Patches
{
    using HarmonyLib;
    using RealTime.CustomAI;

    /// <summary>
    /// A static class that provides the patch objects for the Park Life DLC related methods.
    /// </summary>
    [HarmonyPatch]
    internal static class ParkPatch
    {
        /// <summary>Gets or sets the city spare time behavior.</summary>
        public static ISpareTimeBehavior SpareTimeBehavior { get; set; }

        [HarmonyPatch]
        private sealed class DistrictPark_SimulationStep
        {
            [HarmonyPatch(typeof(DistrictPark), "SimulationStep")]
            [HarmonyPostfix]
            private static void Postfix(byte parkID)
            {
                if(parkID != 0)
                {
                    ref var park = ref DistrictManager.instance.m_parks.m_buffer[parkID];

                    if (SpareTimeBehavior!= null && !SpareTimeBehavior.AreFireworksAllowed)
                    {
                        park.m_flags &= ~DistrictPark.Flags.SpecialMode;
                        return;
                    }

                    if (park.m_dayNightCount == 6 || (park.m_parkPolicies & DistrictPolicies.Park.FireworksBoost) != 0)
                    {
                        park.m_flags |= DistrictPark.Flags.SpecialMode;
                    }
                }
            }
        }
    }
}
