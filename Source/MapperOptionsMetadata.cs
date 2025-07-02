using System;
using System.Collections.Generic;

namespace Celeste.Mod.MapperOptions
{
    public class MapOptionsMetadata
    {
        public List<Option> MapOptions { get; set; } = new();
    }

    public class Option
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool Global { get; set; } = true;
        public string Description { get; set; }
        public bool StartingBoolValue { get; set; } = false;  
        public string Values { get; set; }
        public List<SliderValue> AdvancedValues { get; set; }
    }

    public class SliderValue
    {
        public string Value { get; set; }
        public string VisibleFlag { get; set; }
    }

    public class MapperOptionsMetadata
    {
        public static TextMenu menu;
        public static Dictionary<string, Type> PossibleOptionTypes = new()
        {
            {"onoff", typeof(TextMenu.OnOff) },
            {"slider", typeof(TextMenu.Slider) },
            {"subheader", typeof(TextMenu.SubHeader) }
        };
        public static List<Option> MapOptions;

        public static List<Option> TryGetMapperOptionsMetadata(Session session)
        {
            MapperOptionsModule.ShouldAddOptions = false;

            if (!Everest.Content.TryGet($"Maps/{session.MapData.Filename}.meta", out ModAsset asset)) return null;
            if (!(asset?.PathVirtual?.StartsWith("Maps") ?? false)) return null;
            if (!(asset?.TryDeserialize(out MapOptionsMetadata meta) ?? false)) 
            {
                Logger.Error("MapperOptions", "Failed to deserialize meta.yaml. Check your formatting!");
                return null; 
            }
            MapOptions = meta?.MapOptions;
            
            foreach (Option option in MapOptions)
            {
                if (!MapperOptionsModuleSettings.EnabledOptions.ContainsKey(option.Name)) {
                    MapperOptionsModuleSettings.EnabledOptions.Add(option.Name, option.Global);
                }
                if (option != null && option.Global && MapperOptionsModule.ShouldAddOptions == false)
                {
                    // If we have *any* non-null global options, add the menu
                    MapperOptionsModule.ShouldAddOptions = true;
                }
            }

            return MapOptions;
        }

        public static TextMenu CreateOptionsMenu(Level level)
        {
            // Create the menu
            menu = new TextMenu();

            // Add the fancy cool header
            menu.Add(new TextMenu.Header(Dialog.Clean("MapperOptions_Title")));

            foreach (Option o in MapOptions)
            {
                // Ensure this option is either global (shows up everywhere) or enabled in this zone
                MapperOptionsModuleSettings.EnabledOptions.TryGetValue(o.Name, out bool enabled);
                if (enabled)
                {
                    // Name and type are required for every option. Everything else is optional and changes per-type
                    string itemName = Dialog.Clean(o.Name);
                    string itemType = o.Type.ToLower();
                    TextMenu.Item item = null;

                    // Get the type and make sure it's valid.
                    if (!PossibleOptionTypes.TryGetValue(itemType, out Type T)) throw new Exception("type not found");

                    // Construct the menu option based on what the type is.
                    if (T == typeof(TextMenu.OnOff))
                    {
                        // If this is the first time opening this menu, we need to add the option to our Big Dictionary
                        if (!MapperOptionsModuleSettings.BoolOptions.ContainsKey(o.Name))
                            MapperOptionsModuleSettings.BoolOptions.Add(o.Name, o.StartingBoolValue);

                        item = new TextMenu.OnOff(itemName, MapperOptionsModuleSettings.BoolOptions.GetValueOrDefault(o.Name, o.StartingBoolValue)).Change(value =>
                        {
                            level.Session.SetFlag("MO_" + o.Name, value);
                            MapperOptionsModuleSettings.BoolOptions[o.Name] = value;
                        });

                        // If it starts true, make sure the flag starts enabled too
                        MapperOptionsModuleSettings.BoolOptions.TryGetValue(o.Name, out bool b);
                        if (b) level.Session.SetFlag("MO_" + o.Name, true);
                    }

                    else if (T == typeof(TextMenu.Slider))
                    {
                        if (o.AdvancedValues == null)
                        {
                            // Break our big single string of all the field options down into an array
                            string[] values = o.Values.Replace(" ", "").Split(',');
                            if (values.Length == 0)
                                throw new ArgumentException(
                                    "Mapper Options: A slider option is specified but has no possible values.");
                            string[] cleanedValues = new string[values.Length];
                            for (int i = 0; i < values.Length; i++)
                            {
                                cleanedValues[i] = Dialog.Clean(values[i]);
                            }


                            // If this is the first time opening this menu, we need to add the option to our Big Dictionary
                            if (!MapperOptionsModuleSettings.StringOptions.ContainsKey(o.Name))
                                MapperOptionsModuleSettings.StringOptions.Add(o.Name, 0);

                            item = new TextMenu.Slider(itemName, (int i) => { return cleanedValues[i]; }, 0,
                                    values.Length - 1,
                                    MapperOptionsModuleSettings.StringOptions.GetValueOrDefault(o.Name, 0))
                                .Change(i =>
                                {
                                    for (int j = 0; j < values.Length; j++)
                                    {
                                        level.Session.SetFlag("MO_" + o.Name + "_" + values[j], (i == j));
                                    }

                                    MapperOptionsModuleSettings.StringOptions[o.Name] = i;
                                });

                            // Make sure to set the flag for the appropriate starting option
                            level.Session.SetFlag("MO_" + o.Name + "_" + values[0], true);
                        }
                        else
                        {
                            if (o.AdvancedValues.Count == 0)
                                throw new ArgumentException("Mapper Options: A slider option is specified but has no possible values.");
                            List<string> cleanedValues = new();
                            
                            for (int i = 0; i < o.AdvancedValues.Count; i++)
                            {
                                if (level.Session.GetFlag(o.AdvancedValues[i].VisibleFlag)) 
                                    cleanedValues[i] = Dialog.Clean(o.AdvancedValues[i].Value);
                            }

                            string[] cleanedValuesArr = cleanedValues.ToArray();
                            
                            if (!MapperOptionsModuleSettings.StringOptions.ContainsKey(o.Name))
                                MapperOptionsModuleSettings.StringOptions.Add(o.Name, 0);

                            item = new TextMenu.Slider(
                                itemName,
                                (int i) => { return cleanedValuesArr[i]; },
                                0,
                                o.AdvancedValues.Count - 1,
                                MapperOptionsModuleSettings.StringOptions.GetValueOrDefault(o.Name, 0))
                            .Change(i =>
                            {
                                for (int j = 0; j < cleanedValuesArr.Length; j++)
                                { 
                                    level.Session.SetFlag("MO_" + o.Name + "_" + cleanedValuesArr[j], (i == j));
                                }
                                MapperOptionsModuleSettings.StringOptions[o.Name] = i;
                            });
                            
                            level.Session.SetFlag("MO_" + o.Name + "_" + cleanedValuesArr[0]);
                        }
                    }

                    else if (T == typeof(TextMenu.SubHeader))
                    {
                        item = new TextMenu.SubHeader(itemName);
                    }

                    else
                    {
                        // Something has gone horribly wrong.
                        throw new InvalidOperationException("Run.");
                    }

                    // Give everything a description if present. Needs to happen after adding the item to a menu because descriptions are dogshit.
                    menu.Add(item);
                    if (o.Description != null) item.AddDescription(menu, Dialog.Clean(o.Description));
                }
            }

            // Send back the assembled menu.
            return menu;
        }
    }
}
