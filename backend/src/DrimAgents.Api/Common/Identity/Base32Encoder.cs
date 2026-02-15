using SimpleBase;

namespace DrimAgents.Api.Common.Identity;

public static class Base32Encoder
{
    public static string Encode(long id)
    {
        var bytes = BitConverter.GetBytes(id);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        return Base32.Crockford.Encode(bytes, padding: false).ToLowerInvariant();
    }

    public static long Decode(string encoded)
    {
        var bytes = Base32.Crockford.Decode(encoded.ToUpperInvariant());
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        return BitConverter.ToInt64(bytes, 0);
    }

    public static bool TryDecode(string encoded, out long id)
    {
        try
        {
            id = Decode(encoded);
            return true;
        }
        catch
        {
            id = 0;
            return false;
        }
    }
}
