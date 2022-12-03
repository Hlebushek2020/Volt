namespace VoltBot
{
    internal class Program
    {
        static int Main(string[] args)
        {
            if (!Settings.Settings.Availability())
            {
                return 0;
            }

            //try
            //{
            using (Bot yukoBot = Bot.Current)
            {
                yukoBot.RunAsync().GetAwaiter().GetResult();
            }
            //}
            //catch (Exception ex)
            //{
            return 1;
            //}

            return 0;
        }
    }
}