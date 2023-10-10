using System.Timers;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using System.Text;
using Newtonsoft.Json;


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

        public override Version Version => new Version(1, 0, 0, 0);

        public GracePeriod(Main game) : base(game)
        {

        }

        private static System.Timers.Timer Timer = new System.Timers.Timer();
        private static System.Timers.Timer OneSecTimer = new System.Timers.Timer();
        private static DateTime timer_start;
        private static DateTime timer_end;

        private static Config config;
        internal static string filepath { get { return Path.Combine(TShock.SavePath, "Grace.json"); } }


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

            ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);

            GetDataHandlers.TogglePvp += OnTogglePvp;

            Commands.ChatCommands.Add(new Command("tshock.grace", grace, "grace"));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameUpdate.Deregister(this, OnGameUpdate);

                GetDataHandlers.TogglePvp -= OnTogglePvp;
            }
            base.Dispose(disposing);
        }


        private void OnGameUpdate(EventArgs args)
        {
            foreach (TSPlayer tSPlayer in TShock.Players)
            {
                if (tSPlayer == null) continue;
                if (tSPlayer.TPlayer.hostile != PvPForcedOn)
                {
                    SetPvP(tSPlayer, PvPForcedOn);
                    tSPlayer.SendInfoMessage("Your PvP was {0}abled!", PvPForcedOn ? "en" : "dis");
                }
            }
        }

        private void grace(CommandArgs args)
        {
            TSPlayer tSPlayer = args.Player;
            if (args.Parameters.Count == 0)
            {
                tSPlayer.SendErrorMessage("Invalid syntax! Check {0}grace help to see available commands", Commands.Specifier);
                return;
            }
            switch (args.Parameters[0])
            {
                case "start":
                    if (args.Parameters.Count == 2)
                    {
                        if (int.TryParse("123", out _))
                        {
                            graceStart(int.Parse(args.Parameters[1]));
                        }
                        else
                        {
                            tSPlayer.SendErrorMessage("Invalid syntax! Proper syntax: {0}grace start <time in seconds>", Commands.Specifier);
                        }

                    }
                    else
                    {
                        tSPlayer.SendErrorMessage("Invalid syntax! Proper syntax: {0}grace start <time in seconds>", Commands.Specifier);
                    }
                    break;
                case "stop":
                    graceStop();
                    break;
                case "help":
                    graceHelp(args.Player);
                    break;

            }
        }

        private void graceStart(int sec)
        {
            Timer.Interval = sec * 1000;
            Timer.Enabled = true;
            Timer.Elapsed += new ElapsedEventHandler(graceStop);
            OneSecTimer.Interval = 1000;
            OneSecTimer.Enabled = true;
            OneSecTimer.Elapsed += new ElapsedEventHandler(OnOneSec);
            timer_start = DateTime.Now;
            timer_end = timer_start.AddMilliseconds(Timer.Interval);
            PvPForcedOn = false;

        }

        private void graceStop()
        {
            Timer.Enabled = false;
            OneSecTimer.Enabled = false;
            PvPForcedOn = true;
            ForcePvP();
            TShock.Utils.Broadcast("Grace period is over! PvP is on!", Microsoft.Xna.Framework.Color.DarkRed);
            foreach (TSPlayer tSPlayer in TShock.Players)
            {
                if (tSPlayer != null)
                {
                    tSPlayer.SendData(PacketTypes.Status, RepeatLineBreaks(60));
                }
            }
        }

        private void graceHelp(TSPlayer player)
        {
            player.SendInfoMessage("/grace help - see this message");
            player.SendInfoMessage("/grace start <timeInSeconds> - start the grace period, set it to -1 to make it infinite");
            player.SendInfoMessage("/grace stop - immediately stop the grace period and force PvP on");
        }

        internal static void graceStop(object sender, ElapsedEventArgs args)
        {
            Timer.Enabled = false;
            OneSecTimer.Enabled = false;
            PvPForcedOn = true;
            ForcePvP();
            TShock.Utils.Broadcast("Grace period is over! PvP is on!", Microsoft.Xna.Framework.Color.DarkRed);
            foreach (TSPlayer tSPlayer in TShock.Players)
            {
                if (tSPlayer != null)
                {
                    tSPlayer.SendData(PacketTypes.Status, RepeatLineBreaks(60));
                }

            }
        }
        internal static void OnOneSec(object sender, ElapsedEventArgs args)
        {
            int TimeLeft = (int)(timer_end - DateTime.Now).TotalSeconds;
            int hours = (TimeLeft / 3600);
            TimeLeft %= 3600;
            int minutes = (TimeLeft / 60);
            int seconds = (TimeLeft % 60);
            string time_left = hours.ToString() + "h " + minutes.ToString() + "m " + seconds.ToString() + "s ";
            foreach (TSPlayer tSPlayer in TShock.Players)
            {
                tSPlayer.SendData(PacketTypes.Status, RepeatLineBreaks(12) + "[c/" + config.text_color + ":Grace period ends: ]\n" + "[c/" + config.timer_color + ":" + time_left + "]" + RepeatLineBreaks(50));
            }

        }
        private void OnTogglePvp(object sender, GetDataHandlers.TogglePvpEventArgs args)
        {
            if (args.Pvp == PvPForcedOn) return;

            // This is necessary so the pvp toggle message doesn't appear in chat.
            TSPlayer tSPlayer = args.Player;
            SetPvP(tSPlayer, PvPForcedOn);
            tSPlayer.SendErrorMessage("You're not allowed to toggle PvP!");
            args.Handled = true;
        }


        public static void ForcePvP()
        {
            foreach (TSPlayer tSPlayer in TShock.Players)
            {
                if (tSPlayer == null && PvPForcedOn) continue;
                SetPvP(tSPlayer, PvPForcedOn);
            }
        }

        private static void SetPvP(TSPlayer tSPlayer, bool mode)
        {
            tSPlayer.SetPvP(mode);
            tSPlayer.SendData(PacketTypes.TogglePvp, "", tSPlayer.Index, mode.ToInt());
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
