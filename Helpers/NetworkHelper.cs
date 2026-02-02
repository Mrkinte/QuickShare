using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace QuickShare.Helpers
{
    public class NetworkHelper
    {
        /// <summary>
        /// Retrieve all network card information.
        /// </summary>
        /// <param name="includeIPv6"></param>
        /// <returns></returns>
        public static List<NetworkInterfaceInfo> GetActiveNetworkInterfaces(
            bool includeIPv6 = false)
        {
            var result = new List<NetworkInterfaceInfo>();

            var adapters = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var adapter in adapters)
            {
                // Skip the disabled interfaces.
                if (adapter.OperationalStatus != OperationalStatus.Up)
                    continue;

                // Skip the loopback interface.
                if (adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                var ipProps = adapter.GetIPProperties();

                foreach (var ip in ipProps.UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork) // IPv4
                    {
                        result.Add(new NetworkInterfaceInfo
                        {
                            Name = adapter.Name,
                            IpAddress = ip.Address
                        });
                    }
                    else if (includeIPv6 && ip.Address.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        // Skip the IPv6 local link address.
                        if (!ip.Address.ToString().StartsWith("fe80"))
                        {
                            result.Add(new NetworkInterfaceInfo
                            {
                                Name = adapter.Name,
                                IpAddress = ip.Address
                            });
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Check if a port is in use on the local machine.
        /// </summary>
        /// <param name="port"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static bool IsPortInUse(int port)
        {
            if (port < 1 || port > 65535)
                throw new ArgumentOutOfRangeException(nameof(port), "The port number must be within the range of 1 to 65535.");

            var address = IPAddress.Loopback;

            try
            {
                using var listener = new TcpListener(address, port);
                listener.Start();
                listener.Stop();
                return false;
            }
            catch (SocketException ex)
            {
                return ex.SocketErrorCode == SocketError.AddressAlreadyInUse ||
                       ex.SocketErrorCode == SocketError.AccessDenied;
            }
            catch
            {
                return true;
            }
        }

    }

    public class NetworkInterfaceInfo
    {
        public string Name { get; set; } = string.Empty;
        public IPAddress? IpAddress { get; set; }
        public string DisplayString
        {
            get
            {
                return $"{IpAddress}\t({Name})";
            }
        }
    }
}
