using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace SafeInCloudReader
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("wong number of argument. filePath and password are required.");
                return 1;
            }

            try
            {
                string path = args[0];
                string password = args[1];

                if (!File.Exists(path))
                {
                    Console.Error.WriteLine("file not found.");
                    return 1;
                }

                using (var inputStream = DatabaseReader.Read(new FileStream(path, FileMode.Open, FileAccess.Read), password))
                using (var textReader = new StreamReader(inputStream, Encoding.UTF8))
                {
                    var document = new XmlDocument();
                    document.LoadXml(textReader.ReadToEnd());
                    var writer = new XmlTextWriter(Console.Out);
                    writer.Formatting = Formatting.Indented;
                    document.WriteContentTo(writer);
                    return 0;
                }
            }
            catch (IOException)
            {
                Console.Error.WriteLine("can not read file.");
            }
            catch (ArgumentException)
            {
                Console.Error.WriteLine("wrong password.");
            }

            return 1;
        }
    }
}