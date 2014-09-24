using System;
using System.IO;
using System.Linq;
using d60.Cirqus.TsClient.Generation;

namespace d60.Cirqus.TsClient
{
    class Program
    {
        static readonly ConsoleWriter Writer = new ConsoleWriter();

        static int Main(string [] args)
        {
            try
            {
                Run(args);

                return 0;
            }
            catch (Exception exception)
            {
                Writer.Print("Unhandled exception: {0}", exception);

                return 1;
            }
        }

        static void Run(string[] args)
        {
            if (args.Length != 2)
            {
                throw new ArgumentException(string.Format("Invalid number of args: {0}", args.Length));
            }

            var sourceDll = args[0];
            var destinationDirectory = args[1];

            if (!File.Exists(sourceDll))
            {
                throw new FileNotFoundException(string.Format("Could not find source DLL {0}", sourceDll));
            }

            if (!Directory.Exists(destinationDirectory))
            {
                Writer.Print("Creating directory {0}", destinationDirectory);
                Directory.CreateDirectory(destinationDirectory);
            }

            var proxyGenerator = new ProxyGenerator(sourceDll, Writer);
            
            var results = proxyGenerator.Generate().ToList();

            Writer.Print("Writing files");
            foreach (var result in results)
            {
                result.WriteTo(destinationDirectory);
            }
        }
    }
}
