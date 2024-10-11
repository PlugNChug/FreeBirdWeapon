using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Terraria.ModLoader.Config;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FreeBirdWeapon.Common.Config
{
    public class GameConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [Header("GameConfig")]
        [DefaultValue(false)]
        [ReloadRequired]
        public bool EasyCraft;

        [DefaultValue(false)]
        public bool ScalingDamage;

        [DefaultValue(false)]
        public bool CustomDamage;
        /*{
            get => CustomDamageAmount > 0;
            set
            {
                if (value)
                {
                    CustomDamageAmount = 80;
                }
            }
        }*/

        [DefaultValue("0")]
        public string CustomDamageAmount;

        public override void OnChanged()
        {
            string s = "";
            foreach (char c in CustomDamageAmount)
            {
                if (char.IsDigit(c))
                {
                    s += c;
                }
            }
            if (s.Length > 7)
            {
                s = s[..7];
            }
            if (s.Length == 0)
            {
                s = "0";
            }
            CustomDamageAmount = Math.Abs(int.Parse(s)).ToString();
        }
    }
}
