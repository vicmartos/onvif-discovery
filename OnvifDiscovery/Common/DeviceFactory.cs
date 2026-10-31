using System.Net;
using OnvifDiscovery.Models;

namespace OnvifDiscovery.Common;

internal static class DeviceFactory
{
    internal static DiscoveryDevice CreateDevice(ProbeMatch probeMatch, IPEndPoint remoteEndpoint)
    {
        var scopes = probeMatch.Scopes.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var (model, manufacturer) = ParseScopes(scopes);
        return new DiscoveryDevice(
            ConvertToList(probeMatch.Types),
            ConvertToList(probeMatch.XAddrs),
            model,
            manufacturer,
            remoteEndpoint.Address.ToString(),
            scopes
        );
    }

    private static (string Model, string Manufacturer) ParseScopes(IEnumerable<string> scopes)
    {
        string? model = null;
        string? manufacturer = null;
        string? name = null;
        foreach (var scope in scopes)
        {
            if (!HasValidPercentEncoding(scope) || !Uri.TryCreate(scope, UriKind.Absolute, out var uri))
            {
                continue;
            }

            // Split before decoding so encoded slashes remain part of the value.
            var segments = uri.AbsolutePath.Split('/');
            if (segments.Length < 3 || string.IsNullOrEmpty(segments[^1]))
            {
                continue;
            }

            // Some devices nest metadata, e.g. /acc/mfr/value or /name/acc/value.
            var category = segments.Take(segments.Length - 1).FirstOrDefault(segment =>
                segment is "hardware" or "mfr" or "manufacturer" or "name");
            var value = Uri.UnescapeDataString(segments[^1]);
            switch (category)
            {
                case "hardware":
                    model ??= value;
                    break;
                case "mfr":
                case "manufacturer":
                    manufacturer ??= value;
                    break;
                case "name":
                    name ??= value;
                    break;
            }
        }

        // Preserve the v3 name-based fallback when no manufacturer was advertised.
        manufacturer ??= name?.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return (model ?? string.Empty, manufacturer ?? string.Empty);
    }

    private static IEnumerable<string> ConvertToList(string spacedListString)
    {
        var strings = spacedListString.Split();
        foreach (var str in strings)
        {
            yield return str.Trim();
        }
    }

    private static bool HasValidPercentEncoding(string scope)
    {
        // IsWellFormedOriginalString rejects some encoded Unicode names in real device scopes.
        for (var index = 0; index < scope.Length; index++)
        {
            if (scope[index] != '%')
            {
                continue;
            }

            if (index + 2 >= scope.Length || !Uri.IsHexDigit(scope[index + 1]) || !Uri.IsHexDigit(scope[index + 2]))
            {
                return false;
            }

            index += 2;
        }

        return true;
    }
}