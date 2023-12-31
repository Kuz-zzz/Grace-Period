﻿using System.Timers;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using System.Text;
using Newtonsoft.Json;
using System.Globalization;
using System.IO;

namespace GracePeriod
{
    [ApiVersion(2, 1)]
    public class GracePeriod : TerrariaPlugin
    {
        public static bool PvPForcedOn = true;
        public bool GraceOn = false;


        public override string Author => "Kuz_";

        public override string Description => "Adds grace period for PvP gamemodes";

        public override string Name => "Grace Period";

        public override Version Version => new Version(1, 2, 0, 0);

        public GracePeriod(Main game) : base(game)
        {

        }

        private static System.Timers.Timer Timer = new System.Timers.Timer();
        private static System.Timers.Timer OneSecTimer = new System.Timers.Timer();
        private static DateTime timer_start;
        private static DateTime timer_end;

        private static Config config;
        internal static string filepath { get { return Path.Combine(TShock.SavePath, "grace.json"); } }


        private static void ReadConfig<TConfig>(string path, TConfig defaultConfig, out TConfig config)
        {
            if (!File.Exists(path))
            {
                config = defaultConfig;
                File.WriteAllText(path, JsonConvert.SerializeObject(config, Formatting.Indented));
            }
            else
            {
                config = JsonConvert.DeserializeObject<TConfig>(File.ReadAllText(path));
                
            }
        }
        public override void Initialize()
        {
            ReadConfig(filepath, Config.DefaultConfig(), out config);
            if (config.announcement_color == "_")
            {
                config = Config.DefaultConfig();
                File.WriteAllText(filepath, JsonConvert.SerializeObject(config, Formatting.Indented));
            }
            ReadConfig(filepath, Config.DefaultConfig(), out config);

            ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);

            GetDataHandlers.TogglePvp += OnTogglePvp;

            Commands.ChatCommands.Add(new Command("tshock.grace", Grace, "grace"));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameUpdate.Deregister(this, OnGameUpdate);

                GetDataHandlers.TogglePvp -= OnTogglePvp;

                Timer.Dispose();
                OneSecTimer.Dispose();
            }
            base.Dispose(disposing);
        }


        private void OnGameUpdate(EventArgs args)
        {
            foreach (TSPlayer player in TShock.Players)
            {
                if (player == null) continue;
                if (player.TPlayer.hostile != PvPForcedOn)
                {
                    SetPvP(player, PvPForcedOn);
                    player.SendInfoMessage("Your PvP was {0}abled!", PvPForcedOn ? "en" : "dis");
                }
            }
        }

        private void Grace(CommandArgs args)
        {
            TSPlayer player = args.Player;
            if (args.Parameters.Count == 0)
            {
                player.SendErrorMessage("Invalid syntax! Check {0}grace help to see available commands", Commands.Specifier);
                return;
            }
            switch (args.Parameters[0])
            {
                case "start":
                    if (args.Parameters.Count == 2)
                    {
                        if (int.TryParse("123", out _))
                        {
                            GraceStart(int.Parse(args.Parameters[1]));
                        }
                        else
                        {
                            player.SendErrorMessage("Invalid syntax! Proper syntax: {0}grace start <time in seconds>", Commands.Specifier);
                        }

                    }
                    else
                    {
                        player.SendErrorMessage("Invalid syntax! Proper syntax: {0}grace start <time in seconds>", Commands.Specifier);
                    }
                    break;
                case "stop":
                    GraceStop();
                    break;
                case "help":
                    GraceHelp(args.Player);
                    break;

            }
        }

        private void GraceStart(int sec)
        {
            if (Timer.Enabled)
            {
                Timer.Enabled = false;
                OneSecTimer.Enabled = false;
                Timer.Elapsed -= new ElapsedEventHandler(GraceStop);
                OneSecTimer.Elapsed -= new ElapsedEventHandler(OnOneSec);
            }
            Timer.Interval = sec * 1000;
            Timer.Enabled = true;
            Timer.Elapsed += new ElapsedEventHandler(GraceStop);
            OneSecTimer.Interval = 1000;
            OneSecTimer.Enabled = true;
            OneSecTimer.Elapsed += new ElapsedEventHandler(OnOneSec);
            timer_start = DateTime.Now;
            timer_end = timer_start.AddMilliseconds(Timer.Interval);
            PvPForcedOn = false;

        }

        private void GraceStop()
        {
            if (Timer.Enabled)
            {
                Timer.Enabled = false;
                OneSecTimer.Enabled = false;
                Timer.Elapsed -= new ElapsedEventHandler(GraceStop);
                OneSecTimer.Elapsed -= new ElapsedEventHandler(OnOneSec);
            }
            PvPForcedOn = true;
            ForcePvP();
            TShock.Utils.Broadcast(config.announcement_text, Microsoft.Xna.Framework.Color.Red);
            foreach (TSPlayer player in TShock.Players)
            {
                if (player != null)
                {
                    player.SendData(PacketTypes.Status, number2:1);
                }
            }
        }

        private void GraceHelp(TSPlayer player)
        {
            player.SendInfoMessage("/grace help - see this message");
            player.SendInfoMessage("/grace start <timeInSeconds> - start the grace period, set it to -1 to make it infinite");
            player.SendInfoMessage("/grace stop - immediately stop the grace period and force PvP on");
        }

        internal static void GraceStop(object sender, ElapsedEventArgs args)
        {
            Timer.Enabled = false;
            OneSecTimer.Enabled = false;
            PvPForcedOn = true;
            ForcePvP();
            foreach (TSPlayer player in TShock.Players)
            {
                if (player != null)
                {
                    player.SendData(PacketTypes.Status, number2: 1);
                }

            }

            int r = int.Parse(config.announcement_color.Substring(0, 2), NumberStyles.AllowHexSpecifier); 
            int g = int.Parse(config.announcement_color.Substring(2, 2), NumberStyles.AllowHexSpecifier);
            int b = int.Parse(config.announcement_color.Substring(4, 2), NumberStyles.AllowHexSpecifier);

            TShock.Utils.Broadcast(config.announcement_text, (byte)r, (byte)g, (byte)b);
            
        }
        internal static void OnOneSec(object sender, ElapsedEventArgs args)
        {
            int TimeLeft = (int)(timer_end - DateTime.Now).TotalSeconds;
            int hours = (TimeLeft / 3600);
            TimeLeft %= 3600;
            int minutes = (TimeLeft / 60);
            int seconds = (TimeLeft % 60);
            string time_left = "";
            if (hours != 0)
            {
                time_left = hours.ToString() + "h " + minutes.ToString() + "m " + seconds.ToString() + "s ";

            } else if (minutes != 0)
            {
                time_left = minutes.ToString() + "m " + seconds.ToString() + "s ";

            } else
            {
                time_left = seconds.ToString() + "s ";
            }
            foreach (TSPlayer player in TShock.Players)
            {
                player.SendData(PacketTypes.Status, RepeatLineBreaks(12) + "[c/" + config.text_color + ":Grace period ends: ]\n" + "[c/" + config.timer_color + ":" + time_left + "]", number2:1);
            }

        }
        private void OnTogglePvp(object sender, GetDataHandlers.TogglePvpEventArgs args)
        {
            if (args.Pvp == PvPForcedOn) return;

            // This is necessary so the pvp toggle message doesn't appear in chat.
            TSPlayer player = args.Player;
            SetPvP(player, PvPForcedOn);
            player.SendErrorMessage("You're not allowed to toggle PvP!");
            args.Handled = true;
        }


        public static void ForcePvP()
        {
            foreach (TSPlayer player in TShock.Players)
            {
                if (player == null && PvPForcedOn) continue;
                SetPvP(player, PvPForcedOn);
            }
        }

        private static void SetPvP(TSPlayer player, bool mode)
        {
            player.SetPvP(mode);
            player.SendData(PacketTypes.TogglePvp, "", player.Index, mode.ToInt());
        }

        public static string RepeatLineBreaks(int number)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < number; i++)
            {
                sb.Append("\r\n");
            }

            return sb.ToString();
        }

       
    }
}
