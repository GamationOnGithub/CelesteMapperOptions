using Celeste.Mod.Meta;
using System.Collections.Generic;
using System;
using Monocle;

namespace Celeste.Mod.MapperOptions;

public class MapperOptionsModule : EverestModule {
    public static MapperOptionsModule Instance { get; private set; }

    public override Type SettingsType => typeof(MapperOptionsModuleSettings);
    public static MapperOptionsModuleSettings Settings => (MapperOptionsModuleSettings) Instance._Settings;

    public override Type SessionType => typeof(MapperOptionsModuleSession);
    public static MapperOptionsModuleSession Session => (MapperOptionsModuleSession) Instance._Session;

    public override Type SaveDataType => typeof(MapperOptionsModuleSaveData);
    public static MapperOptionsModuleSaveData SaveData => (MapperOptionsModuleSaveData) Instance._SaveData;

    public List<Option> Options;

    public static bool ShouldAddOptions = false;

    public MapperOptionsModule() {
        Instance = this;
#if DEBUG
        // debug builds use verbose logging
        Logger.SetLogLevel(nameof(MapperOptionsModule), LogLevel.Verbose);
#else
        // release builds use info logging to reduce spam in log files
        Logger.SetLogLevel(nameof(MapperOptionsModule), LogLevel.Info);
#endif
    }

    private static void Hook_OnCreatePauseMenuButtons(Level level, TextMenu menu, bool minimal)
    {
        if (ShouldAddOptions)
        {
            int menuIndex = menu.Items.FindIndex(item =>
            item.GetType() == typeof(TextMenu.Button) && ((TextMenu.Button)item).Label == Dialog.Clean("menu_pause_savequit"));

            if (menuIndex < 0)
            {
                return;
            }

            menu.Insert(menuIndex - 1, new TextMenu.Button(Dialog.Clean("MapperOptions_Title"))
            {
                OnPressed = () => {
                    menu.RemoveSelf();

                    TextMenu accMenu = MapperOptionsMetadata.CreateOptionsMenu(level);

                    bool comesFromPauseMainMenu = level.PauseMainMenuOpen;
                    level.PauseMainMenuOpen = false;

                    accMenu.OnESC = accMenu.OnCancel = () => {
                        // close this menu
                        Audio.Play(SFX.ui_main_button_back);

                        accMenu.Close();

                        // readd original menu
                        level.Add(menu);

                        level.PauseMainMenuOpen = comesFromPauseMainMenu;
                    };

                    accMenu.OnPause = () => {
                        Audio.Play(SFX.ui_main_button_back);

                        accMenu.Close();

                        level.Paused = false;
                        Engine.FreezeTimer = 0.15f;
                    };

                    level.Add(accMenu);
                }
            });
        }
    }

    private void Hook_OnLevelLoad(Level level, Player.IntroTypes playerIntro, bool isFromLoader)
    {
        if (isFromLoader) {
            List<Option> options = MapperOptionsMetadata.TryGetMapperOptionsMetadata(level.Session);
            if (options == null) return;
            this.Options = options;
        }
    }

    public override void Load() {
        Everest.Events.Level.OnLoadLevel += Hook_OnLevelLoad;
        Everest.Events.Level.OnCreatePauseMenuButtons += Hook_OnCreatePauseMenuButtons;
    }

    public override void Unload() {
        Everest.Events.Level.OnLoadLevel -= Hook_OnLevelLoad;
        Everest.Events.Level.OnCreatePauseMenuButtons -= Hook_OnCreatePauseMenuButtons;
    }
}