using SharpPcap;

namespace IPK_Project01;

public class NetworkUtilities
{
    private static IpUtilities _ip = new IpUtilities();
    private static TcpUtilities _tcp = new TcpUtilities();
    private static UdpUtilities _udp = new UdpUtilities();

    public IPAddress[] ResolveDomain(string serverUrl)
    {
        IPAddress[] addresses = Dns.GetHostAddresses(serverUrl);
        if (addresses.Length == 0)
        {
            throw new Exception("Could not resolve domain to an IP address.");
        }
        return addresses;
    }

    public int  CheckPort(IPAddress destIp, string interfaceName, int destPort, bool isTcp, int waitTime)
    {
        if (isTcp)
        {
            IPAddress srcIp = _ip.GetLocalIpFromDevice(interfaceName, destIp.AddressFamily);
            
            Random random = new Random();
            int srcPort = random.Next(1, 65536);
            
            //IPv4
            if (destIp.AddressFamily == AddressFamily.InterNetwork)
            {
                // Create raw TCP socket (only works on Linux)
                Socket socket = new Socket(destIp.AddressFamily, SocketType.Raw, ProtocolType.Tcp);
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, true);
                
                EndPoint endPoint = new IPEndPoint(destIp, destPort);
                
                byte[] tcpPacket = new byte[40];
                _ip.IPv4_Header(ref tcpPacket, srcIp.GetAddressBytes(), destIp.GetAddressBytes());
                _tcp.TCP_Header(ref tcpPacket, srcIp.GetAddressBytes(), destIp.GetAddressBytes(), srcPort, destPort, 20, true);
                
                socket.SendTo(tcpPacket, endPoint);

                int result = Receive(srcIp, destIp, srcPort, destPort, waitTime, true);
                if (result == 0)
                {
                    socket.SendTo(tcpPacket, endPoint);
                    result = Receive(srcIp, destIp, srcPort, destPort, waitTime, true);
                }
                
                socket.Close();
                return result;
            }
            else
            {
                byte[] tcpPacket = new byte[20];
                
                //_ip.IPv6_Header(ref tcpPacket, srcIp.GetAddressBytes(), destIp.GetAddressBytes());
                _tcp.TCP_Header(ref tcpPacket, srcIp.GetAddressBytes(), destIp.GetAddressBytes(), srcPort, destPort, 0, false);
                
                Socket socket = new Socket(destIp.AddressFamily, SocketType.Raw, ProtocolType.Tcp);
                socket.Bind(new IPEndPoint(new IPAddress(srcIp.GetAddressBytes()), 0));
                socket.SendTo(tcpPacket, new IPEndPoint(destIp, 0));
                
                int result = Receive(srcIp, destIp, srcPort, destPort, waitTime, false);
                
                socket.Close();
                return result;
            }
        }
        return 0;
    }

    private int Receive(IPAddress srcIp, IPAddress destIp, int srcPort, int destPort, int waitTime, bool isIpv4)
    {
        Socket socket;
        byte[] buffer = new byte[4096];
        EndPoint endPoint;
        
        if (isIpv4)
        {
            socket = new Socket(destIp.AddressFamily, SocketType.Raw, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, true);
            socket.ReceiveTimeout = waitTime;
        
            socket.Bind(new IPEndPoint(IPAddress.Any, destPort));
            
            endPoint = new IPEndPoint(destIp, 0);
        }
        else
        {
            socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Raw, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(srcIp, destPort));
            socket.ReceiveTimeout = waitTime;
            
            endPoint = new IPEndPoint(destIp, 0);
        }
        
        try
        {
            while (true)
            {
                int received = socket.ReceiveFrom(buffer, ref endPoint);
                if (received > 0)
                {
                    socket.Close();
                    return IsAckOrRstPacket(buffer, destIp, srcPort, destPort, isIpv4);
                }
            }
        }
        catch
        {
            socket.Close();
            return 0;
        }
    }

    private int IsAckOrRstPacket(byte[] buffer, IPAddress destIp, int srcPort, int destPort, bool isIpv4)
    {
        // Extract source IP (Bytes 12-15 in IPv4 header)
        string sourceIp;
        int ipHeaderLength = isIpv4 ? 20 : 0;
        
        if(isIpv4)
        {
            sourceIp = $"{buffer[12]}.{buffer[13]}.{buffer[14]}.{buffer[15]}";
            if (sourceIp != destIp.ToString())
                return 0;
        }
        else
        {
            int tcpHeaderStart = 0; // no IPv6 header included

            int tcpSourcePort = (buffer[tcpHeaderStart] << 8) | buffer[tcpHeaderStart + 1];
            int tcpDestinationPort = (buffer[tcpHeaderStart + 2] << 8) | buffer[tcpHeaderStart + 3];
            
            if (tcpSourcePort != destPort || tcpDestinationPort != srcPort)
                return 0;
        }
        
        // Extract TCP Flags (Byte 33 in IP + TCP header)
        int tcpFlags = buffer[ipHeaderLength + 13];

        bool isAck = (tcpFlags & 0x10) != 0; // ACK flag is 0x10 (00010000)
        bool isRst = (tcpFlags & 0x04) != 0; // RST flag is 0x04 (00000100)
        
        if (isRst) 
            return 2;
        return isAck ? 1 : 0;
    }
    
    /*private int ProcessIPv6Response(byte[] buffer, int received, int targetPort, int sentSourcePort, IPAddress targetIp)
    {
        Console.WriteLine($"[ProcessIPv6Response] Received {received} bytes");

        if (received < 20)
        {
            Console.WriteLine("[ProcessIPv6Response] Packet too short to contain TCP header.");
            return 0;
        }

        int tcpHeaderStart = 0; // no IPv6 header included

        int tcpSourcePort = (buffer[tcpHeaderStart] << 8) | buffer[tcpHeaderStart + 1];
        int tcpDestinationPort = (buffer[tcpHeaderStart + 2] << 8) | buffer[tcpHeaderStart + 3];
        Console.WriteLine($"[ProcessIPv6Response] TCP src port: {tcpSourcePort}, dst port: {tcpDestinationPort}");

        if (tcpSourcePort != targetPort || tcpDestinationPort != sentSourcePort)
        {
            Console.WriteLine("[ProcessIPv6Response] Port mismatch, skipping.");
            return 0;
        }

        byte tcpFlags = buffer[tcpHeaderStart + 13];
        Console.WriteLine($"[ProcessIPv6Response] TCP flags: 0x{tcpFlags:X2}");

        if ((tcpFlags & (byte)TcpFlags.Rst) != 0)
        {
            Console.WriteLine("[ProcessIPv6Response] TCP RST received — port is closed.");
            return 2;
        }

        if ((tcpFlags & (byte)TcpFlags.SynAck) == (byte)TcpFlags.SynAck)
        {
            Console.WriteLine("[ProcessIPv6Response] SYN+ACK received — port is open.");
            return 1;
        }

        Console.WriteLine("[ProcessIPv6Response] Flags don't match expected values.");
        return null;
    }*/

    /*
    private int CheckUDP(IPAddress address, string interace, int waitTime, int port)
    {
        int result = 0;
        do
        {
            _udp.SendPacket(address.ToString(), port);
            result = _udp.ReceivePacket(waitTime);
        } while (result == 0);
        return result;
    }*/
}