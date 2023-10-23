namespace RealTime.Patches
{
    using ColossalFramework;
    using HarmonyLib;
    using UnityEngine;

    [HarmonyPatch]
    internal class InstanceManagerPatch
    {
        [HarmonyPatch]
        private sealed class InstanceManager_GetLocation
        {
            [HarmonyPatch(typeof(InstanceManager), "GetLocation")]
            [HarmonyPrefix]
            private static bool Prefix(InstanceID id, ref InstanceID __result)
            {
                if (id.Citizen != 0)
                {
                    var citizenManager = Singleton<CitizenManager>.instance;
                    ushort num = citizenManager.m_citizens.m_buffer[id.Citizen].m_instance;
                    if (num != 0 && (citizenManager.m_instances.m_buffer[num].m_flags & CitizenInstance.Flags.Character) != 0)
                    {
                        id.CitizenInstance = num;
                    }
                    else
                    {
                        switch (citizenManager.m_citizens.m_buffer[id.Citizen].CurrentLocation)
                        {
                            case Citizen.Location.Home:
                                id.Building = citizenManager.m_citizens.m_buffer[id.Citizen].m_homeBuilding;
                                break;
                            case Citizen.Location.Work:
                                id.Building = citizenManager.m_citizens.m_buffer[id.Citizen].m_workBuilding;
                                break;
                            case Citizen.Location.Visit:
                                id.Building = citizenManager.m_citizens.m_buffer[id.Citizen].m_visitBuilding;
                                break;
                            case Citizen.Location.Hotel:
                                id.Building = citizenManager.m_citizens.m_buffer[id.Citizen].m_hotelBuilding;
                                break;
                            case Citizen.Location.Moving:
                            {
                                ushort vehicle = citizenManager.m_citizens.m_buffer[id.Citizen].m_vehicle;
                                if (vehicle != 0)
                                {
                                    id.Vehicle = vehicle;
                                }
                                else if (num != 0)
                                {
                                    id.CitizenInstance = num;
                                }
                                break;
                            }
                        }
                    }
                }
                if (id.CitizenInstance != 0)
                {
                    var citizenManager2 = Singleton<CitizenManager>.instance;
                    if ((citizenManager2.m_instances.m_buffer[id.CitizenInstance].m_flags & CitizenInstance.Flags.InsideBuilding) != 0)
                    {
                        Vector3 pos = citizenManager2.m_instances.m_buffer[id.CitizenInstance].m_targetPos;
                        ushort num2 = Singleton<BuildingManager>.instance.FindBuilding(pos, 100f, ItemClass.Service.None, ItemClass.SubService.None, Building.Flags.Created, Building.Flags.Deleted);
                        if (num2 != 0)
                        {
                            id.Building = num2;
                        }
                    }
                }
                else if (id.Vehicle != 0)
                {
                    var vehicleManager = Singleton<VehicleManager>.instance;
                    if ((vehicleManager.m_vehicles.m_buffer[id.Vehicle].m_flags & Vehicle.Flags.InsideBuilding) != 0)
                    {
                        Vector3 pos2 = vehicleManager.m_vehicles.m_buffer[id.Vehicle].m_targetPos0;
                        ushort num3 = Singleton<BuildingManager>.instance.FindBuilding(pos2, 100f, ItemClass.Service.None, ItemClass.SubService.None, Building.Flags.Created, Building.Flags.Deleted);
                        if (num3 != 0)
                        {
                            id.Building = num3;
                        }
                    }
                }
                __result = id;
                return false;
            }


        }
    }
}
