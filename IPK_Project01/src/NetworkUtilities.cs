using SharpPcap;
using SharpPcap.LibPcap;

using ProtocolType = System.Net.Sockets.ProtocolType;

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
        IPAddress srcIp = _ip.GetLocalIpFromDevice(interfaceName, destIp.AddressFamily);
            
        Random random = new Random();
        int srcPort = random.Next(1, 65536);
        //TCP
        if (isTcp)
        {
            Socket socket = new Socket(destIp.AddressFamily, SocketType.Raw, ProtocolType.Tcp);
            
            //IPv4
            if (destIp.AddressFamily == AddressFamily.InterNetwork)
            {
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, true);
                
                EndPoint endPoint = new IPEndPoint(destIp, destPort);
                
                byte[] tcpPacket = new byte[40];
                _ip.IPv4_Header(ref tcpPacket, srcIp.GetAddressBytes(), destIp.GetAddressBytes(), isTcp);
                _tcp.TCP_Header(ref tcpPacket, srcIp.GetAddressBytes(), destIp.GetAddressBytes(), srcPort, destPort, 20, true);

                if (destIp.Equals(IPAddress.Parse("127.0.0.1")))
                {
                    Console.WriteLine("TCP connection established.");
                    
                }
                
                socket.SendTo(tcpPacket, endPoint);

                int result = Receive(srcIp, destIp, srcPort, destPort, waitTime, true, isTcp);
                if (result == 0)
                {
                    socket.SendTo(tcpPacket, endPoint);
                    result = Receive(srcIp, destIp, srcPort, destPort, waitTime, true, isTcp);
                }

                socket.Close();
                return result;
            }
            //IPv6
            else
            {
                socket.Bind(new IPEndPoint(new IPAddress(srcIp.GetAddressBytes()), 0));
                
                byte[] tcpPacket = new byte[20];
                _tcp.TCP_Header(ref tcpPacket, srcIp.GetAddressBytes(), destIp.GetAddressBytes(), srcPort, destPort, 0, false);
                
                socket.SendTo(tcpPacket, new IPEndPoint(destIp, 0));
                
                int result = Receive(srcIp, destIp, srcPort, destPort, waitTime, false, isTcp);
                if (result == 0)
                {
                    socket.SendTo(tcpPacket, new IPEndPoint(destIp, 0));
                    result = Receive(srcIp, destIp, srcPort, destPort, waitTime, false, isTcp);
                }
                
                socket.Close();
                return result;
            }
        }
        //UDP
        else
        {
            Socket socket = new Socket(destIp.AddressFamily, SocketType.Raw, ProtocolType.Udp);
            
            //IPv4
            if (destIp.AddressFamily == AddressFamily.InterNetwork)
            {
                socket.Bind(new IPEndPoint(srcIp, 0));

                EndPoint endPoint = new IPEndPoint(destIp, destPort);

                byte[] udpPacket = _udp.UDP_Header(srcIp.GetAddressBytes(), destIp.GetAddressBytes(), srcPort, destPort, true);
                
                socket.SendTo(udpPacket, endPoint);
                
                int result = Receive(srcIp, destIp, srcPort, destPort, waitTime, true, isTcp);

                socket.Close();
                return result;
            }
            //IPv6
            else
            {
                socket.Bind(new IPEndPoint(new IPAddress(srcIp.GetAddressBytes()), 0));
                
                byte[] udpPacket = _udp.UDP_Header(srcIp.GetAddressBytes(), destIp.GetAddressBytes(), srcPort, destPort, false);
                
                socket.SendTo(udpPacket, new IPEndPoint(destIp, 0));
                
                int result = Receive(srcIp, destIp, srcPort, destPort, waitTime, false, isTcp);

                socket.Close();
                return result;
            }
        }
        return 99;
    }

    private int Receive(IPAddress srcIp, IPAddress destIp, int srcPort, int destPort, int waitTime, bool isIpv4, bool isTcp)
    {
        Socket socket;
        byte[] buffer = new byte[4096];
        
        if(isTcp)
            socket = new Socket(destIp.AddressFamily, SocketType.Raw, ProtocolType.Tcp);
        else
        {
            if(isIpv4)
                socket = new Socket(destIp.AddressFamily, SocketType.Raw, ProtocolType.Icmp);
            else
                socket = new Socket(destIp.AddressFamily, SocketType.Raw, ProtocolType.IcmpV6);
        }
        //Set ip and port where to listen
        socket.Bind(new IPEndPoint(srcIp, srcPort));
        socket.ReceiveTimeout = waitTime;
        
        DateTime startTime = DateTime.Now;
        try
        {
            while ((startTime.Millisecond + waitTime) >= DateTime.Now.Millisecond)
            {
                int received = socket.Receive(buffer);
                if (received > 0)
                {
                    if(isTcp)
                    {
                        int result = IsAckOrRstPacket(buffer, destIp, srcPort, destPort, isIpv4);
                        if(result != 99)
                        {
                            socket.Close();
                            return result;
                        }
                    }
                    else
                    {
                        socket.Close();
                        return IsUdpOpen(buffer, isIpv4);
                    }
                }
            }
            throw new SocketException((int)SocketError.TimedOut);
        }
        catch
        {
            socket.Close();
            if(isTcp)
                return 0;
            return 1;
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
                return 99;
        }
        else
        {
            int tcpHeaderStart = 0; // no IPv6 header included

            int tcpSourcePort = (buffer[tcpHeaderStart] << 8) | buffer[tcpHeaderStart + 1];
            int tcpDestinationPort = (buffer[tcpHeaderStart + 2] << 8) | buffer[tcpHeaderStart + 3];
            
            if (tcpSourcePort != destPort || tcpDestinationPort != srcPort)
                return 99;
        }
        
        // Extract TCP Flags (Byte 33 in IP + TCP header)
        int tcpFlags = buffer[ipHeaderLength + 13];

        bool isAck = (tcpFlags & 0x10) != 0; // ACK flag is 0x10 (00010000)
        bool isRst = (tcpFlags & 0x04) != 0; // RST flag is 0x04 (00000100)
        
        if (isRst) 
            return 2;
        return isAck ? 1 : 99;
    }

    private int IsUdpOpen(byte[] buffer, bool isIpv4)
    {
        if(isIpv4)
        { 
            if (buffer[20] == 3 && buffer[21] == 3) // 20(ICMP Type) 21(ICMP Code)j
                return 2;
            //Console.WriteLine($"Received ICMP Packet  - Type: {buffer[0]}, Code: {buffer[1]}");
            return 1;
        }
        
        if (buffer[0] == 1 && buffer[1] == 4) // 0(ICMP Type) 1(ICMP Code)
            return 2;
        //Console.WriteLine($"Received ICMPv6 Packet  - Type: {buffer[0]}, Code: {buffer[1]}");
        return 1;
        
    }
}