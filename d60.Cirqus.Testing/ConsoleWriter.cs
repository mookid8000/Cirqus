using System;

namespace d60.Cirqus.Testing
{
    public class ConsoleWriter : IWriter 
    {
        public void WriteLine(string text)
        {
            Console.WriteLine(text);
        }
    }
}