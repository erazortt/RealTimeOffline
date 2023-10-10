namespace RealTime.Patches
{
    using ColossalFramework;
    using HarmonyLib;
    using UnityEngine;
    using static Citizen;

    internal class CitizenPatch
    {
        [HarmonyPatch]
        private sealed class Citizen_GetGarbageAccumulation
        {
            [HarmonyPatch(typeof(Citizen), "GetGarbageAccumulation")]
            [HarmonyPrefix]
            public static bool GetGarbageAccumulation(Education educationLevel, ref int __result)
            {
                switch(educationLevel)
                {
                    case Education.Uneducated:
                        __result = 10;
                        break;
                    case Education.OneSchool:
                        __result = 9;
                        break;
                    case Education.TwoSchools:
                        __result = 8;
                        break;
                    case Education.ThreeSchools:
                        __result = 7;
                        break;
                    default:
                        __result = 0;
                        break;
                };
                return false;
            }
        }

        [HarmonyPatch]
        private sealed class Citizen_GetMailAccumulation
        {
            [HarmonyPatch(typeof(Citizen), "GetMailAccumulation")]
            [HarmonyPrefix]
            public static bool GetMailAccumulation(Education educationLevel, ref int __result)
            {
                switch (educationLevel)
                {
                    case Education.Uneducated:
                        __result = 7;
                        break;
                    case Education.OneSchool:
                        __result = 8;
                        break;
                    case Education.TwoSchools:
                        __result = 9;
                        break;
                    case Education.ThreeSchools:
                        __result = 10;
                        break;
                    default:
                        __result = 0;
                        break;
                };
                return false;
            }
        }

        [HarmonyPatch]
        private sealed class Citizen_ResetHotel
        {
            [HarmonyPatch(typeof(Citizen), "ResetHotel")]
            [HarmonyPrefix]
            public static bool ResetHotel(Citizen __instance, uint citizenID, uint unitID)
            {
                if (__instance.m_hotelBuilding != 0)
                {
                    var buffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
                    if (unitID == 0)
                    {
                        unitID = buffer[__instance.m_hotelBuilding].m_citizenUnits;
                    }
                    __instance.RemoveFromUnits(citizenID, unitID, CitizenUnit.Flags.Hotel);
                    var buildingInfo = buffer[__instance.m_hotelBuilding].Info;
                    if (buildingInfo.m_buildingAI is HotelAI hotelAI)
                    {
                        hotelAI.RemoveGuest(__instance.m_hotelBuilding, ref buffer[__instance.m_hotelBuilding]);
                    }
                    else if (buildingInfo.m_class.m_service == ItemClass.Service.Commercial && buildingInfo.m_class.m_subService == ItemClass.SubService.CommercialTourist && (buildingInfo.name.Contains("hotel") || buildingInfo.name.Contains("Hotel")))
                    {
                        buffer[__instance.m_hotelBuilding].m_roomUsed = (ushort)Mathf.Max(buffer[__instance.m_hotelBuilding].m_roomUsed - 1, 0);
                    }
                    __instance.m_hotelBuilding = 0;
                }
                return false;
            }
        }

        [HarmonyPatch]
        private sealed class Citizen_SetHotel
        {
            [HarmonyPatch(typeof(Citizen), "SetHotel")]
            [HarmonyPrefix]
            public static bool SetHotel(Citizen __instance, uint citizenID, ushort buildingID, uint unitID)
            {
                __instance.ResetHotel(citizenID, unitID);
                if (unitID != 0)
                {
                    var buffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
                    var instance = Singleton<CitizenManager>.instance;
                    if (__instance.AddToUnit(citizenID, ref instance.m_units.m_buffer[unitID]))
                    {
                        __instance.m_hotelBuilding = instance.m_units.m_buffer[unitID].m_building;
                        __instance.WealthLevel = GetWealthLevel(buffer[__instance.m_hotelBuilding].Info.m_class.m_level);
                        var buildingInfo = buffer[__instance.m_hotelBuilding].Info;
                        if (buildingInfo.m_buildingAI is HotelAI hotelAI)
                        {
                            hotelAI.AddGuest(__instance.m_hotelBuilding, ref buffer[__instance.m_hotelBuilding]);
                        }
                        else if (buildingInfo.m_class.m_service == ItemClass.Service.Commercial && buildingInfo.m_class.m_subService == ItemClass.SubService.CommercialTourist && (buildingInfo.name.Contains("hotel") || buildingInfo.name.Contains("Hotel")))
                        {
                            buffer[__instance.m_hotelBuilding].m_roomUsed = (ushort)Mathf.Min(buffer[__instance.m_hotelBuilding].m_roomUsed + 1, buffer[__instance.m_hotelBuilding].m_roomMax);
                        }
                    }
                }
                else
                {
                    if (buildingID == 0)
                    {
                        return false;
                    }
                    var buffer2 = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
                    if (__instance.AddToUnits(citizenID, buffer2[buildingID].m_citizenUnits, CitizenUnit.Flags.Hotel))
                    {
                        __instance.m_hotelBuilding = buildingID;
                        __instance.WealthLevel = GetWealthLevel(buffer2[__instance.m_hotelBuilding].Info.m_class.m_level);
                        var buildingInfo = buffer2[__instance.m_hotelBuilding].Info;
                        if (buildingInfo.m_buildingAI is HotelAI hotelAI)
                        {
                            hotelAI.AddGuest(__instance.m_hotelBuilding, ref buffer2[__instance.m_hotelBuilding]);
                        }
                        else if (buildingInfo.m_class.m_service == ItemClass.Service.Commercial && buildingInfo.m_class.m_subService == ItemClass.SubService.CommercialTourist && (buildingInfo.name.Contains("hotel") || buildingInfo.name.Contains("Hotel")))
                        {
                            buffer2[__instance.m_hotelBuilding].m_roomUsed = (ushort)Mathf.Min(buffer2[__instance.m_hotelBuilding].m_roomUsed + 1, buffer2[__instance.m_hotelBuilding].m_roomMax);
                        }
                    }
                }
                return false;
            }
        }
    }
}
