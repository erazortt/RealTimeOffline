namespace RealTime.CustomAI
{
    using System.Collections.Generic;
    using RealTime.Core;

    internal static class BuildingWorkTimeManager
    {
        public static Dictionary<ushort, WorkTime> BuildingsWorkTime;

        public struct WorkTime
        {
            public bool WorkAtNight;
            public bool WorkAtWeekands;
            public bool HasExtendedWorkShift;
            public bool HasContinuousWorkShift;
            public int WorkShifts;
        }

        public static void Init()
        {
            if (BuildingsWorkTime == null)
            {
                BuildingsWorkTime = new Dictionary<ushort, WorkTime>();
            }
        }

        public static void Deinit() => BuildingsWorkTime = new Dictionary<ushort, WorkTime>();

        internal static WorkTime GetBuildingWorkTime(ushort buildingID) => !BuildingsWorkTime.TryGetValue(buildingID, out var workTime) ? default : workTime;

        internal static void CreateBuildingWorkTime(ushort buildingID, BuildingInfo buildingInfo)
        {
            float height = BuildingManager.instance.m_buildings.m_buffer[buildingID].Info.m_size.y;
            if (!BuildingsWorkTime.TryGetValue(buildingID, out _))
            {
                bool OpenAtNight = ShouldOccur(RealTimeMod.configProvider.Configuration.OpenCommercialAtNightQuota);
                if (height > RealTimeMod.configProvider.Configuration.SwitchOffLightsMaxHeight || buildingInfo.m_class.m_subService == ItemClass.SubService.CommercialLeisure || buildingInfo.m_class.m_subService == ItemClass.SubService.CommercialTourist)
                {
                    OpenAtNight = true;
                }
                bool OpenAtWeekends = ShouldOccur(RealTimeMod.configProvider.Configuration.OpenCommercialAtWeekendsQuota);
                bool HasExtendedWorkShift = ShouldOccur(50);
                bool HasContinuousWorkShift = ShouldOccur(50);

                if (HasExtendedWorkShift)
                {
                    HasContinuousWorkShift = false;
                }

                int WorkShifts = 2;

                if (HasContinuousWorkShift && !OpenAtNight)
                {
                    WorkShifts = 1;
                }

                if (OpenAtNight)
                {
                    WorkShifts = HasContinuousWorkShift ? 2 : 3;
                }

                var workTime = new WorkTime()
                {
                    WorkAtNight = OpenAtNight,
                    WorkAtWeekands = OpenAtWeekends,
                    HasExtendedWorkShift = HasExtendedWorkShift,
                    HasContinuousWorkShift = HasContinuousWorkShift,
                    WorkShifts = WorkShifts
                };
                BuildingsWorkTime.Add(buildingID, workTime);
            }
        }

        public static void SetBuildingWorkTime(ushort buildingID, WorkTime workTime) => BuildingsWorkTime[buildingID] = workTime;


        public static void RemoveBuildingWorkTime(ushort buildingID) => BuildingsWorkTime.Remove(buildingID);


        private static bool ShouldOccur(uint probability) => SimulationManager.instance.m_randomizer.Int32(100u) < probability;
    }

}
