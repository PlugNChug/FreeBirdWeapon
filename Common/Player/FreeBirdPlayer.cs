using FreeBirdWeapon.Common.Config;
using FreeBirdWeapon.Content.Items.Weapons;
using Microsoft.Build.Tasks;
using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace FreeBirdWeapon.Common.Player
{
    internal class FreeBirdSceneEffect : ModSceneEffect
    {
        public override int Music => 0;
        public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;

        public override bool IsSceneEffectActive(Terraria.Player player)
        {
            return player.GetModPlayer<FreeBirdPlayer>().holdingFreeBirdWeapon;
        }
    }
    internal class FreeBirdPlayer : ModPlayer
    {
        // Bool values that will be checked every frame to see if the player is doing anything with the Free Bird Guitar
        public bool holdingFreeBirdWeapon = false;
        public bool usingFreeBirdWeapon = false;

        // For when the player is using the guitar
        private int tickCounter;

        /*// Helper variables for faster run speed checking
        private float runMultiplier = 1f;
        private float accMultiplier = 1f;*/

        /*// So we don't bloat the network
        internal enum EnablePackets
        {
            Waiting,
            Enabled,
            Disabled
        }
        internal EnablePackets packetsEnabled = EnablePackets.Waiting;*/

        // Allocate sound slots for both the backing track and the solo
        private SlotId backingTrackSlot;
        private SlotId soloSlot;

        // These are the sounds that will be played when interacting with the guitar. Both sounds classified as music.
        private SoundStyle backingTrack = new SoundStyle(BackingTrackChooser(), 0, SoundType.Music);
        private SoundStyle solo = new SoundStyle(SoloChooser(), 0, SoundType.Music);

        /*private SoundStyle backingTrackMidi = new SoundStyle("FreeBirdWeapon/Assets/BackingTrackMidi", 0, SoundType.Music);
        private SoundStyle soloMidi = new SoundStyle("FreeBirdWeapon/Assets/SoloMidi", 0, SoundType.Music);*/


        // This variable exists so the sounds above abide by the game's sound settings
        private float musicVolume = Main.musicVolume * 0.8f;

        private static string BackingTrackChooser()
        {
            if (ModContent.GetInstance<MusicConfig>().MidiSong)
                return "FreeBirdWeapon/Assets/BackingTrackMidi";
            return "FreeBirdWeapon/Assets/BackingTrack";
        }

        private static string SoloChooser()
        {
            if (ModContent.GetInstance<MusicConfig>().MidiSong)
                return "FreeBirdWeapon/Assets/SoloMidi";
            return "FreeBirdWeapon/Assets/Solo";
        }

        // The initial load for the above sounds. Most likely will lag for a second upon entering a world.
        public override void OnEnterWorld()
        {
            backingTrackSlot = SoundEngine.PlaySound(backingTrack, Player.position);
            SoundEngine.TryGetActiveSound(backingTrackSlot, out var activeBackingTrack);
            activeBackingTrack.Stop();

            soloSlot = SoundEngine.PlaySound(solo, Player.position);
            SoundEngine.TryGetActiveSound(soloSlot, out var activeSolo);
            activeSolo.Stop();
        }

        // Constantly checks if the player is interacting (holding, using) with the guitar.
        public override void PostUpdate()
        {
            // These flags check if the sounds are currently playing- and they also prevent certain errors from occurring
            bool flag = SoundEngine.TryGetActiveSound(backingTrackSlot, out var activeBackingTrack);
            bool flag2 = SoundEngine.TryGetActiveSound(soloSlot, out var activeSolo);

            // If the player is holding the guitar and the sounds are not playing, start playing the sounds
            if (!flag && holdingFreeBirdWeapon) {
                if (Main.gameInactive)
                    return;
                backingTrackSlot = SoundEngine.PlaySound(backingTrack, Player.position);
                soloSlot = SoundEngine.PlaySound(solo, Player.position);
                // By default, the solo should be muted until the player starts using the guitar
                if (flag2)
                    activeSolo.Volume = 0;
            } else if (holdingFreeBirdWeapon)   // If the sounds are already being played
            {
                /*runMultiplier = 2f; // Increase run/acceleration speed multipliers.
                accMultiplier = 1.2f; // This will last as long as you are holding the guitar.*/
                try
                {
                    if (Main.gameInactive)
                    {
                        activeBackingTrack.Stop();
                        activeSolo.Stop();
                        return;
                    }
                    // Constantly update the backing track to be positioned at the player
                    activeBackingTrack.Position = Player.position;

                    // If the volume is lower than normal (like if the player swaps between the guitar to another item back to guitar quickly)
                    // then raise the volume back to normal
                    if (activeBackingTrack.Volume < musicVolume)
                        activeBackingTrack.Volume *= 1.035f;
                    else 
                        activeBackingTrack.Volume = musicVolume;
                }
                catch
                {
                    // Do nothing
                }
            } else if (flag && !holdingFreeBirdWeapon)  // If the sounds are being played AND the player is no longer holding the guitar
            {
                /*runMultiplier = 1f; // Revert run/acceleration speed multipliers.
                accMultiplier = 1f; // Also found in the last else statement just in case*/
                try
                {
                    // Constantly update the backing track to be positioned at the player
                    activeBackingTrack.Position = Player.position;

                    // Keep lowering the volume of the backing tracks until it reaches a certain point,
                    // and once it reaches that point, stop playing both tracks.
                    // If the player swaps back to the guitar after this point, both sounds will play from the beginning.
                    activeBackingTrack.Volume *= 0.95f;
                    if (activeBackingTrack.Volume <= 0.02f)
                    {
                        activeBackingTrack.Volume = 0;
                        activeBackingTrack.Stop();
                        activeSolo.Stop();
                    }
                }
                catch 
                {
                    // Do nothing
                }

            }

            if (flag2 && usingFreeBirdWeapon)   // If the player is pressing and/or holding the use button
            {
                // Update the solo's position constantly to be at the player
                activeSolo.Position = Player.position;
                // Matches the use animation time for the guitar
                tickCounter = 15;
            }
            else if (flag2 && tickCounter > 0)    // If the tick counter was set to its max value
            {
                activeSolo.Position = Player.position;
                // Decrement the counter
                tickCounter--;

                // Give the solo a baseline volume so it can be multiplied further (since if it's 0 then it won't increase in volume if the volume value is multiplied)
                if (activeSolo.Volume <= 0)
                    activeSolo.Volume = 0.1f;

                // Quickly raise the volume of the solo and backing track if it's below the volume threshold, or hold those volumes as they are
                if (activeSolo.Volume < musicVolume)
                {
                    activeBackingTrack.Volume *= 1.5f;
                    activeSolo.Volume *= 1.5f;
                }
                else
                {
                    activeBackingTrack.Volume = musicVolume;
                    activeSolo.Volume = musicVolume;
                }
            }
            else if (flag2)   // In other words, when guitar is not being used but still held
            {
                // Update the solo's position constantly to be at the player
                activeSolo.Position = Player.position;

                // Swiftly lower the volume of the solo
                activeSolo.Volume *= 0.75f;
                if (activeSolo.Volume <= 0.02f)
                    activeSolo.Volume = 0;
            }
            /*else    // If the guitar is not being held regardless if the music is still playing, revert run/acceleration speed multipliers
            {
                runMultiplier = 1f;
                accMultiplier = 1f;
            }*/

            /*if (!holdingFreeBirdWeapon && packetsEnabled == EnablePackets.Disabled)
                packetsEnabled = EnablePackets.Waiting;
            else if (holdingFreeBirdWeapon && packetsEnabled == EnablePackets.Waiting)
                packetsEnabled = EnablePackets.Enabled;
            else if (packetsEnabled == EnablePackets.Enabled)
            {
                Main.NewText("Packets Enabled");
                packetsEnabled = EnablePackets.Disabled;
                ModPacket packet = Mod.GetPacket();
                packet.Write((byte)FreeBirdWeapon.MessageType.HoldGuitarMoveSpeedSync);
                packet.Write((byte)Player.whoAmI);
                packet.Write(runMultiplier);
                packet.Write(accMultiplier);
                packet.Write(holdingFreeBirdWeapon);
                packet.Send(255, Player.whoAmI);
            }*/

        }

        public override void PostUpdateRunSpeeds()
        {
            bool speedCheck = false;
            if (Player.HeldItem.type == ModContent.ItemType<FreeBirdGuitar>())
                speedCheck = true;
            if (speedCheck)
            {
                Player.maxRunSpeed *= 2f;
                Player.accRunSpeed *= 2f;
                Player.runAcceleration *= 1.2f;
            }
        }

        /*public void SyncFreeBird(int toWho, int fromWho, bool newPlayer)
        {
            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)FreeBirdWeapon.MessageType.HoldGuitarMoveSpeedSync);
            packet.Write((byte)Player.whoAmI);
            packet.Write(runMultiplier);
            packet.Write(accMultiplier);
            packet.Write(holdingFreeBirdWeapon);
            packet.Send(toWho, fromWho);
        }

        public void ReceivePlayerSync(BinaryReader reader)
        {
            runMultiplier = reader.ReadSingle();
            accMultiplier = reader.ReadSingle();
            holdingFreeBirdWeapon = reader.ReadBoolean();
        }

        public override void CopyClientState(ModPlayer targetCopy)
        {
            FreeBirdPlayer clone = (FreeBirdPlayer)targetCopy;
            clone.runMultiplier = runMultiplier;
            clone.accMultiplier = accMultiplier;
            clone.holdingFreeBirdWeapon = holdingFreeBirdWeapon;
        }

        public override void SendClientChanges(ModPlayer clientPlayer)
        {
            FreeBirdPlayer clone = (FreeBirdPlayer)clientPlayer;

            if (runMultiplier != clone.runMultiplier || accMultiplier != clone.accMultiplier || holdingFreeBirdWeapon != clone.holdingFreeBirdWeapon)
                SyncPlayer(toWho: -1, fromWho: Main.myPlayer, newPlayer: false);
        }*/

        public override void ResetEffects()
        {
            // If the player is not holding the weapon, reset the bool values back to false
            holdingFreeBirdWeapon = false;
            usingFreeBirdWeapon = false;
        }
    }
}
