using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Ionic.Zlib;

namespace SafeInCloudReader
{
    public class DatabaseReader
    {
        private const int KEY_LENGTH = 256;

        private const int MAGIC = 1285;

        private class Header
        {
            public short Magic;

            public byte Version;

            public byte[] Salt;

            public byte[] Iv;

            public byte[] SecretSalt;

            public byte[] Secrets;
        }

        public static Stream Read(Stream inputStream, string password)
        {
            if (inputStream == null)
            {
                throw new ArgumentNullException(nameof(inputStream));
            }

            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            (byte[] secretKey, byte[] secretIv) = GetSecrets(inputStream, password);

            using (var algorithm = GetAlgorithm(secretKey, secretIv))
            {
                Stream stream = new CryptoStream(inputStream, algorithm.CreateDecryptor(), CryptoStreamMode.Read);
                stream = new BufferedStream(stream);
                stream = new ZlibStream(stream, CompressionMode.Decompress);
                return stream;
            }
        }

        private static Header ReadHeader(BinaryReader input)
        {
            var header = new Header();

            header.Magic = input.ReadInt16();
            if (header.Magic != MAGIC)
            {
                throw new FormatException("unexpected magic value");
            }

            header.Version = input.ReadByte();
            if (header.Version != 1)
            {
                throw new FormatException("unexpected sver value");
            }

            header.Salt = ReadByteArray(input);
            header.Iv = ReadByteArray(input);
            header.SecretSalt = ReadByteArray(input);
            header.Secrets = ReadByteArray(input);

            return header;
        }

        private static (byte[] Key, byte[] Iv) GetSecrets(Stream inputStream, string password)
        {
            using (var binaryReader = new BinaryReader(inputStream, Encoding.ASCII, leaveOpen: true))
            {
                Header header = ReadHeader(binaryReader);

                byte[] passwordBytes = password.Select(x => (byte)x).ToArray();
                byte[] key = GetKey(passwordBytes, header.Salt, 10000);

                using (var algorithm = GetAlgorithm(key, header.Iv))
                using (var decryptor = algorithm.CreateDecryptor())
                {
                    byte[] secrets = decryptor.TransformFinalBlock(header.Secrets, 0, header.Secrets.Length);
                    using (var secretsReader = new BinaryReader(new MemoryStream(secrets)))
                    {
                        try
                        {
                            byte[] secretIv = ReadByteArray(secretsReader);
                            byte[] secretKey = ReadByteArray(secretsReader);
                            byte[] checkSum = ReadByteArray(secretsReader);
                            byte[] calculatedCheckSum = GetKey(secretKey, header.SecretSalt, 1000);

                            if (!checkSum.SequenceEqual(calculatedCheckSum))
                            {
                                throw new ArgumentException("wrong password");
                            }

                            return (secretKey, secretIv);
                        }
                        catch (EndOfStreamException)
                        {
                            throw new ArgumentException("wrong password");
                        }
                    }
                }
            }
        }

        private static SymmetricAlgorithm GetAlgorithm(byte[] key, byte[] iv)
        {
            var aes = Aes.Create("AesManaged");
            aes.KeySize = KEY_LENGTH;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;
            aes.Key = key;
            aes.IV = iv;
            return aes;
        }

        private static byte[] GetKey(byte[] password, byte[] salt, int iterationCount)
        {
            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, iterationCount))
            {
                return deriveBytes.GetBytes(KEY_LENGTH / 8);
            }
        }

        private static byte[] ReadByteArray(BinaryReader reader)
        {
            int length = reader.ReadByte();
            return reader.ReadBytes(length);
        }
    }
}