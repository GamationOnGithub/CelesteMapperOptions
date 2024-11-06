using System.Collections.Generic;

namespace Celeste.Mod.MapperOptions;

public class MapperOptionsModuleSettings : EverestModuleSettings {

    [SettingIgnore]
    public static Dictionary<string, bool> BoolOptions { get; set; } = new();

    [SettingIgnore]
    public static Dictionary<string, int> StringOptions { get; set; } = new();
}