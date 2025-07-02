using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Celeste.Mod.MapperOptions.Triggers
{
    [CustomEntity("MapperOptions/OptionZoneTrigger")]
    public class OptionZoneTrigger : Trigger
    {
        public string[] toAffect;

        public OptionZoneTrigger(EntityData data, Vector2 offset) : base(data, offset)
        {
            toAffect = data.Attr("toEnable", null).Split(',');
            for (int i = 0; i < toAffect.Length; i++)
            {
                toAffect[i] = toAffect[i].Trim();
            }
        }

        public override void OnEnter(Player player)
        {
            // Loop through every local option
            foreach (string s in toAffect)
            {

                // Make sure this option is actually real so we don't toggle something fake
                if (MapperOptionsModuleSettings.EnabledOptions.ContainsKey(s))
                {
                    // Make sure the menu shows up
                    MapperOptionsModule.ShouldAddOptions = true;

                    // Turn it on, since we're inside the trigger
                    MapperOptionsModuleSettings.EnabledOptions[s] = true;
                }
                // Yell
                else Logger.Warn("MapperOptions", $"Option Zone Trigger contains an option {s} that isn't defined. Fix it!");
            }
            base.OnEnter(player);
        }

        public override void OnLeave(Player player)
        {
            base.OnLeave(player);
            foreach (string s in toAffect)
            {
                if (MapperOptionsModuleSettings.EnabledOptions.ContainsKey(s))
                    MapperOptionsModuleSettings.EnabledOptions[s] = MapperOptionsMetadata.MapOptions.Find(option => option.Name == s).Global;
                else Logger.Warn("MapperOptions", $"Option Zone Trigger contains an option {s} that isn't defined. Fix it!");
            }

            // Disable the menu in case all options are local
            MapperOptionsModule.ShouldAddOptions = false;
            foreach (string option in MapperOptionsModuleSettings.EnabledOptions.Keys)
            {
                // If any options should be enabled after we disable the local ones, make sure the menu stays on
                if (MapperOptionsModuleSettings.EnabledOptions[option] == true) MapperOptionsModule.ShouldAddOptions = true;
            }
        }
    }
}
