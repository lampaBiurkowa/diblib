using DibBase.ModelBase;
using System.Security.Cryptography;
using System.Text;

namespace DibBase.Obfuscation;

public static class IdObfuscator
{
    readonly static Dictionary<long, string> entitiesHash = new();

    static IdObfuscator()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            try
            {
                var derivedTypes = assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Entity)));
                foreach (var type in derivedTypes)
                    entitiesHash.Add(BitConverter.ToInt64(ComputeHash(type.Name), 0), type.Name);
            }
            catch { } //ignore invalid assemblies
        }
    }


    public static Guid ObfuscateId(long number, string text)
    {
        byte[] numberBytes = BitConverter.GetBytes(number);
        byte[] textBytes = ComputeHash(text);
        byte[] guidBytes = new byte[Guid.Empty.ToByteArray().Length];
        Array.Copy(textBytes, 0, guidBytes, 0, textBytes.Length);
        Array.Copy(numberBytes, 0, guidBytes, textBytes.Length, numberBytes.Length);

        return new Guid(guidBytes);
    }

    public static ObfuscatedFields DecodeId(Guid obfuscatedGuid)
    {
        byte[] guidBytes = obfuscatedGuid.ToByteArray();

        long number = BitConverter.ToInt64(guidBytes, 8);
        string text = entitiesHash[BitConverter.ToInt64(guidBytes[0..8], 0)];

        return new() { Id = number, EntityType = text };
    }

    static byte[] ComputeHash(string input)
    {
        byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(input));
        byte[] truncatedHash = new byte[8];
        Array.Copy(hash, truncatedHash, 8);
        return truncatedHash;
    }
}
