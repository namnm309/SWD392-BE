using System;
using System.Text;

namespace InfrastructureLayer.Core.Redis;

public static class RedisCacheDefaults
{
    public static readonly TimeSpan DefaultTtl = TimeSpan.FromSeconds(30);
}

public static class RedisCacheKeyBuilder
{
    public static string Build(string prefix, params (string Name, object? Value)[] segments)
    {
        var sb = new StringBuilder(prefix);
        foreach (var (name, value) in segments)
        {
            if (value is null) continue;
            var formatted = FormatValue(value);
            if (string.IsNullOrEmpty(formatted)) continue;
            sb.Append('|').Append(name).Append(':').Append(formatted);
        }
        return sb.ToString();
    }

    private static string? FormatValue(object value)
    {
        return value switch
        {
            DateTime dt => dt.ToUniversalTime().ToString("O"),
            DateTimeOffset dto => dto.ToUniversalTime().ToString("O"),
            Guid guid => guid.ToString(),
            Enum enumValue => Convert.ToInt32(enumValue).ToString(),
            bool boolean => boolean ? "1" : "0",
            _ => value.ToString()
        };
    }
}

