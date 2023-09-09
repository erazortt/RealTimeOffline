// <copyright file="OutsideConnectionAIPatch.cs" company="dymanoid">
// Copyright (c) dymanoid. All rights reserved.
// </copyright>

namespace RealTime.Patches
{
    using HarmonyLib;
    using RealTime.CustomAI;

    /// <summary>
    /// A static class that provides the patch objects for the outside connections AI.
    /// </summary>
    [HarmonyPatch]
    internal static class OutsideConnectionAIPatch
    {
        /// <summary>Gets or sets the spare time behavior simulation.</summary>
        public static ISpareTimeBehavior SpareTimeBehavior { get; set; }

        [HarmonyPatch]
        private sealed class OutsideConnectionAI_DummyTrafficProbability
        {
            [HarmonyPatch(typeof(OutsideConnectionAI), "DummyTrafficProbability")]
            [HarmonyPostfix]
            private static void Postfix(ref int __result)
            {
                // Using the relaxing chance of an adult as base value - seems to be reasonable.
                int chance = (int)SpareTimeBehavior.GetRelaxingChance(Citizen.AgeGroup.Adult);
                __result = __result * chance * chance / 10_000;
            }
        }
    }
}
