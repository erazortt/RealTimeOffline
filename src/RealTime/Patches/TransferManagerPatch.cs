// <copyright file="TransferManagerPatch.cs" company="dymanoid">
// Copyright (c) dymanoid. All rights reserved.
// </copyright>

namespace RealTime.Patches
{
    using HarmonyLib;
    using RealTime.CustomAI;

    /// <summary>
    /// A static class that provides the patch objects for the game's transfer manager.
    /// </summary>
    [HarmonyPatch]
    internal static class TransferManagerPatch
    {
        /// <summary>Gets or sets the custom AI object for buildings.</summary>
        public static RealTimeBuildingAI RealTimeAI { get; set; }

        [HarmonyPatch]
        private sealed class TransferManager_AddOutgoingOffer
        {
            [HarmonyPatch(typeof(TransferManager), "AddOutgoingOffer")]
            [HarmonyPrefix]
            private static bool Prefix(TransferManager.TransferReason material, ref TransferManager.TransferOffer offer)
            {
                switch (material)
                {
                    case TransferManager.TransferReason.Entertainment:
                    case TransferManager.TransferReason.EntertainmentB:
                    case TransferManager.TransferReason.EntertainmentC:
                    case TransferManager.TransferReason.EntertainmentD:
                    case TransferManager.TransferReason.TouristA:
                    case TransferManager.TransferReason.TouristB:
                    case TransferManager.TransferReason.TouristC:
                    case TransferManager.TransferReason.TouristD:
                        return RealTimeAI.IsEntertainmentTarget(offer.Building);

                    case TransferManager.TransferReason.Shopping:
                    case TransferManager.TransferReason.ShoppingB:
                    case TransferManager.TransferReason.ShoppingC:
                    case TransferManager.TransferReason.ShoppingD:
                    case TransferManager.TransferReason.ShoppingE:
                    case TransferManager.TransferReason.ShoppingF:
                    case TransferManager.TransferReason.ShoppingG:
                    case TransferManager.TransferReason.ShoppingH:
                        return RealTimeAI.IsShoppingTarget(offer.Building);

                    case TransferManager.TransferReason.ParkMaintenance:
                        return RealTimeAI.IsParkMaintenanceHours(offer.Building);

                    case TransferManager.TransferReason.Mail:
                    case TransferManager.TransferReason.SortedMail:
                        return RealTimeAI.IsMailHours(offer.Building);

                    case TransferManager.TransferReason.Garbage:
                        return RealTimeAI.IsGarbageHours(offer.Building);

                    default:
                        return true;
                }
            }
        }

        [HarmonyPatch]
        private sealed class TransferManager_AddIncomingOffer
        {
            [HarmonyPatch(typeof(TransferManager), "AddIncomingOffer")]
            [HarmonyPrefix]
            private static bool Prefix(TransferManager.TransferReason material, ref TransferManager.TransferOffer offer)
            {
                switch (material)
                {
                    case TransferManager.TransferReason.UnsortedMail:
                        return RealTimeAI.IsMailHours(offer.Building);

                    case TransferManager.TransferReason.RoadMaintenance:
                    case TransferManager.TransferReason.Snow:
                        return RealTimeAI.IsMaintenanceSnowRoadServiceHours(offer.NetSegment);

                    case TransferManager.TransferReason.ParkMaintenance:
                        return RealTimeAI.IsParkMaintenanceHours(offer.Building);

                    case TransferManager.TransferReason.Garbage:
                        return RealTimeAI.IsGarbageHours(offer.Building);

                    default:
                        return true;
                }
            }
        }
    }
}
