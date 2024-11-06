using Celeste.Mod.Meta;
using System.Collections.Generic;
using System;
using Monocle;
using System.Linq;

namespace Celeste.Mod.MapperOptions;

public class MapperOptionsModule : EverestModule {
    public static MapperOptionsModule Instance { get; private set; }

    public override Type SettingsType => typeof(MapperOptionsModuleSettings);
    public static MapperOptionsModuleSettings Settings => (MapperOptionsModuleSettings) Instance._Settings;

    public override Type SaveDataType => typeof(MapperOptionsModuleSaveData);
    public static MapperOptionsModuleSaveData SaveData => (MapperOptionsModuleSaveData) Instance._SaveData;

    public List<Option> Options;

    public static bool ShouldAddOptions = false;

    public static TextMenu OptionsMenu;

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
                OnPressed = () =>
                {
                    menu.RemoveSelf();

                    bool comesFromPauseMainMenu = level.PauseMainMenuOpen;
                    level.PauseMainMenuOpen = false;

                    OptionsMenu.OnESC = OptionsMenu.OnCancel = () =>
                    {
                        // close this menu
                        Audio.Play(SFX.ui_main_button_back);

                        OptionsMenu.Close();

                        // readd original menu
                        level.Add(menu);

                        level.PauseMainMenuOpen = comesFromPauseMainMenu;
                    };

                    OptionsMenu.OnPause = () =>
                    {
                        Audio.Play(SFX.ui_main_button_back);

                        OptionsMenu.Close();

                        level.Paused = false;
                        Engine.FreezeTimer = 0.15f;
                    };

                    level.Add(OptionsMenu);

                    // Make sure the description on the last selected item gets added, by force.
                    // If this is the first time opening the menu, find the first non-(sub)header option and add that description instead.
                    if (OptionsMenu.Current != null && OptionsMenu.Current.OnEnter != null) OptionsMenu.Current.OnEnter();
                    else
                    {
                        foreach (TextMenu.Item item in OptionsMenu.Items)
                        {
                            if (item is TextMenu.Header or TextMenu.SubHeader) continue;
                            if (item.OnEnter != null) item.OnEnter();
                            break;
                        }
                    }
                }
            });
        }
    }

    private void Hook_OnLevelLoad(Level level, Player.IntroTypes playerIntro, bool isFromLoader)
    {
        if (isFromLoader) {
            List<Option> options = MapperOptionsMetadata.TryGetMapperOptionsMetadata(level.Session);
            if (options != null) OptionsMenu = MapperOptionsMetadata.CreateOptionsMenu(level); else return;
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