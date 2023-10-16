
namespace RealTime.Serializer
{
    using System;
    using System.Collections.Generic;
    using RealTime.CustomAI;
    using UnityEngine;
    using static RealTime.CustomAI.FireBurnStartTimeManager;

    public class FireBurnStartTimeSerializer
    {
        // Some magic values to check we are line up correctly on the tuple boundaries
        private const uint uiTUPLE_START = 0xFEFEFEFE;
        private const uint uiTUPLE_END = 0xFAFAFAFA;

        private const ushort iFIRE_BURN_START_TIME_DATA_VERSION = 1;

        public static void SaveData(FastList<byte> Data)
        {
            // Write out metadata
            StorageData.WriteUInt16(iFIRE_BURN_START_TIME_DATA_VERSION, Data);
            StorageData.WriteInt32(FireBurnStartTimeManager.FireBurnStartTime.Count, Data);

            // Write out each buffer settings
            foreach (var kvp in FireBurnStartTimeManager.FireBurnStartTime)
            {
                // Write start tuple
                StorageData.WriteUInt32(uiTUPLE_START, Data);

                // Write actual settings
                StorageData.WriteUInt16(kvp.Key, Data);
                StorageData.WriteDateTime(kvp.Value.StartDate, Data);
                StorageData.WriteFloat(kvp.Value.StartTime, Data);
                StorageData.WriteFloat(kvp.Value.Duration, Data);

                // Write end tuple
                StorageData.WriteUInt32(uiTUPLE_END, Data);
            }
        }

        public static void LoadData(int iGlobalVersion, byte[] Data, ref int iIndex)
        {
            if (Data != null && Data.Length > iIndex)
            {
                int iFireBurnStartTimeVersion = StorageData.ReadUInt16(Data, ref iIndex);
                Debug.Log("Global: " + iGlobalVersion + " BufferVersion: " + iFireBurnStartTimeVersion + " DataLength: " + Data.Length + " Index: " + iIndex);
                if (FireBurnStartTime == null)
                {
                    FireBurnStartTime = new Dictionary<ushort, BurnTime>();
                }
                int FireBurnStartTime_Count = StorageData.ReadInt32(Data, ref iIndex);
                for (int i = 0; i < FireBurnStartTime_Count; i++)
                {
                    CheckStartTuple($"Buffer({i})", FireBurnStartTime_Count, Data, ref iIndex);

                    ushort BuildingId = StorageData.ReadUInt16(Data, ref iIndex);

                    var StartDate = StorageData.ReadDateTime(Data, ref iIndex);
                    float StartTime = StorageData.ReadFloat(Data, ref iIndex);
                    float Duration = StorageData.ReadFloat(Data, ref iIndex);

                    var burnTime = new BurnTime()
                    {
                        StartDate = StartDate,
                        StartTime = StartTime,
                        Duration = Duration
                    };

                    FireBurnStartTime.Add(BuildingId, burnTime);
                    CheckEndTuple($"Buffer({i})", iFireBurnStartTimeVersion, Data, ref iIndex);
                }
            }
        }

        private static void CheckStartTuple(string sTupleLocation, int iDataVersion, byte[] Data, ref int iIndex)
        {
            if (iDataVersion >= 1)
            {
                uint iTupleStart = StorageData.ReadUInt32(Data, ref iIndex);
                if (iTupleStart != uiTUPLE_START)
                {
                    throw new Exception($"Buffer start tuple not found at: {sTupleLocation}");
                }
            }
        }

        private static void CheckEndTuple(string sTupleLocation, int iDataVersion, byte[] Data, ref int iIndex)
        {
            if (iDataVersion >= 1)
            {
                uint iTupleEnd = StorageData.ReadUInt32(Data, ref iIndex);
                if (iTupleEnd != uiTUPLE_END)
                {
                    throw new Exception($"Buffer end tuple not found at: {sTupleLocation}");
                }
            }
        }

    }
}
