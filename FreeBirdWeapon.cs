using FreeBirdWeapon.Common.Players;
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
        internal enum MessageType : byte
        {
            FreeBirdMusicSync,
            FreeBirdMusicStop
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            MessageType msgType = (MessageType)reader.ReadByte();

            switch (msgType)
            {
                case MessageType.FreeBirdMusicSync:
                    int playerID = reader.ReadByte();
                    bool isHolding = reader.ReadBoolean();
                    bool isUsing = reader.ReadBoolean();
                    long musicTime = reader.ReadInt64();
                    
                    if (playerID >= 0 && playerID < Main.maxPlayers)
                    {
                        FreeBirdPlayer fbPlayer = Main.player[playerID].GetModPlayer<FreeBirdPlayer>();
                        fbPlayer.ReceiveMusicSync(isHolding, isUsing, musicTime);
                    }
                    
                    // Server forwards to all other clients
                    if (Main.netMode == NetmodeID.Server)
                    {
                        ModPacket packet = GetPacket();
                        packet.Write((byte)MessageType.FreeBirdMusicSync);
                        packet.Write((byte)playerID);
                        packet.Write(isHolding);
                        packet.Write(isUsing);
                        packet.Write(musicTime);
                        packet.Send(-1, whoAmI);
                    }
                    break;
                    
                case MessageType.FreeBirdMusicStop:
                    int stoppingPlayerID = reader.ReadByte();
                    
                    if (stoppingPlayerID >= 0 && stoppingPlayerID < Main.maxPlayers)
                    {
                        FreeBirdPlayer fbPlayer = Main.player[stoppingPlayerID].GetModPlayer<FreeBirdPlayer>();
                        fbPlayer.ReceiveMusicStop();
                    }
                    
                    // Server forwards to all other clients
                    if (Main.netMode == NetmodeID.Server)
                    {
                        ModPacket packet = GetPacket();
                        packet.Write((byte)MessageType.FreeBirdMusicStop);
                        packet.Write((byte)stoppingPlayerID);
                        packet.Send(-1, whoAmI);
                    }
                    break;
                    
                default:
                    Logger.WarnFormat("FreeBirdWeapon: Unknown Message type: {0}", msgType);
                    break;
            }
        }
    }
}
