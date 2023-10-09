// <copyright file="TouristAIPatch.cs" company="dymanoid">
// Copyright (c) dymanoid. All rights reserved.
// </copyright>



namespace RealTime.Patches
{
    using System;
    using HarmonyLib;
    using RealTime.CustomAI;
    using SkyTools.Tools;
    using RealTime.GameConnection;
    using static RealTime.GameConnection.HumanAIConnectionBase<TouristAI, Citizen>;
    using static RealTime.GameConnection.TouristAIConnection<TouristAI, Citizen>;
    using ColossalFramework;
    using ColossalFramework.Globalization;


    /// <summary>
    /// A static class that provides the patch objects and the game connection objects for the tourist AI .
    /// </summary>
    [HarmonyPatch]
    internal static class TouristAIPatch
    {
        /// <summary>Gets or sets the custom AI object for tourists.</summary>
        public static RealTimeTouristAI<TouristAI, Citizen> RealTimeAI { get; set; }

        /// <summary>Creates a game connection object for the tourist AI class.</summary>
        /// <returns>A new <see cref="TouristAIConnection{TouristAI, Citizen}"/> object.</returns>
        public static TouristAIConnection<TouristAI, Citizen> GetTouristAIConnection()
        {
            try
            {
                var getRandomTargetType = AccessTools.MethodDelegate<GetRandomTargetTypeDelegate>(AccessTools.Method(typeof(TouristAI), "GetRandomTargetType"));

                var getLeavingReason = AccessTools.MethodDelegate<GetLeavingReasonDelegate>(AccessTools.Method(typeof(HumanAI), "GetLeavingReason"));

                var addTouristVisit = AccessTools.MethodDelegate<AddTouristVisitDelegate>(AccessTools.Method(typeof(TouristAI), "AddTouristVisit", new Type[] { typeof(uint), typeof(ushort) })); // 

                var doRandomMove = AccessTools.MethodDelegate<DoRandomMoveDelegate>(AccessTools.Method(typeof(TouristAI), "DoRandomMove"));

                var findEvacuationPlace = AccessTools.MethodDelegate<FindEvacuationPlaceDelegate>(AccessTools.Method(typeof(HumanAI), "FindEvacuationPlace"));

                var findVisitPlace = AccessTools.MethodDelegate<FindVisitPlaceDelegate>(AccessTools.Method(typeof(HumanAI), "FindVisitPlace"));

                var getEntertainmentReason = AccessTools.MethodDelegate<GetEntertainmentReasonDelegate>(AccessTools.Method(typeof(TouristAI), "GetEntertainmentReason"));

                var getEvacuationReason = AccessTools.MethodDelegate<GetEvacuationReasonDelegate>(AccessTools.Method(typeof(TouristAI), "GetEvacuationReason"));

                var getShoppingReason = AccessTools.MethodDelegate<GetShoppingReasonDelegate>(AccessTools.Method(typeof(TouristAI), "GetShoppingReason"));

                var startMoving = AccessTools.MethodDelegate<StartMovingDelegate>(AccessTools.Method(typeof(HumanAI), "StartMoving", new Type[] { typeof(uint), typeof(Citizen).MakeByRefType(), typeof(ushort), typeof(ushort) }));

                return new TouristAIConnection<TouristAI, Citizen>(
                    getRandomTargetType,
                    getLeavingReason,
                    addTouristVisit,
                    doRandomMove,
                    findEvacuationPlace,
                    findVisitPlace,
                    getEntertainmentReason,
                    getEvacuationReason,
                    getShoppingReason,
                    startMoving);
            }
            catch (Exception e)
            {
                Log.Error("The 'Real Time' mod failed to create a delegate for type 'TouristAI', no method patching for the class: " + e);
                return null;
            }
        }

        [HarmonyPatch]
        private sealed class TouristAI_UpdateLocation
        {
            [HarmonyPatch(typeof(TouristAI), "UpdateLocation")]
            [HarmonyPrefix]
            private static bool Prefix(TouristAI __instance, uint citizenID, ref Citizen data)
            {
                RealTimeAI.UpdateLocation(__instance, citizenID, ref data);
                return false;
            }
        }

        [HarmonyPatch]
        private sealed class TouristAI_GetLocalizedStatus
        {
            [HarmonyPatch(typeof(TouristAI), "GetLocalizedStatus",
            new Type[] { typeof(uint), typeof(Citizen), typeof(InstanceID) },
            new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Out })]
            [HarmonyPostfix]
            private static void Postfix1(TouristAI __instance, uint citizenID, ref Citizen data, out InstanceID target, ref string __result)
            {
                target = InstanceID.Empty;
                var currentLocation = data.CurrentLocation;
                if (currentLocation == Citizen.Location.Visit)
                {
                    ushort hotelBuilding = data.m_hotelBuilding;
                    if (hotelBuilding != 0)
                    {
                        target.Building = hotelBuilding;
                        var info = Singleton<BuildingManager>.instance.m_buildings.m_buffer[hotelBuilding].Info;
                        if (info != null && info.GetAI() is CommercialBuildingAI && info.m_class.m_service == ItemClass.Service.Commercial && info.m_class.m_subService == ItemClass.SubService.CommercialTourist && info.name.Contains("Hotel"))
                        {
                            __result = Locale.Get("CITIZEN_STATUS_HOTEL");
                        }
                    }

                }
            }

            [HarmonyPatch(typeof(TouristAI), "GetLocalizedStatus",
            new Type[] { typeof(ushort), typeof(CitizenInstance), typeof(InstanceID) },
            new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Out })]
            [HarmonyPostfix]
            private static void Postfix2(TouristAI __instance, ushort instanceID, ref CitizenInstance data, out InstanceID target, ref string __result)
            {
                var instance = Singleton<CitizenManager>.instance;
                uint citizen = data.m_citizen;
                ushort targetBuilding = data.m_targetBuilding;
                target = InstanceID.Empty;
                ushort hotelBuilding = 0;
                ushort vehicle = 0;
                if (citizen != 0)
                {
                    hotelBuilding = instance.m_citizens.m_buffer[citizen].m_hotelBuilding;
                    vehicle = instance.m_citizens.m_buffer[citizen].m_vehicle;
                }
                if (targetBuilding != 0)
                {
                    bool flag3 = data.m_path == 0 && (data.m_flags & CitizenInstance.Flags.HangAround) != 0;
                    if (vehicle != 0)
                    {
                        var instance3 = Singleton<VehicleManager>.instance;
                        var info2 = instance3.m_vehicles.m_buffer[vehicle].Info;
                        if (info2.m_class.m_service == ItemClass.Service.Residential && info2.m_vehicleType != VehicleInfo.VehicleType.Bicycle)
                        {
                            if (info2.m_vehicleAI.GetOwnerID(vehicle, ref instance3.m_vehicles.m_buffer[vehicle]).Citizen == citizen)
                            {
                                if (targetBuilding == hotelBuilding)
                                {
                                    target = InstanceID.Empty;
                                    var info = Singleton<BuildingManager>.instance.m_buildings.m_buffer[targetBuilding].Info;
                                    if (info != null && info.GetAI() is CommercialBuildingAI && info.m_class.m_service == ItemClass.Service.Commercial && info.m_class.m_subService == ItemClass.SubService.CommercialTourist && info.name.Contains("Hotel"))
                                    {
                                        __result = Locale.Get("CITIZEN_STATUS_DRIVINGTO_HOTEL");
                                    }
                                }


                            }
                        }
                        else if (info2.m_class.m_service == ItemClass.Service.PublicTransport || info2.m_class.m_service == ItemClass.Service.Disaster)
                        {
                            if (targetBuilding == hotelBuilding)
                            {
                                target = InstanceID.Empty;
                                var info = Singleton<BuildingManager>.instance.m_buildings.m_buffer[targetBuilding].Info;
                                if (info != null && info.GetAI() is CommercialBuildingAI && info.m_class.m_service == ItemClass.Service.Commercial && info.m_class.m_subService == ItemClass.SubService.CommercialTourist && info.name.Contains("Hotel"))
                                {
                                    __result = Locale.Get("CITIZEN_STATUS_TRAVELLINGTO_HOTEL");
                                }
                            }
                        }
                    }
                    else
                    {
                        if (targetBuilding == hotelBuilding)
                        {
                            target = InstanceID.Empty;
                            var info = Singleton<BuildingManager>.instance.m_buildings.m_buffer[targetBuilding].Info;
                            if (info != null && info.GetAI() is CommercialBuildingAI && info.m_class.m_service == ItemClass.Service.Commercial && info.m_class.m_subService == ItemClass.SubService.CommercialTourist && info.name.Contains("Hotel"))
                            {
                                __result = Locale.Get((!flag3) ? "CITIZEN_STATUS_GOINGTO_HOTEL" : "CITIZEN_STATUS_AT_HOTEL");
                            }
                        }
                    }
                }
            }
        }
    }
}
