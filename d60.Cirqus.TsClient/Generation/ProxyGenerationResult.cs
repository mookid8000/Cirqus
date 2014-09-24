using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace d60.Cirqus.TsClient.Generation
{
    class ProxyGenerationResult
    {
        static readonly Encoding Encoding = Encoding.UTF8;

        readonly string _filename;
        readonly IWriter _writer;
        readonly string _code;

        public ProxyGenerationResult(string filename, IWriter writer, string code)
        {
            _filename = filename;
            _writer = writer;
            _code = code;
        }

        public void WriteTo(string destinationDirectory)
        {
            var destinationFilePath = Path.Combine(destinationDirectory, _filename);

            if (File.Exists(destinationFilePath) && !HasChanged(destinationFilePath))
            {
                _writer.Print("    No changes - skipping {0}", destinationFilePath);
                return;
            }

            _writer.Print("    Writing {0}", destinationFilePath);
            var header = string.Format(HeaderTemplate, HashPrefix, GetHash());
            File.WriteAllText(destinationFilePath, header + Environment.NewLine + Environment.NewLine + _code, Encoding);
        }

        string GetHash()
        {
            return Convert.ToBase64String(MD5.Create().ComputeHash(Encoding.GetBytes(_code)));
        }

        bool HasChanged(string destinationFilePath)
        {
            return GetHash() != GetHashFromFile(destinationFilePath);
        }

        string GetHashFromFile(string destinationFilePath)
        {
            using (var file = File.OpenRead(destinationFilePath))
            using (var reader = new StreamReader(file, Encoding))
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    var trimmedLine = line.TrimStart();
                    
                    if (string.IsNullOrWhiteSpace(trimmedLine)) break;

                    if (!trimmedLine.StartsWith(HashPrefix)) continue;
                    
                    var hash = trimmedLine.Substring(HashPrefix.Length);

                    return hash;
                }
            }

            return "";
        }

        const string HeaderTemplate = @"/* 
    Generated with d60.Cirqus.TsClient.... should probably not be edited directly, should probably be regenerated instead... :)
    {0}{1}
*/";
        const string HashPrefix = "Hash: ";
    }
}