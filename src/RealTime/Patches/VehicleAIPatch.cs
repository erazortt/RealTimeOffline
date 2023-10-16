namespace RealTime.Patches
{
    using System;
    using HarmonyLib;
    using RealTime.CustomAI;

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
                if (RealTimeAI.ShouldExtinguishFire(buildingID))
                {
                    return true;
                }
                return false;
            }

            [HarmonyPatch(typeof(FireTruckAI), "ArriveAtTarget")]
            [HarmonyPrefix]
            private static void ArriveAtTarget(ushort vehicleID, ref Vehicle data)
            {
                if (data.m_targetBuilding != 0)
                {
                    RealTimeAI.CreateBuildingFire(data.m_targetBuilding);
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
            private static bool Prefix(FireTruckAI __instance, ushort vehicleID, ref Vehicle data, ushort buildingID, ref Building buildingData, ref bool __result)
            {
                if (RealTimeAI.ShouldExtinguishFire(buildingID))
                {
                    return true;
                }
                return false;
            }

            [HarmonyPatch(typeof(FireCopterAI), "ArriveAtTarget")]
            [HarmonyPrefix]
            private static void ArriveAtTarget(ushort vehicleID, ref Vehicle data)
            {
                if (data.m_targetBuilding != 0)
                {
                    RealTimeAI.CreateBuildingFire(data.m_targetBuilding);
                }
            }

        }
    }
}
