using System.Collections.Generic;

namespace Celeste.Mod.MapperOptions;

public class MapperOptionsModuleSaveData : EverestModuleSaveData {
    public static Dictionary<string, bool> BoolOptions { get; set; } = new();
    public static Dictionary<string, int> StringOptions { get; set; } = new();
}