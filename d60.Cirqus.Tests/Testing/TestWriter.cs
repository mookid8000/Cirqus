using System;
using d60.Cirqus.Testing;

namespace d60.Cirqus.Tests.Testing
{
    class TestWriter : IWriter 
    {
        public TestWriter()
        {
            Buffer = "";
        }

        public string Buffer { get; private set; }

        public void WriteLine(string text)
        {
            Buffer += text + "\r\n";
            Console.WriteLine(text);
        }
    }
}