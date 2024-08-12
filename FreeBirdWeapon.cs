using FreeBirdWeapon.Common.Player;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FreeBirdWeapon
{
    // Please read https://github.com/tModLoader/tModLoader/wiki/Basic-tModLoader-Modding-Guide#mod-skeleton-contents for more information about the various files in a mod.
    public class FreeBirdWeapon : Mod
    {
        /*internal enum MessageType : byte
        {
            HoldGuitarMoveSpeedSync,
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            MessageType msgType = (MessageType)reader.ReadByte();

            switch (msgType)
            {
                case MessageType.HoldGuitarMoveSpeedSync:
                    int playerID = reader.ReadByte();
                    FreeBirdPlayer packetThing = Main.player[playerID].GetModPlayer<FreeBirdPlayer>();
                    packetThing.ReceivePlayerSync(reader);
                    if (Main.netMode == NetmodeID.Server)
                        packetThing.SyncPlayer(-1, whoAmI, false);
                    break;
                default:
                    Logger.WarnFormat("FreeBirdWeapon: Unknown Message type: {0}", msgType);
                    break;
            }
        }*/
    }
}
