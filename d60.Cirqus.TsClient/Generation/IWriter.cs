using System;

namespace d60.Cirqus.TsClient.Generation
{
    interface IWriter
    {
        void Print(string message, params object[] objs);
    }

    class ConsoleWriter : IWriter
    {
        public void Print(string message, params object[] objs)
        {
            Console.WriteLine(message, objs);
        }
    }
}