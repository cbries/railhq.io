using libEsuEcos;
using libEsuEcos.Commands;

namespace railyEsuEcos
{
    internal static class Globals
    {
        public static string VersionS = "0.0.1";
        public static string Name = "ecos";
        public static string DisplayName = "ESU ECoS Driver";
        public static string Copyright = "(c) 2025 Dr. Christian Benjamin Ries";
        public static string Description = "Driver to communicate with ESU's ECoS 50210/50200";

        public const string CommandLineTermination = "\r\n";

        public static ICommand CommandGetInfo;
        public static ICommand CommandGetStatus;

        static Globals()
        {
            CommandGetInfo = new Get();
            CommandGetInfo.Arguments.Add(new CommandArgument { Name = "1" });
            CommandGetInfo.Arguments.Add(new CommandArgument { Name = "info" });

            CommandGetStatus = new Get();
            CommandGetStatus.Arguments.Add(new CommandArgument { Name = "1" });
            CommandGetStatus.Arguments.Add(new CommandArgument { Name = "status" });
        }
    }
}
