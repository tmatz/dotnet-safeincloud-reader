using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace SafeInCloudReader
{
    class Program
    {
        static int Main(string[] args)
        {
            string filePath = null;
            byte[] password = null;

            try
            {
                switch (args.Length)
                {
                    case 1:
                        filePath = args[0];
                        password = GetPasswordFromConsole("Password:");
                        break;
                    case 2:
                        filePath = args[0];
                        password = args[1].Select(c => (byte)c).ToArray();
                        break;
                    default:
                        Console.Error.WriteLine("Usage: SafeInCloudReader <filePath> [password]");
                        return 1;
                }

                if (!File.Exists(filePath))
                {
                    Console.Error.WriteLine("file not found.");
                    return 1;
                }

                using (var inputStream = DatabaseReader.Read(new FileStream(filePath, FileMode.Open, FileAccess.Read), password))
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

        private static byte[] GetPasswordFromConsole(string message)
        {
            List<byte> password = new();
            Console.Write(message);
            while (true)
            {
                var key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        return password.ToArray();
                    case ConsoleKey.Backspace:
                        if (password.Count == 0) { break; }
                        password.RemoveAt(password.Count - 1);
                        break;
                    default:
                        if (key.KeyChar == 0) { break; }
                        password.Add((byte)key.KeyChar);
                        break;
                }
            }
        }
    }
}