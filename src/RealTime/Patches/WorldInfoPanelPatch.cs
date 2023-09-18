// <copyright file="WorldInfoPanelPatch.cs" company="dymanoid">
// Copyright (c) dymanoid. All rights reserved.
// </copyright>

namespace RealTime.Patches
{
    using ColossalFramework;
    using ColossalFramework.UI;
    using HarmonyLib;
    using RealTime.CustomAI;
    using RealTime.UI;

    /// <summary>
    /// A static class that provides the patch objects for the world info panel game methods.
    /// </summary>
    [HarmonyPatch]
    internal static class WorldInfoPanelPatch
    {
        /// <summary>Gets or sets the custom AI object for buildings.</summary>
        public static RealTimeBuildingAI RealTimeAI { get; set; }

        /// <summary>Gets or sets the customized citizen information panel.</summary>
        public static CustomCitizenInfoPanel CitizenInfoPanel { get; set; }

        /// <summary>Gets or sets the customized vehicle information panel.</summary>
        public static CustomVehicleInfoPanel VehicleInfoPanel { get; set; }

        /// <summary>Gets or sets the customized campus information panel.</summary>
        public static CustomCampusWorldInfoPanel CampusWorldInfoPanel { get; set; }

        [HarmonyPatch]
        private sealed class WorldInfoPanel_UpdateBindings
        {
            [HarmonyPatch(typeof(WorldInfoPanel), "UpdateBindings")]
            [HarmonyPostfix]
            private static void Postfix(WorldInfoPanel __instance, ref InstanceID ___m_InstanceID)
            {
                switch (__instance)
                {
                    case CitizenWorldInfoPanel _:
                        CitizenInfoPanel?.UpdateCustomInfo(ref ___m_InstanceID);
                        break;

                    case VehicleWorldInfoPanel _:
                        VehicleInfoPanel?.UpdateCustomInfo(ref ___m_InstanceID);
                        break;

                    case CampusWorldInfoPanel _:
                        CampusWorldInfoPanel?.UpdateCustomInfo(ref ___m_InstanceID);
                        break;
                }
            }
        }

        [HarmonyPatch]
        private sealed class CityServiceWorldInfoPanel_UpdateBindings
        {
            [HarmonyPatch(typeof(CityServiceWorldInfoPanel), "UpdateBindings")]
            [HarmonyPostfix]
            private static void Postfix(CityServiceWorldInfoPanel __instance, ref InstanceID ___m_InstanceID, ref UILabel ___m_Status)
            {
                //if (RealTimeAI.ShouldSwitchBuildingLightsOff(___m_InstanceID.Building) && ___m_Status != null)
                //{
                //    ___m_Status.text = "Closed";
                //}
            }
        }
    }
}
