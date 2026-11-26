using System;

namespace OLE.Examples
{
    internal static class PlayerProgram
    {
        private static int Main(string[] args)
        {
            bool challenge = args.Length == 1 && args[0] == "--challenge";
            if (args.Length > 0 && !challenge)
            {
                Console.Error.WriteLine("Usage: TutorialConsole [--challenge]");
                return 2;
            }

            return ConsoleGame.Run(TutorialCampaign.CreateSession(), challenge);
        }
    }
}
