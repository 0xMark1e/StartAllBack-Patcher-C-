using System;

namespace SynezSAB
{
    internal class ConsoleLog
    {
        public void Info(string msg)    => Write(msg, ConsoleColor.Gray,   newline: false);
        public void Done(string msg)    => Write(msg, ConsoleColor.Green,  newline: true);
        public void Warning(string msg) => Write(msg, ConsoleColor.Yellow, newline: true);
        public void Error(string msg)   => Write(msg, ConsoleColor.Red,    newline: true);
        public void Colored(string msg, string _) => Write(msg, ConsoleColor.Cyan, newline: false);

        public void Banner(string msg)
        {
            string line = msg.PadLeft((42 + msg.Length) / 2, '-').PadRight(42, '-');
            Write(line, ConsoleColor.White, newline: true);
        }

        private static void Write(string text, ConsoleColor color, bool newline)
        {
            ConsoleColor prev = Console.ForegroundColor;
            Console.ForegroundColor = color;
            if (newline) Console.WriteLine(text);
            else         Console.Write(text);
            Console.ForegroundColor = prev;
        }
    }
}
