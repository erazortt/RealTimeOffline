namespace RealTime.Patches
{
    using System;
    using HarmonyLib;
    using RealTime.CustomAI;
    using UnityEngine;

    [HarmonyPatch]
    internal static class VehicleAIPatch
    {
        // <summary>Gets or sets the custom AI object for buildings.</summary>
        public static RealTimeBuildingAI RealTimeAI { get; set; }

        [HarmonyPatch]
        private sealed class FireTruckAI_ExtinguishFire
        {

            [HarmonyPatch(typeof(FireTruckAI), "ExtinguishFire")]
            [HarmonyPrefix]
            private static bool Prefix(FireTruckAI __instance, ushort vehicleID, ref Vehicle data, ushort buildingID, ref Building buildingData, ref bool __result)
            {
                RealTimeAI.CreateBuildingFire(data.m_targetBuilding);
                if (RealTimeAI.ShouldExtinguishFire(buildingID))
                {
                    return true;
                }
                return false;
            }

            [HarmonyPatch(typeof(FireTruckAI), "SetTarget")]
            [HarmonyPrefix]
            private static void SetTarget(ushort vehicleID, ref Vehicle data, ushort targetBuilding)
            {
                if (targetBuilding == 0)
                {
                    RealTimeAI.RemoveBuildingFire(data.m_targetBuilding);
                }
            }

        }

        [HarmonyPatch]
        private sealed class FireCopterAI_ExtinguishFire
        {
            [HarmonyPatch(typeof(FireCopterAI), "ExtinguishFire",
                new Type[] { typeof(ushort), typeof(Vehicle), typeof(ushort), typeof(Building) },
                new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Ref })]
            [HarmonyPrefix]
            private static bool Prefix(FireCopterAI __instance, ushort vehicleID, ref Vehicle data, ushort buildingID, ref Building buildingData, ref bool __result)
            {
                RealTimeAI.CreateBuildingFire(data.m_targetBuilding);
                if (RealTimeAI.ShouldExtinguishFire(buildingID))
                {
                    return true;
                }
                int num2 = Mathf.Min(__instance.m_fireFightingRate, data.m_transferSize);
                data.m_transferSize = (ushort)(data.m_transferSize - num2);
                return false;
            }

            [HarmonyPatch(typeof(FireCopterAI), "SetTarget")]
            [HarmonyPrefix]
            private static void SetTarget(ushort vehicleID, ref Vehicle data, ushort targetBuilding)
            {
                if (targetBuilding == 0)
                {
                    RealTimeAI.RemoveBuildingFire(data.m_targetBuilding);
                }
            }

        }
    }
}
