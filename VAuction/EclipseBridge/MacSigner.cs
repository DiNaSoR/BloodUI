using System;
using System.Security.Cryptography;
using System.Text;

namespace VAuction.EclipseBridge;

internal static class MacSigner
{
    public static string AppendMac(string message, byte[] key)
    {
        if (string.IsNullOrEmpty(message)) return string.Empty;
        if (key == null || key.Length == 0) return string.Empty;

        string mac = GenerateMac(message, key);
        return string.IsNullOrEmpty(mac) ? string.Empty : $"{message};mac{mac}";
    }

    public static string GenerateMac(string message, byte[] key)
    {
        using var hmac = new HMACSHA256(key);
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
        byte[] hashBytes = hmac.ComputeHash(messageBytes);
        return Convert.ToBase64String(hashBytes);
    }
}

