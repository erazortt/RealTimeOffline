namespace RealTime.Patches
{
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

        }
    }
}
