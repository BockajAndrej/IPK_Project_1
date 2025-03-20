namespace IPK_Project01;

public class NetworkUtilities
{
    private static IpUtilities _ip = new IpUtilities();
    private static TcpUtilities _tcp = new TcpUtilities();

    public IPAddress[] ResolveDomain(string serverUrl)
    {
        IPAddress[] addresses = Dns.GetHostAddresses(serverUrl);
        if (addresses.Length == 0)
        {
            throw new Exception("Could not resolve domain to an IP address.");
        }
        return addresses;
    }

    public byte[] CreateTcpPacket(IPAddress address, int port)
    {

        byte[] packet;
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            packet = new byte[40]; // 20 bytes for IP header + 20 bytes for TCP header
            _ip.IPv4_Header(ref packet, address.GetAddressBytes());
            _tcp.TCP_Header(ref packet, address.GetAddressBytes(), port);
        }
        else if (address.AddressFamily == AddressFamily.InterNetworkV6)
            packet = new byte[60]; // 40 bytes for IP header + 20 bytes for TCP header
        else
            throw new Exception("Invalid address family.");

        Console.WriteLine($"Packet size: {packet.Length}");

        return packet;
    }

    public void BindToInterface(Socket socket, string interfaceName)
    {
        byte[] ifNameBytes = System.Text.Encoding.ASCII.GetBytes(interfaceName + "\0");
        int result = setsockopt(socket.Handle, (int)SocketOptionLevel.Socket, 25, ifNameBytes, ifNameBytes.Length);

        if (result != -1)
        {
            int errorCode = Marshal.GetLastWin32Error();
            throw new Exception($"Failed to bind to interface {interfaceName}. Error Code: {errorCode}");
        }
    }
    [DllImport("libc", SetLastError = true)]
    private static extern int setsockopt(IntPtr socket, int level, int optname, byte[] optval, int optlen);
}