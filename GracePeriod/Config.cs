namespace GracePeriod
{
    public class Config
    {
        public string text_color = "";
        public string timer_color = "";

        public static Config DefaultConfig()
        {
            Config vConf = new Config
            {
                text_color = "FFFFFF",
                timer_color = "98ce91",
            };

            return vConf;
        }
    }
}
