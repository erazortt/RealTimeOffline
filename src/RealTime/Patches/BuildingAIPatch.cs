// <copyright file="BuildingAIPatch.cs" company="dymanoid">
// Copyright (c) dymanoid. All rights reserved.
// </copyright>

namespace RealTime.Patches
{
    using System;
    using System.Reflection;
    using ColossalFramework;
    using ColossalFramework.Math;
    using HarmonyLib;
    using RealTime.Config;
    using RealTime.Core;
    using RealTime.CustomAI;
    using RealTime.Simulation;
    using SkyTools.Configuration;
    using UnityEngine;

    /// <summary>
    /// A static class that provides the patch objects for the building AI game methods.
    /// </summary>
    ///
    [HarmonyPatch]
    internal static class BuildingAIPatch
    {
        /// <summary>Gets or sets the custom AI object for buildings.</summary>
        public static RealTimeBuildingAI RealTimeAI { get; set; }

        /// <summary>Gets or sets the weather information service.</summary>
        public static IWeatherInfo WeatherInfo { get; set; }

        [HarmonyPatch]
        private sealed class CommercialBuildingAI_SimulationStepActive
        {
            [HarmonyPatch(typeof(CommercialBuildingAI), "SimulationStepActive")]
            [HarmonyPrefix]
            private static bool Prefix(ref Building buildingData, ref byte __state)
            {
                __state = buildingData.m_outgoingProblemTimer;
                if (buildingData.m_customBuffer2 > 0)
                {
                    // Simulate some goods become spoiled; additionally, this will cause the buildings to never reach the 'stock full' state.
                    // In that state, no visits are possible anymore, so the building gets stuck
                    --buildingData.m_customBuffer2;
                }

                return true;
            }

            [HarmonyPatch(typeof(CommercialBuildingAI), "SimulationStepActive")]
            [HarmonyPostfix]
            private static void Postfix(ushort buildingID, ref Building buildingData, byte __state)
            {
                if (__state != buildingData.m_outgoingProblemTimer)
                {
                    RealTimeAI.ProcessBuildingProblems(buildingID, __state);
                }
            }
        }

        [HarmonyPatch]
        private sealed class MarketAI_SimulationStep
        {
            [HarmonyPatch(typeof(MarketAI), "SimulationStep")]
            [HarmonyPrefix]
            private static bool Prefix(ref Building buildingData, ref byte __state)
            {
                __state = buildingData.m_outgoingProblemTimer;
                if (buildingData.m_customBuffer2 > 0)
                {
                    // Simulate some goods become spoiled; additionally, this will cause the buildings to never reach the 'stock full' state.
                    // In that state, no visits are possible anymore, so the building gets stuck
                    --buildingData.m_customBuffer2;
                }

                return true;
            }

            [HarmonyPatch(typeof(MarketAI), "SimulationStep")]
            [HarmonyPostfix]
            private static void Postfix(ushort buildingID, ref Building buildingData, byte __state)
            {
                if (__state != buildingData.m_outgoingProblemTimer)
                {
                    RealTimeAI.ProcessBuildingProblems(buildingID, __state);
                }
            }
        }

        [HarmonyPatch]
        private sealed class PrivateBuildingAI_HandleWorkers
        {
            [HarmonyPatch(typeof(PrivateBuildingAI), "HandleWorkers")]
            [HarmonyPrefix]
            private static bool Prefix(ref Building buildingData, ref byte __state)
            {
                __state = buildingData.m_workerProblemTimer;
                return true;
            }

            [HarmonyPatch(typeof(PrivateBuildingAI), "HandleWorkers")]
            [HarmonyPostfix]
            private static void Postfix(ushort buildingID, ref Building buildingData, byte __state)
            {
                if (__state != buildingData.m_workerProblemTimer)
                {
                    RealTimeAI.ProcessWorkerProblems(buildingID, __state);
                }
            }
        }

        [HarmonyPatch]
        private sealed class PrivateBuildingAI_GetConstructionTime
        {
            [HarmonyPatch(typeof(PrivateBuildingAI), "GetConstructionTime")]
            [HarmonyPrefix]
            private static bool Prefix(ref int __result)
            {
                __result = RealTimeAI.GetConstructionTime();
                return false;
            }
        }

        [HarmonyPatch]
        private sealed class BuildingAI_CalculateUnspawnPosition
        {
            [HarmonyPatch(typeof(BuildingAI), "CalculateUnspawnPosition",
                new Type[] { typeof(ushort), typeof(Building), typeof(Randomizer), typeof(CitizenInfo), typeof(ushort), typeof(Vector3), typeof(Vector3), typeof(Vector2), typeof(CitizenInstance.Flags) },
                new ArgumentType[] {ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Out, ArgumentType.Out, ArgumentType.Out } )]
            [HarmonyPostfix]
            private static void Postfix(BuildingAI __instance, ushort buildingID, ref Building data, ref Randomizer randomizer, CitizenInfo info, ref Vector3 position, ref Vector3 target, ref CitizenInstance.Flags specialFlags)
            {
                if (!WeatherInfo.IsBadWeather || data.Info == null || data.Info.m_enterDoors == null)
                {
                    return;
                }

                var enterDoors = data.Info.m_enterDoors;
                bool doorFound = false;
                for (int i = 0; i < enterDoors.Length; ++i)
                {
                    var prop = enterDoors[i].m_finalProp;
                    if (prop == null)
                    {
                        continue;
                    }

                    if (prop.m_doorType == PropInfo.DoorType.Enter || prop.m_doorType == PropInfo.DoorType.Both)
                    {
                        doorFound = true;
                        break;
                    }
                }

                if (!doorFound)
                {
                    return;
                }

                __instance.CalculateSpawnPosition(buildingID, ref data, ref randomizer, info, out var spawnPosition, out var spawnTarget);

                position = spawnPosition;
                target = spawnTarget;
                specialFlags &= ~(CitizenInstance.Flags.HangAround | CitizenInstance.Flags.SittingDown);
            }
        }

        [HarmonyPatch]
        private sealed class PrivateBuildingAI_GetUpgradeInfo
        {
            [HarmonyPatch(typeof(PrivateBuildingAI), "GetUpgradeInfo")]
            [HarmonyPrefix]
            private static bool Prefix(ref BuildingInfo __result, ushort buildingID, ref Building data)
            {
                if(!RealTimeCore.ApplyBuildingPatch)
                {
                    return true;
                }

                if ((data.m_flags & Building.Flags.Upgrading) != 0)
                {
                    return true;
                }

                if (!RealTimeAI.CanBuildOrUpgrade(data.Info.GetService(), buildingID))
                {
                    __result = null;
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch]
        private sealed class BuildingManager_CreateBuilding
        {
            [HarmonyPatch(typeof(BuildingManager), "CreateBuilding")]
            [HarmonyPrefix]
            private static bool Prefix(BuildingInfo info, ref bool __result)
            {
                if(!RealTimeCore.ApplyBuildingPatch)
                {
                    return true;
                }

                if (!RealTimeAI.CanBuildOrUpgrade(info.GetService()))
                {
                    __result = false;
                    return false;
                }

                return true;
            }

            [HarmonyPatch(typeof(BuildingManager), "CreateBuilding")]
            [HarmonyPostfix]
            private static void Postfix(bool __result, ref ushort building, BuildingInfo info)
            {
                if(!RealTimeCore.ApplyBuildingPatch)
                {
                    return;
                }

                if (__result)
                {
                    RealTimeAI.RegisterConstructingBuilding(building, info.GetService());
                }
            }
        }

        [HarmonyPatch]
        private sealed class PlayerBuildingAI_ProduceGoods
        {
            [HarmonyPatch(typeof(PlayerBuildingAI), "ProduceGoods")]
            [HarmonyPostfix]
            private static void Postfix(ushort buildingID, ref Building buildingData)
            {
                if ((buildingData.m_flags & Building.Flags.Active) != 0
                    && RealTimeAI.ShouldSwitchBuildingLightsOff(buildingID))
                {
                    buildingData.m_flags &= ~Building.Flags.Active;
                }
            }
        }

        [HarmonyPatch]
        private sealed class CommonBuildingAI_GetColor
        {

            [HarmonyPatch(typeof(CommonBuildingAI), "GetColor")]
            [HarmonyPostfix]
            private static void Postfix(ushort buildingID, InfoManager.InfoMode infoMode, ref Color __result)
            {
                var negativeColor = InfoManager.instance.m_properties.m_modeProperties[(int)InfoManager.InfoMode.TrafficRoutes].m_negativeColor;
                var targetColor = InfoManager.instance.m_properties.m_modeProperties[(int)InfoManager.InfoMode.TrafficRoutes].m_targetColor;
                switch (infoMode)
                {
                    case InfoManager.InfoMode.TrafficRoutes:
                        __result = Color.Lerp(negativeColor, targetColor, RealTimeAI.GetBuildingReachingTroubleFactor(buildingID));
                        return;

                    case InfoManager.InfoMode.None:
                        if (RealTimeAI.ShouldSwitchBuildingLightsOff(buildingID))
                        {
                            __result.a = 0f;
                        }

                        return;
                }
            }
        }

        [HarmonyPatch]
        private sealed class CommonBuildingAI_BuildingDeactivated
        {
            private delegate void EmptyBuildingDelegate(CommonBuildingAI __instance, ushort buildingID, ref Building data, CitizenUnit.Flags flags, bool onlyMoving);
            private static readonly EmptyBuildingDelegate EmptyBuilding = AccessTools.MethodDelegate<EmptyBuildingDelegate>(typeof(CommonBuildingAI).GetMethod("EmptyBuilding", BindingFlags.Instance | BindingFlags.NonPublic), null, false);

            [HarmonyPatch(typeof(CommonBuildingAI), "BuildingDeactivated")]
            [HarmonyPrefix]
            private static bool Prefix(CommonBuildingAI __instance, ushort buildingID, ref Building data)
            {
                if (RealTimeAI.ShouldSwitchBuildingLightsOff(buildingID))
                {
                    TransferManager.TransferOffer offer = default;
	                offer.Building = buildingID;
	                Singleton<TransferManager>.instance.RemoveOutgoingOffer(TransferManager.TransferReason.Garbage, offer);
	                Singleton<TransferManager>.instance.RemoveOutgoingOffer(TransferManager.TransferReason.Crime, offer);
	                Singleton<TransferManager>.instance.RemoveOutgoingOffer(TransferManager.TransferReason.Sick, offer);
	                Singleton<TransferManager>.instance.RemoveOutgoingOffer(TransferManager.TransferReason.Sick2, offer);
	                Singleton<TransferManager>.instance.RemoveOutgoingOffer(TransferManager.TransferReason.Dead, offer);
	                Singleton<TransferManager>.instance.RemoveOutgoingOffer(TransferManager.TransferReason.Fire, offer);
	                Singleton<TransferManager>.instance.RemoveOutgoingOffer(TransferManager.TransferReason.Fire2, offer);
	                Singleton<TransferManager>.instance.RemoveOutgoingOffer(TransferManager.TransferReason.ForestFire, offer);
	                Singleton<TransferManager>.instance.RemoveOutgoingOffer(TransferManager.TransferReason.Collapsed, offer);
	                Singleton<TransferManager>.instance.RemoveOutgoingOffer(TransferManager.TransferReason.Collapsed2, offer);
	                Singleton<TransferManager>.instance.RemoveOutgoingOffer(TransferManager.TransferReason.Mail, offer);
	                Singleton<TransferManager>.instance.RemoveIncomingOffer(TransferManager.TransferReason.Worker0, offer);
	                Singleton<TransferManager>.instance.RemoveIncomingOffer(TransferManager.TransferReason.Worker1, offer);
	                Singleton<TransferManager>.instance.RemoveIncomingOffer(TransferManager.TransferReason.Worker2, offer);
	                Singleton<TransferManager>.instance.RemoveIncomingOffer(TransferManager.TransferReason.Worker3, offer);
	                EmptyBuilding(__instance, buildingID, ref data, CitizenUnit.Flags.Created, onlyMoving: false);
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch]
        private sealed class FishingHarborAI_TrySpawnBoat
        {
            [HarmonyPatch(typeof(FishingHarborAI), "TrySpawnBoat")]
            [HarmonyPrefix]
            private static bool Prefix(ref Building buildingData) => (buildingData.m_flags & Building.Flags.Active) != 0;
        }

        [HarmonyPatch]
        private sealed class BuildingAI_RenderMesh
        {
            [HarmonyPatch(typeof(BuildingAI), "RenderMeshes")]
            [HarmonyPrefix]
            public static void RenderMeshes(RenderManager.CameraInfo cameraInfo, ushort buildingID, ref Building data, int layerMask, ref RenderManager.Instance instance)
            {
                if (RealTimeAI.ShouldSwitchBuildingLightsOff(buildingID))
                {
                    instance.m_dataVector3.y = 44;
                }
                else
                {
                    instance.m_dataVector3.y = 0;
                }
            }

        }
    }
}
