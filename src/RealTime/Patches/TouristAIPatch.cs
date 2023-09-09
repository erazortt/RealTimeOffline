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
    }
}
