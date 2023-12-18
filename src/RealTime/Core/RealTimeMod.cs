// <copyright file="RealTimeMod.cs" company="dymanoid">Copyright (c) dymanoid. All rights reserved.</copyright>

namespace RealTime.Core
{
    using System;
    using System.IO;
    using System.Linq;
    using CitiesHarmony.API;
    using ColossalFramework;
    using ColossalFramework.Globalization;
    using ColossalFramework.IO;
    using ColossalFramework.Plugins;
    using ICities;
    using RealTime.Config;
    using RealTime.CustomAI;
    using RealTime.Localization;
    using RealTime.UI;
    using SkyTools.Configuration;
    using SkyTools.Localization;
    using SkyTools.Tools;
    using SkyTools.UI;
    using UnityEngine;

    /// <summary>The main class of the Real Time mod.</summary>
    public class RealTimeMod : LoadingExtensionBase, IUserMod
    {
        private const long WorkshopId = 3059406297;
        private const string NoWorkshopMessage = "Real Time can only run when subscribed to in Steam Workshop";

        private readonly string modVersion = GitVersion.GetAssemblyVersion(typeof(RealTimeMod).Assembly);
        private readonly string modPath = GetModPath();

        public static ConfigurationProvider<RealTimeConfig> configProvider;
        private RealTimeCore core;
        private ConfigUI configUI;
        private LocalizationProvider localizationProvider;

        /// <summary>Gets the name of this mod.</summary>
        public string Name => "Real Time";

        /// <summary>Gets the description string of this mod.</summary>
        public string Description => "Adjusts the time flow and the Cims behavior to make them more real. Version: " + modVersion;

        /// <summary>Called when this mod is enabled.</summary>
        public void OnEnabled()
        {
            Log.SetupDebug(Name, LogCategory.Generic);

            Log.Info("The 'Real Time' mod has been enabled, version: " + modVersion);
            configProvider = new ConfigurationProvider<RealTimeConfig>(RealTimeConfig.StorageId, Name, () => new RealTimeConfig(latestVersion: true));
            configProvider.LoadDefaultConfiguration();
            localizationProvider = new LocalizationProvider(Name, modPath);
            HarmonyHelper.DoOnHarmonyReady(() => PatchUtil.PatchAll());
        }

        /// <summary>Called when this mod is disabled.</summary>
        public void OnDisabled()
        {
            CloseConfigUI();
            if (configProvider?.IsDefault == true)
            {
                configProvider.SaveDefaultConfiguration();
            }

            if (HarmonyHelper.IsHarmonyInstalled)
            {
                PatchUtil.UnpatchAll();
            }

            Log.Info("The 'Real Time' mod has been disabled.");
        }

        /// <summary>Called when this mod's settings page needs to be created.</summary>
        /// <param name="helper">
        /// An <see cref="UIHelperBase"/> reference that can be used to construct the mod's settings page.
        /// </param>
        public void OnSettingsUI(UIHelperBase helper)
        {
            if (helper == null || configProvider == null)
            {
                return;
            }

            if (configProvider.Configuration == null)
            {
                Log.Warning("The 'Real Time' mod wants to display the configuration page, but the configuration is unexpectedly missing.");
                configProvider.LoadDefaultConfiguration();
            }

            IViewItemFactory itemFactory = new CitiesViewItemFactory(helper);
            CloseConfigUI();
            configUI = ConfigUI.Create(configProvider, itemFactory);
            ApplyLanguage();
        }


        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);
            try
            {
                FireBurnTimeManager.Init();
                BuildingWorkTimeManager.Init();
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                FireBurnTimeManager.Deinit();
                BuildingWorkTimeManager.Deinit();
            }
        }

        public override void OnReleased()
        {
            base.OnReleased();
            try
            {
                FireBurnTimeManager.Deinit();
                BuildingWorkTimeManager.Deinit();
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
            }
        }

        /// <summary>
        /// Called when a game level is loaded. If applicable, activates the Real Time mod for the loaded level.
        /// </summary>
        /// <param name="mode">The <see cref="LoadMode"/> a game level is loaded in.</param>
        public override void OnLevelLoaded(LoadMode mode)
        {
            switch (mode)
            {
                case LoadMode.LoadGame:
                case LoadMode.NewGame:
                case LoadMode.LoadScenario:
                case LoadMode.NewGameFromScenario:
                    break;

                default:
                    return;
            }

            Log.Info($"The 'Real Time' mod starts, game mode {mode}.");

            var compatibility = Compatibility.Create(localizationProvider);

            bool isNewGame = mode == LoadMode.NewGame || mode == LoadMode.NewGameFromScenario;
            core = RealTimeCore.Run(configProvider, modPath, localizationProvider, isNewGame, compatibility);
            if (core == null)
            {
                Log.Warning("Showing a warning message to user because the mod isn't working");
                MessageBox.Show(
                    localizationProvider.Translate(TranslationKeys.Warning),
                    localizationProvider.Translate(TranslationKeys.ModNotWorkingMessage));
            }
            else
            {
                CheckCompatibility(compatibility);
            }
        }

        /// <summary>
        /// Called when a game level is about to be unloaded. If the Real Time mod was activated for this level,
        /// deactivates the mod for this level.
        /// </summary>
        public override void OnLevelUnloading()
        {
            if (core != null)
            {
                Log.Info("The 'Real Time' mod stops.");
                core = null;
            }

            configProvider.LoadDefaultConfiguration();
        }

        private static string GetModPath()
        {
            string addonsPath = Path.Combine(DataLocation.localApplicationData, "Addons");
            string localModsPath = Path.Combine(addonsPath, "Mods");
            string localModPath = Path.Combine(localModsPath, "RealTime");

            if(Directory.Exists(localModPath))
            {
                return localModPath;
            }

            var pluginInfo = PluginManager.instance.GetPluginsInfo()
                .FirstOrDefault(pi => pi.name.Contains("RealTime"));

            return pluginInfo?.modPath;
        }

        private void CheckCompatibility(Compatibility compatibility)
        {
            if (core == null)
            {
                return;
            }

            string message = null;
            bool incompatibilitiesDetected = configProvider.Configuration.ShowIncompatibilityNotifications
                && compatibility.AreAnyIncompatibleModsActive(out message);

            if (core.IsRestrictedMode)
            {
                message += localizationProvider.Translate(TranslationKeys.RestrictedMode);
            }

            if (incompatibilitiesDetected || core.IsRestrictedMode)
            {
                Notification.Notify(Name + " - " + localizationProvider.Translate(TranslationKeys.Warning), message);
            }
        }

        private void ApplyLanguage()
        {
            if (!SingletonLite<LocaleManager>.exists)
            {
                return;
            }

            if (localizationProvider.LoadTranslation(LocaleManager.instance.language))
            {
                localizationProvider.SetEnglishUSFormatsState(configProvider.Configuration.UseEnglishUSFormats);
                core?.Translate(localizationProvider);
            }

            configUI?.Translate(localizationProvider);
        }

        private void CloseConfigUI()
        {
            if (configUI != null)
            {
                configUI.Close();
                configUI = null;
            }
        }
    }
}
