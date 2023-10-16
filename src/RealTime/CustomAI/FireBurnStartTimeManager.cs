namespace RealTime.CustomAI
{
    using System;
    using System.Collections.Generic;
    using RealTime.Simulation;

    public static class FireBurnStartTimeManager
    {
        public static Dictionary<ushort, BurnTime> FireBurnStartTime;

        public struct BurnTime
        {
            public DateTime StartDate;
            public float StartTime;
            public float Duration;
        }

        public static void Init()
        {
            if (FireBurnStartTime == null)
            {
                FireBurnStartTime = new Dictionary<ushort, BurnTime>();
            }
        }

        public static void Deinit() => FireBurnStartTime = new Dictionary<ushort, BurnTime>();

        internal static BurnTime GetBuildingFireStartTime(ushort buildingID, ITimeInfo timeInfo)
        {
            if (!FireBurnStartTime.TryGetValue(buildingID, out var burnTime))
            {
                float burnDuration = 0.5f; // UnityEngine.Random.Range(0.5f, 4f);
                burnTime = new BurnTime()
                {
                    StartDate = timeInfo.Now.Date,
                    StartTime = timeInfo.CurrentHour,
                    Duration = burnDuration
                };
                FireBurnStartTime.Add(buildingID, burnTime);
            }
            return burnTime;
        }

        public static void SetBuildingFireStartTime(ushort buildingID, BurnTime burnTime) => FireBurnStartTime[buildingID] = burnTime;
    }

}
