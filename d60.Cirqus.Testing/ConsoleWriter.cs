using System;

namespace d60.Cirqus.Testing
{
    public class ConsoleWriter : IWriter 
    {
        public void Write(string text)
        {
            Console.Write(text);
        }
    }
}