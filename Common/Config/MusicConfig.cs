using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace FreeBirdWeapon.Common.Config
{
    public class MusicConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [Header("MusicToggle")]
        [DefaultValue(false)]
        [ReloadRequired]
        public bool MidiSong;
    }
}
