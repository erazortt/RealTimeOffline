// <copyright file="ResidentAIPatch.cs" company="dymanoid">Copyright (c) dymanoid. All rights reserved.</copyright>

namespace RealTime.Patches
{
    using System;
    using HarmonyLib;
    using RealTime.CustomAI;
    using SkyTools.Tools;
    using RealTime.GameConnection;
    using static RealTime.GameConnection.HumanAIConnectionBase<ResidentAI, Citizen>;
    using static RealTime.GameConnection.ResidentAIConnection<ResidentAI, Citizen>;
    using RealTime.Core;
    using ColossalFramework;
    using System.Collections.Generic;

    /// <summary>
    /// A static class that provides the patch objects and the game connection objects for the resident AI .
    /// </summary>
    [HarmonyPatch]
    internal static class ResidentAIPatch
    {
        /// <summary>Gets or sets the custom AI object for resident citizens.</summary>
        public static RealTimeResidentAI<ResidentAI, Citizen> RealTimeAI { get; set; }

        /// <summary>Creates a game connection object for the resident AI class.</summary>
        /// <returns>A new <see cref="ResidentAIConnection{ResidentAI, Citizen}"/> object.</returns>
        public static ResidentAIConnection<ResidentAI, Citizen> GetResidentAIConnection()
        {
            try
            {
                var doRandomMove = AccessTools.MethodDelegate<DoRandomMoveDelegate>(AccessTools.Method(typeof(ResidentAI), "DoRandomMove"));

                var findEvacuationPlace = AccessTools.MethodDelegate<FindEvacuationPlaceDelegate>(AccessTools.Method(typeof(HumanAI), "FindEvacuationPlace"));

                var findHospital = AccessTools.MethodDelegate<FindHospitalDelegate>(AccessTools.Method(typeof(ResidentAI), "FindHospital"));

                var findVisitPlace = AccessTools.MethodDelegate<FindVisitPlaceDelegate>(AccessTools.Method(typeof(HumanAI), "FindVisitPlace"));

                var getEntertainmentReason = AccessTools.MethodDelegate<GetEntertainmentReasonDelegate>(AccessTools.Method(typeof(ResidentAI), "GetEntertainmentReason"));

                var getEvacuationReason = AccessTools.MethodDelegate<GetEvacuationReasonDelegate>(AccessTools.Method(typeof(ResidentAI), "GetEvacuationReason"));

                var getShoppingReason = AccessTools.MethodDelegate<GetShoppingReasonDelegate>(AccessTools.Method(typeof(ResidentAI), "GetShoppingReason"));

                var startMoving = AccessTools.MethodDelegate<StartMovingDelegate>(AccessTools.Method(typeof(HumanAI), "StartMoving", new Type[] { typeof(uint), typeof(Citizen).MakeByRefType(), typeof(ushort), typeof(ushort)}));

                var startMovingWithOffer = AccessTools.MethodDelegate<StartMovingWithOfferDelegate>(AccessTools.Method(typeof(HumanAI), "StartMoving", new Type[] { typeof(uint), typeof(Citizen).MakeByRefType(), typeof(ushort), typeof(TransferManager.TransferOffer)}));

                var attemptAutodidact = AccessTools.MethodDelegate<AttemptAutodidactDelegate>(AccessTools.Method(typeof(ResidentAI), "AttemptAutodidact"));

                return new ResidentAIConnection<ResidentAI, Citizen>(
                    doRandomMove,
                    findEvacuationPlace,
                    findHospital,
                    findVisitPlace,
                    getEntertainmentReason,
                    getEvacuationReason,
                    getShoppingReason,
                    startMoving,
                    startMovingWithOffer,
                    attemptAutodidact);
            }
            catch (Exception e)
            {
                Log.Error("The 'Real Time' mod failed to create a delegate for type 'ResidentAI', no method patching for the class: " + e);
                return null;
            }
        }

        [HarmonyPatch]
        private sealed class ResidentAI_UpdateLocation
        {
            [HarmonyPatch(typeof(ResidentAI), "UpdateLocation")]
            [HarmonyPrefix]
            private static bool Prefix(ResidentAI __instance, uint citizenID, ref Citizen data)
            {
                if (RealTimeAI != null)
                {
                    RealTimeAI.UpdateLocation(__instance, citizenID, ref data);
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch]
        private sealed class HumanAI_ArriveAtTarget
        {
            [HarmonyPatch(typeof(HumanAI), "ArriveAtTarget")]
            [HarmonyPostfix]
            private static void Postfix(ushort instanceID, ref CitizenInstance citizenData, bool __result)
            {
                if (__result && citizenData.m_citizen != 0)
                {
                    RealTimeAI.RegisterCitizenArrival(citizenData.m_citizen);
                }
            }
        }

        [HarmonyPatch]
        private sealed class ResidentAI_UpdateAge
        {
            [HarmonyPatch(typeof(ResidentAI), "UpdateAge")]
            [HarmonyPrefix]
            private static bool Prefix(ref bool __result)
            {
                if(!RealTimeCore.ApplyCitizenPatch)
                {
                    return true;
                }

                if (RealTimeAI.CanCitizensGrowUp)
                {
                    return true;
                }

                __result = false;
                return false;
            }
        }

        [HarmonyPatch]
        private sealed class ResidentAI_CanMakeBabies
        {
            [HarmonyPatch(typeof(ResidentAI), "CanMakeBabies")]
            [HarmonyPrefix]
            private static bool Prefix(uint citizenID, ref Citizen data, ref bool __result)
            {
                if(!RealTimeCore.ApplyCitizenPatch)
                {
                    return true;
                }

                __result = RealTimeAI.CanMakeBabies(citizenID, ref data);
                return false;
            }
        }

        [HarmonyPatch]
        private sealed class HumanAI_StartMoving
        {
            [HarmonyPatch(typeof(HumanAI), "StartMoving",
                new Type[] { typeof(uint), typeof(Citizen), typeof(ushort), typeof(ushort) },
                new ArgumentType[] {ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal})]
            [HarmonyPostfix]
            private static void Postfix(uint citizenID, bool __result)
            {
                if (__result && citizenID != 0)
                {
                    RealTimeAI.RegisterCitizenDeparture(citizenID);
                }
            }
        }

        [HarmonyPatch]
        private sealed class ResidentAI_SimulationStep
        {
            [HarmonyPatch(typeof(ResidentAI), "SimulationStep",
                new Type[] { typeof(ushort), typeof(CitizenInstance), typeof(CitizenInstance.Frame), typeof(bool) },
                new ArgumentType[] {ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Normal})]
            [HarmonyPostfix]
            private static void Postfix(ResidentAI __instance, ushort instanceID, ref CitizenInstance citizenData)
            {
                if (instanceID == 0)
                {
                    return;
                }

                if ((citizenData.m_flags & (CitizenInstance.Flags.WaitingTaxi | CitizenInstance.Flags.WaitingTransport)) != 0)
                {
                    RealTimeAI.ProcessWaitingForTransport(__instance, citizenData.m_citizen, instanceID);
                }
            }

            [HarmonyPatch(typeof(ResidentAI), "SimulationStep",
                new Type[] { typeof(ushort), typeof(CitizenInstance), typeof(CitizenInstance.Frame), typeof(bool) },
                new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Normal })]
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> TranspileSimulationStep1(IEnumerable<CodeInstruction> instructions)
            {
                var inst = new List<CodeInstruction>(instructions);

                for (int i = 0; i < inst.Count; i++)
                {
                    if (inst[i].LoadsConstant(-100))
                    {
                        inst[i].operand = -1;
                    }
                }
                return inst;
            }

            [HarmonyPatch(typeof(ResidentAI), "SimulationStep",
                new Type[] { typeof(uint), typeof(CitizenUnit) },
                new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref })]
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> TranspileSimulationStep2(IEnumerable<CodeInstruction> instructions)
            {
                var inst = new List<CodeInstruction>(instructions);

                for (int i = 0; i < inst.Count; i++)
                {
                    if (inst[i].LoadsConstant(20))
                    {
                        inst[i].operand = 1;
                    }
                }
                return inst;
            }

        }


        [HarmonyPatch]
        private sealed class ResidentAI_StartTransfer
        {
            [HarmonyPatch(typeof(ResidentAI), "StartTransfer",
                new Type[] { typeof(uint), typeof(Citizen), typeof(TransferManager.TransferReason), typeof(TransferManager.TransferOffer) },
                new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal })]
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> TranspileStartTransfer(IEnumerable<CodeInstruction> instructions)
            {
                var inst = new List<CodeInstruction>(instructions);

                for (int i = 0; i < inst.Count; i++)
                {
                    if (inst[i].LoadsConstant(100))
                    {
                        inst[i].operand = 1;
                    }
                }
                return inst;
            }

            [HarmonyPatch(typeof(ResidentAI), "StartTransfer")]
            [HarmonyPrefix]
            private static bool Prefix(ResidentAI __instance, uint citizenID, ref Citizen data, TransferManager.TransferReason reason, TransferManager.TransferOffer offer)
            {
                if (data.m_flags == Citizen.Flags.None || data.Dead && reason != TransferManager.TransferReason.Dead)
                {
                    return true;
                }
                switch (reason)
                {
                    case TransferManager.TransferReason.Shopping:
                    case TransferManager.TransferReason.ShoppingB:
                    case TransferManager.TransferReason.ShoppingC:
                    case TransferManager.TransferReason.ShoppingD:
                    case TransferManager.TransferReason.ShoppingE:
                    case TransferManager.TransferReason.ShoppingF:
                    case TransferManager.TransferReason.ShoppingG:
                    case TransferManager.TransferReason.ShoppingH:
                        if (data.m_homeBuilding != 0 && !data.Sick)
                        {
                            var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[offer.Building];
                            // dont shop in hotel buildings
                            if (building.Info.m_buildingAI is HotelAI)
                            {
                                return false;
                            }
                            if(building.Info.m_class.m_service == ItemClass.Service.Commercial && (building.Info.m_class.m_subService == ItemClass.SubService.CommercialTourist || building.Info.m_class.m_subService == ItemClass.SubService.CommercialLeisure))
                            {
                                return false;
                            }
                        }
                        return true;
                    case TransferManager.TransferReason.Entertainment:
                    case TransferManager.TransferReason.EntertainmentB:
                    case TransferManager.TransferReason.EntertainmentC:
                    case TransferManager.TransferReason.EntertainmentD:
                        if (data.m_homeBuilding != 0 && !data.Sick)
                        {
                            var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[offer.Building];
                            // dont go to entertainment in hotel buildings with no events
                            if (building.Info.m_buildingAI is HotelAI && building.m_eventIndex == 0)
                            {
                                return false;
                            }
                            // dont go to entertainment in after the dark hotel buildings
                            if (building.Info.m_class.m_service == ItemClass.Service.Commercial && building.Info.m_class.m_subService == ItemClass.SubService.CommercialTourist && (building.Info.name.Contains("hotel") || building.Info.name.Contains("Hotel")))
                            {
                                return false;
                            }
                        }
                        return true;
                    default:
                        return true;
                }
            }
        }
    }
}
