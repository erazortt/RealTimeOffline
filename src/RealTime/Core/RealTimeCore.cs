// <copyright file="RealTimeCore.cs" company="dymanoid">
// Copyright (c) dymanoid. All rights reserved.
// </copyright>

namespace RealTime.Core
{
    using System;
    using System.Collections.Generic;
    using RealTime.Config;
    using RealTime.CustomAI;
    using RealTime.Events;
    using RealTime.Events.Storage;
    using RealTime.GameConnection;
    using RealTime.Patches;
    using RealTime.Simulation;
    using RealTime.UI;
    using SkyTools.Configuration;
    using SkyTools.GameTools;
    using SkyTools.Localization;
    using SkyTools.Storage;
    using SkyTools.Tools;

    /// <summary>
    /// The core component of the Real Time mod. Activates and deactivates
    /// the different parts of the mod's logic.
    /// </summary>
    internal sealed class RealTimeCore
    {
        private const string HarmonyId = "com.cities_skylines.dymanoid.realtime";

        private readonly List<IStorageData> storageData = new List<IStorageData>();
        private readonly TimeAdjustment timeAdjustment;
        private readonly CustomTimeBar timeBar;
        private readonly RealTimeEventManager eventManager;
        private readonly VanillaEvents vanillaEvents;

        private bool isEnabled;

        public static bool ApplyCitizenPatch = false;
        public static bool ApplyBuildingPatch = false;

        private RealTimeCore(
            TimeAdjustment timeAdjustment,
            CustomTimeBar timeBar,
            RealTimeEventManager eventManager,
            VanillaEvents vanillaEvents)
        {
            this.timeAdjustment = timeAdjustment;
            this.timeBar = timeBar;
            this.eventManager = eventManager;
            this.vanillaEvents = vanillaEvents;
            isEnabled = true;
        }

        /// <summary>Gets a value indicating whether the mod is running in a restricted mode due to method patch failures.</summary>
        public bool IsRestrictedMode { get; private set; }

        /// <summary>
        /// Runs the mod by activating its parts.
        /// </summary>
        ///
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="configProvider"/> or <paramref name="localizationProvider"/>
        /// or <paramref name="compatibility"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="rootPath"/> is null or an empty string.</exception>
        ///
        /// <param name="configProvider">The configuration provider that provides the mod's configuration.</param>
        /// <param name="rootPath">The path to the mod's assembly. Additional files are stored here too.</param>
        /// <param name="localizationProvider">The <see cref="ILocalizationProvider"/> to use for text translation.</param>
        /// <param name="setDefaultTime"><c>true</c> to initialize the game time to a default value (real world date and city wake up hour);
        /// <c>false</c> to leave the game time unchanged.</param>
        ///
        /// <returns>A <see cref="RealTimeCore"/> instance that can be used to stop the mod.</returns>
        public static RealTimeCore Run(
            ConfigurationProvider<RealTimeConfig> configProvider,
            string rootPath,
            ILocalizationProvider localizationProvider,
            bool setDefaultTime,
            Compatibility compatibility)
        {
            if (configProvider == null)
            {
                throw new ArgumentNullException(nameof(configProvider));
            }

            if (string.IsNullOrEmpty(rootPath))
            {
                throw new ArgumentException("The root path cannot be null or empty string", nameof(rootPath));
            }

            if (localizationProvider == null)
            {
                throw new ArgumentNullException(nameof(localizationProvider));
            }

            if (compatibility == null)
            {
                throw new ArgumentNullException(nameof(compatibility));
            }

            CheckMethodPatches(compatibility);

            if (StorageBase.CurrentLevelStorage != null)
            {
                LoadStorageData(new[] { configProvider }, StorageBase.CurrentLevelStorage);
            }

            localizationProvider.SetEnglishUSFormatsState(configProvider.Configuration.UseEnglishUSFormats);

            var timeInfo = new TimeInfo(configProvider.Configuration);
            var buildingManager = new BuildingManagerConnection();
            var randomizer = new GameRandomizer();

            var weatherInfo = new WeatherInfo(new WeatherManagerConnection(), randomizer);

            var gameConnections = new GameConnections<Citizen>(
                timeInfo,
                new CitizenConnection(),
                new CitizenManagerConnection(),
                buildingManager,
                randomizer,
                new TransferManagerConnection(),
                weatherInfo);

            var eventManager = new RealTimeEventManager(
                configProvider.Configuration,
                CityEventsLoader.Instance,
                new EventManagerConnection(),
                buildingManager,
                randomizer,
                timeInfo,
                Constants.MaxTravelTime);

            if (!SetupCustomAI(timeInfo, configProvider.Configuration, gameConnections, eventManager, compatibility))
            {
                Log.Error("The 'Real Time' mod failed to setup the customized AI and will now be deactivated.");
                return null;
            }

            var timeAdjustment = new TimeAdjustment(configProvider.Configuration);
            var gameDate = timeAdjustment.Enable(setDefaultTime);
            SimulationHandler.CitizenProcessor.UpdateFrameDuration();

            CityEventsLoader.Instance.ReloadEvents(rootPath);

            var customTimeBar = new CustomTimeBar();
            customTimeBar.Enable(gameDate);
            customTimeBar.CityEventClick += CustomTimeBarCityEventClick;

            var vanillaEvents = VanillaEvents.Customize();

            var result = new RealTimeCore(timeAdjustment, customTimeBar, eventManager, vanillaEvents);
            eventManager.EventsChanged += result.CityEventsChanged;

            var statistics = new Statistics(timeInfo, localizationProvider);
            if (statistics.Initialize())
            {
                statistics.RefreshUnits();
            }
            else
            {
                statistics = null;
            }

            SimulationHandler.NewDay += result.CityEventsChanged;

            SimulationHandler.TimeAdjustment = timeAdjustment;
            SimulationHandler.DayTimeSimulation = new DayTimeSimulation(configProvider.Configuration);
            SimulationHandler.EventManager = eventManager;
            SimulationHandler.WeatherInfo = weatherInfo;
            SimulationHandler.Buildings = BuildingAIPatch.RealTimeAI;
            SimulationHandler.Buildings.UpdateFrameDuration();

            CitizenManagerPatch.NewCitizenBehavior = new NewCitizenBehavior(randomizer, configProvider.Configuration);

            SimulationHandler.Buildings.InitializeLightState();

            SimulationHandler.Statistics = statistics;

            WorldInfoPanelPatch.CitizenInfoPanel = CustomCitizenInfoPanel.Enable(ResidentAIPatch.RealTimeAI, localizationProvider);
            WorldInfoPanelPatch.VehicleInfoPanel = CustomVehicleInfoPanel.Enable(ResidentAIPatch.RealTimeAI, localizationProvider);
            WorldInfoPanelPatch.CampusWorldInfoPanel = CustomCampusWorldInfoPanel.Enable(localizationProvider);

            AwakeSleepSimulation.Install(configProvider.Configuration);

            var schedulesStorage = ResidentAIPatch.RealTimeAI.GetStorageService(
                schedules => new CitizenScheduleStorage(schedules, gameConnections.CitizenManager.GetCitizensArray, timeInfo));

            result.storageData.Add(schedulesStorage);
            result.storageData.Add(eventManager);
            if (StorageBase.CurrentLevelStorage != null)
            {
                StorageBase.CurrentLevelStorage.GameSaving += result.GameSaving;
                LoadStorageData(result.storageData, StorageBase.CurrentLevelStorage);
            }

            result.storageData.Add(configProvider);
            result.Translate(localizationProvider);

            return result;
        }

        /// <summary>
        /// Stops the mod by deactivating all its parts.
        /// </summary>
        public void Stop()
        {
            if (!isEnabled)
            {
                return;
            }

            ResidentAIPatch.RealTimeAI = null;
            TouristAIPatch.RealTimeAI = null;
            BuildingAIPatch.RealTimeAI = null;
            VehicleAIPatch.RealTimeAI = null;
            WorldInfoPanelPatch.RealTimeAI = null;
            BuildingAIPatch.WeatherInfo = null;
            TransferManagerPatch.RealTimeAI = null;
            SimulationHandler.EventManager = null;
            SimulationHandler.DayTimeSimulation = null;
            SimulationHandler.TimeAdjustment = null;
            SimulationHandler.WeatherInfo = null;
            SimulationHandler.Buildings = null;
            SimulationHandler.CitizenProcessor = null;
            SimulationHandler.Statistics?.Close();
            SimulationHandler.Statistics = null;
            ParkPatch.SpareTimeBehavior = null;
            OutsideConnectionAIPatch.SpareTimeBehavior = null;
            CitizenManagerPatch.NewCitizenBehavior = null;

            vanillaEvents.Revert();

            timeAdjustment.Disable();
            timeBar.CityEventClick -= CustomTimeBarCityEventClick;
            timeBar.Disable();
            eventManager.EventsChanged -= CityEventsChanged;
            SimulationHandler.NewDay -= CityEventsChanged;

            CityEventsLoader.Instance.Clear();

            AwakeSleepSimulation.Uninstall();

            StorageBase.CurrentLevelStorage.GameSaving -= GameSaving;

            WorldInfoPanelPatch.CitizenInfoPanel?.Disable();
            WorldInfoPanelPatch.CitizenInfoPanel = null;

            WorldInfoPanelPatch.VehicleInfoPanel?.Disable();
            WorldInfoPanelPatch.VehicleInfoPanel = null;

            WorldInfoPanelPatch.CampusWorldInfoPanel?.Disable();
            WorldInfoPanelPatch.CampusWorldInfoPanel = null;

            isEnabled = false;
        }

        /// <summary>
        /// Translates all the mod's component to a different language obtained from
        /// the specified <paramref name="localizationProvider"/>.
        /// </summary>
        ///
        /// <exception cref="ArgumentNullException">Thrown when the argument is null.</exception>
        ///
        /// <param name="localizationProvider">An instance of the <see cref="ILocalizationProvider"/> to use for translation.</param>
        public void Translate(ILocalizationProvider localizationProvider)
        {
            if (localizationProvider == null)
            {
                throw new ArgumentNullException(nameof(localizationProvider));
            }

            timeBar.Translate(localizationProvider.CurrentCulture);
            UIGraphPatch.Translate(localizationProvider.CurrentCulture);
        }

        private static void CheckMethodPatches(Compatibility compatibility)
        {
            if (compatibility.IsAnyModActive(WorkshopMods.CitizenLifecycleRebalance, WorkshopMods.LifecycleRebalanceRevisited))
            {
                ApplyCitizenPatch = false;
                Log.Info("The 'Real Time' mod will not change the citizens aging because a 'Lifecycle Rebalance' mod is active.");
            }
            else
            {
                ApplyCitizenPatch = true;
            }

            if (compatibility.IsAnyModActive(
                WorkshopMods.BuildingThemes,
                WorkshopMods.ForceLevelUp,
                WorkshopMods.PloppableRico,
                WorkshopMods.PloppableRicoHighDensityFix,
                WorkshopMods.PloppableRicoRevisited,
                WorkshopMods.PlopTheGrowables))
            {
                ApplyBuildingPatch = false;
                Log.Info("The 'Real Time' mod will not change the building construction and upgrading behavior because some building mod is active.");
            }
            else
            {
                ApplyBuildingPatch = true;
            }
        }

        private static bool SetupCustomAI(
            TimeInfo timeInfo,
            RealTimeConfig config,
            GameConnections<Citizen> gameConnections,
            RealTimeEventManager eventManager,
            Compatibility compatibility)
        {
            var residentAIConnection = ResidentAIPatch.GetResidentAIConnection();
            if (residentAIConnection == null)
            {
                return false;
            }

            float travelDistancePerCycle = compatibility.IsAnyModActive(WorkshopMods.RealisticWalkingSpeed)
                ? Constants.AverageTravelDistancePerCycle * 0.583f
                : Constants.AverageTravelDistancePerCycle;

            var spareTimeBehavior = new SpareTimeBehavior(config, timeInfo);
            var travelBehavior = new TravelBehavior(gameConnections.BuildingManager, travelDistancePerCycle);
            var workBehavior = new WorkBehavior(config, gameConnections.Random, gameConnections.BuildingManager, timeInfo, travelBehavior);

            ParkPatch.SpareTimeBehavior = spareTimeBehavior;
            OutsideConnectionAIPatch.SpareTimeBehavior = spareTimeBehavior;

            var realTimeBuildingAI = new RealTimeBuildingAI(
                config,
                timeInfo,
                gameConnections.BuildingManager,
                new ToolManagerConnection(),
                workBehavior,
                travelBehavior);

            BuildingAIPatch.RealTimeAI = realTimeBuildingAI;
            BuildingAIPatch.WeatherInfo = gameConnections.WeatherInfo;
            VehicleAIPatch.RealTimeAI = realTimeBuildingAI;
            TransferManagerPatch.RealTimeAI = realTimeBuildingAI;
            WorldInfoPanelPatch.RealTimeAI = realTimeBuildingAI;

            var realTimeResidentAI = new RealTimeResidentAI<ResidentAI, Citizen>(
                config,
                gameConnections,
                residentAIConnection,
                eventManager,
                realTimeBuildingAI,
                workBehavior,
                spareTimeBehavior,
                travelBehavior);

            ResidentAIPatch.RealTimeAI = realTimeResidentAI;
            SimulationHandler.CitizenProcessor = new CitizenProcessor<ResidentAI, Citizen>(
                realTimeResidentAI, timeInfo, spareTimeBehavior, travelBehavior);

            var touristAIConnection = TouristAIPatch.GetTouristAIConnection();
            if (touristAIConnection == null)
            {
                return false;
            }

            var realTimeTouristAI = new RealTimeTouristAI<TouristAI, Citizen>(
                config,
                gameConnections,
                touristAIConnection,
                eventManager,
                spareTimeBehavior);

            TouristAIPatch.RealTimeAI = realTimeTouristAI;

            return true;
        }

        private static void CustomTimeBarCityEventClick(object sender, CustomTimeBarClickEventArgs e)
            => CameraHelper.NavigateToBuilding(e.CityEventBuildingId, zoomIn: true);

        private static void LoadStorageData(IEnumerable<IStorageData> storageData, StorageBase storage)
        {
            foreach (var item in storageData)
            {
                storage.Deserialize(item);
                Log.Debug(LogCategory.Generic, "The 'Real Time' mod loaded its data from container " + item.StorageDataId);
            }
        }

        private void CityEventsChanged(object sender, EventArgs e) => timeBar.UpdateEventsDisplay(eventManager.AllEvents);

        private void GameSaving(object sender, EventArgs e)
        {
            var storage = (StorageBase)sender;
            foreach (var item in storageData)
            {
                storage.Serialize(item);
                Log.Debug(LogCategory.Generic, "The 'Real Time' mod stored its data in the current game for container " + item.StorageDataId);
            }
        }
    }
}
