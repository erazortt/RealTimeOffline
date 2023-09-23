namespace RealTime.Patches
{
    using HarmonyLib;
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


    }
}
