using System;
using System.Security.Cryptography;

namespace VAuction.Domain;

internal static class AuctionId
{
    public static ulong Create(long unixSeconds)
    {
        int rnd = RandomNumberGenerator.GetInt32(0, 65536);
        return ((ulong)unixSeconds << 16) | (uint)rnd;
    }

    public static string ToKey(ulong id)
    {
        const string alphabet = "0123456789abcdefghijklmnopqrstuvwxyz";
        if (id == 0) return "0";

        Span<char> buffer = stackalloc char[13]; // enough for base36 of 64-bit
        int pos = buffer.Length;

        ulong value = id;
        while (value > 0)
        {
            ulong rem = value % 36;
            buffer[--pos] = alphabet[(int)rem];
            value /= 36;
        }

        return new string(buffer[pos..]);
    }

    public static bool TryParseKey(string key, out ulong id)
    {
        id = 0;
        if (string.IsNullOrWhiteSpace(key)) return false;

        string s = key.Trim().ToLowerInvariant();
        foreach (char c in s)
        {
            int v = c switch
            {
                >= '0' and <= '9' => c - '0',
                >= 'a' and <= 'z' => 10 + (c - 'a'),
                _ => -1
            };

            if (v < 0) return false;
            checked
            {
                id = (id * 36) + (ulong)v;
            }
        }

        return true;
    }
}

