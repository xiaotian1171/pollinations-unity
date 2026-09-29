using System;
using System.Threading.Tasks;

namespace Pollinations.Tests
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            bool live = Array.IndexOf(args, "--live") >= 0;

            Console.WriteLine("Pollinations core tests");
            Console.WriteLine();

            JsonTests.Run();
            UrlTests.Run();
            ErrorTests.Run();
            ModelTests.Run();
            ConfigTests.Run();
            ClientTests.Run();
            ChatTests.Run();
            DeviceFlowTests.Run();
            WavTests.Run();
            TransportTests.Run();

            int exit = Check.Report();

            if (live)
            {
                int liveExit = LiveCheck.RunAsync().GetAwaiter().GetResult();
                if (liveExit != 0)
                {
                    exit = liveExit;
                }
            }

            return exit;
        }
    }
}
