using System.Net;
using System.Net.Sockets;

namespace ImmoDigger.Infrastructure.Import;

/// <summary>
/// Prevents the user-triggered metadata fetcher from becoming an SSRF
/// proxy into localhost or a private network. Host names are resolved
/// before the request and every returned address must be publicly routable.
/// </summary>
public interface IManualImportUrlSafetyChecker
{
    Task<bool> IsSafeAsync(Uri uri, CancellationToken cancellationToken);
}

public sealed class ManualImportUrlSafetyChecker : IManualImportUrlSafetyChecker
{
    public async Task<bool> IsSafeAsync(Uri uri, CancellationToken cancellationToken)
    {
        if ((uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrWhiteSpace(uri.Host) || !string.IsNullOrEmpty(uri.UserInfo))
        {
            return false;
        }

        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(uri.DnsSafeHost, cancellationToken);
        }
        catch (Exception exception) when (exception is SocketException or ArgumentException)
        {
            return false;
        }

        return addresses.Length > 0 && addresses.All(IsPublicAddress);
    }

    internal static bool IsPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address) ||
            address.Equals(IPAddress.Any) ||
            address.Equals(IPAddress.None) ||
            address.Equals(IPAddress.IPv6Any) ||
            address.Equals(IPAddress.IPv6None))
        {
            return false;
        }

        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            return bytes[0] != 0 &&
                   bytes[0] != 10 &&
                   bytes[0] != 127 &&
                   !(bytes[0] == 100 && bytes[1] is >= 64 and <= 127) &&
                   !(bytes[0] == 169 && bytes[1] == 254) &&
                   !(bytes[0] == 172 && bytes[1] is >= 16 and <= 31) &&
                   !(bytes[0] == 192 && bytes[1] == 168) &&
                   !(bytes[0] == 198 && bytes[1] is 18 or 19) &&
                   bytes[0] < 224;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var isUniqueLocal = (bytes[0] & 0xfe) == 0xfc;
            return !address.IsIPv6LinkLocal &&
                   !address.IsIPv6Multicast &&
                   !address.IsIPv6SiteLocal &&
                   !isUniqueLocal;
        }

        return false;
    }
}
