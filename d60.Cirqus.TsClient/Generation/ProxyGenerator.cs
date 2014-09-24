using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using d60.Cirqus.TsClient.Model;

namespace d60.Cirqus.TsClient.Generation
{
    class ProxyGenerator
    {
        readonly IWriter _writer;
        readonly List<string> _sourceDlls = new List<string>();

        public ProxyGenerator(string sourceDll, IWriter writer)
        {
            _writer = writer;
            _sourceDlls.Add(sourceDll);
        }

        public List<ProxyGenerationResult> Generate()
        {
            return GetProxyGenerationResults().ToList();
        }

        IEnumerable<ProxyGenerationResult> GetProxyGenerationResults()
        {
            var commandTypes = _sourceDlls
                .Select(LoadAssembly)
                .SelectMany(GetTypes)
                .Where(ProxyGeneratorContext.IsCommand)
                .ToList();

            _writer.Print("Found {0} command types", commandTypes.Count);

            var commandsFileName = string.Format("commands.ts");
            var commandProcessorFileName = string.Format("commandProcessor.ts");

            var context = new ProxyGeneratorContext(commandTypes);
            var code = context.GetCommandDefinitations();

            yield return new ProxyGenerationResult(commandsFileName, _writer, code);

            var moreCode = context.GetCommandProcessorDefinitation();

            yield return new ProxyGenerationResult(commandProcessorFileName, _writer, moreCode);
        }

        Type[] GetTypes(Assembly assembly)
        {
            try
            {
                var types = assembly.GetTypes();

                return types;
            }
            catch (ReflectionTypeLoadException exception)
            {
                var loaderExceptions = exception.LoaderExceptions;

                var message = string.Format(@"Could not load types from {0} - got the following loader exceptions: {1}", assembly, string.Join(Environment.NewLine, loaderExceptions.Select(e => e.ToString())));

                throw new ApplicationException(message);
            }
        }

        Assembly LoadAssembly(string filePath)
        {
            try
            {
                _writer.Print("Loading DLL {0}", filePath);
                return Assembly.LoadFile(Path.GetFullPath(filePath));
            }
            catch (BadImageFormatException exception)
            {
                throw new BadImageFormatException(string.Format("Could not load {0}", filePath), exception);
            }
        }
    }
}