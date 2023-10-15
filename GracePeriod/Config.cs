namespace GracePeriod
{
    public class Config
    {
        public string text_color = "";
        public string timer_color = "";
        public string announcement_color = "_";
        public string announcement_text = "";

        public static Config DefaultConfig()
        {
            Config vConf = new Config
            {
                text_color = "FFFFFF",
                timer_color = "8eec8e",
                announcement_color = "FF0000",
                announcement_text = "The grace period has ended.\nYou are now at the mercy of yourself and others.\nYou are being forced into PvP.",
            };

            return vConf;
        }
    }
}
