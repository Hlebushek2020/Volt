using System;

namespace VoltBot.Logs
{
    internal static class SynchronizedConsole
    {
        private static object _consoleLocker = new object();

        public static void WriteLine(string value)
        {
            lock (_consoleLocker)
            {
                Console.WriteLine(value);
            }
        }

        public static void WriteLine(string value, ConsoleColor color)
        {
            lock (_consoleLocker)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(value);
                Console.ResetColor();
            }
        }
    }
}