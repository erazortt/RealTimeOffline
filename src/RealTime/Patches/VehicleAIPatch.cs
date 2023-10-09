namespace RealTime.Patches
{
    using System;
    using ColossalFramework;
    using HarmonyLib;
    using UnityEngine;

    [HarmonyPatch]
    internal static class VehicleAIPatch
    {
        [HarmonyPatch]
        private sealed class FireTruckAI_ExtinguishFire
        {
            private static int GetBuildingVolume(BuildingInfoGen buildingInfoGen)
            {
                float gridSizeX = (buildingInfoGen.m_max.x - buildingInfoGen.m_min.x) / 16f;
                float gridSizeY = (buildingInfoGen.m_max.z - buildingInfoGen.m_min.z) / 16f;
                float gridArea = gridSizeX * gridSizeY;

                float volume = 0f;
                float[] heights = buildingInfoGen.m_heights;
                for (int i = 0; i < heights.Length; i++)
                {
                    volume += gridArea * heights[i];
                }
                return (int)volume;
            }

            [HarmonyPatch(typeof(FireTruckAI), "ExtinguishFire")]
            [HarmonyPrefix]
            private static bool Prefix(FireTruckAI __instance, ushort vehicleID, ref Vehicle data, ushort buildingID, ref Building buildingData, ref bool __result)
            {
                int volume = GetBuildingVolume(buildingData.Info.m_generatedInfo);
                int num = Mathf.Min(5000, buildingData.m_fireIntensity * volume);
                if (num != 0)
                {
                    int num2 = Singleton<SimulationManager>.instance.m_randomizer.Int32(1, __instance.m_fireFightingRate);
                    num = Mathf.Max(num - num2, 0);
                    num *= volume * 8;
                    buildingData.m_fireIntensity = (byte)num;
                    if (num == 0)
                    {
                        InstanceID id = default;
                        id.Building = buildingID;
                        Singleton<InstanceManager>.instance.SetGroup(id, null);
                        if (data.m_sourceBuilding != 0)
                        {
                            int tempExport = Singleton<BuildingManager>.instance.m_buildings.m_buffer[data.m_sourceBuilding].m_tempExport;
                            Singleton<BuildingManager>.instance.m_buildings.m_buffer[data.m_sourceBuilding].m_tempExport = (byte)Mathf.Min(tempExport + 1, 255);
                        }
                        var flags = buildingData.m_flags;
                        if (buildingData.m_productionRate != 0 && (buildingData.m_flags & Building.Flags.Evacuating) == 0)
                        {
                            buildingData.m_flags |= Building.Flags.Active;
                        }
                        var flags2 = buildingData.m_flags;
                        Singleton<BuildingManager>.instance.UpdateBuildingRenderer(buildingID, buildingData.GetLastFrameData().m_fireDamage == 0 || (buildingData.m_flags & (Building.Flags.Abandoned | Building.Flags.Collapsed)) != 0);
                        Singleton<BuildingManager>.instance.UpdateBuildingColors(buildingID);
                        if (flags2 != flags)
                        {
                            Singleton<BuildingManager>.instance.UpdateFlags(buildingID, flags2 ^ flags);
                        }
                        var properties = Singleton<GuideManager>.instance.m_properties;
                        if (properties != null)
                        {
                            Singleton<BuildingManager>.instance.m_buildingOnFire.Deactivate(buildingID, soft: false);
                        }
                    }
                    if (buildingData.m_subBuilding != 0 && buildingData.m_parentBuilding == 0)
                    {
                        int num3 = 0;
                        ushort subBuilding = buildingData.m_subBuilding;
                        while (subBuilding != 0)
                        {
                            Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding].m_fireIntensity = (byte)Mathf.Min(num, Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding].m_fireIntensity);
                            if (num == 0)
                            {
                                InstanceID id2 = default;
                                id2.Building = subBuilding;
                                Singleton<InstanceManager>.instance.SetGroup(id2, null);
                                Singleton<BuildingManager>.instance.UpdateBuildingRenderer(subBuilding, updateGroup: true);
                                Singleton<BuildingManager>.instance.UpdateBuildingColors(subBuilding);
                            }
                            subBuilding = Singleton<BuildingManager>.instance.m_buildings.m_buffer[subBuilding].m_subBuilding;
                            if (++num3 > 49152)
                            {
                                CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                break;
                            }
                        }
                    }
                }
                __result = num == 0;
                return false;
            }

        }
    }
}
