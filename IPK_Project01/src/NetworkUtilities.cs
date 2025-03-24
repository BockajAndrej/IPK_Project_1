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

    public int  CheckPort(bool isTcp, IPAddress address, string interfaceName, int waitTime, int port)
    {
        if (isTcp)
        { 
            return CheckTCP(address, interfaceName, waitTime, port);
        }
        else
        {
            return CheckUDP(address, interfaceName, waitTime, port);
        }
    }
    
    private int CheckTCP(IPAddress address, string interfaceName, int waitTime, int port)
    {
        Socket? socket = null;
        // Create raw TCP socket (only works on Linux)
        socket = new Socket(address.AddressFamily, SocketType.Raw, ProtocolType.Tcp);
        // Set socket options to include IP headers 
        if (address.AddressFamily == AddressFamily.InterNetwork)
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, true);
        else
        {
            IPAddress.TryParse(_ip.GetLocalIPv6Address(), out IPAddress sourceAddress);
            
            byte[] targetIp = address.GetAddressBytes();
            byte[] sourceIp = sourceAddress.GetAddressBytes();
            byte[] tcpHeader = new byte[20];
            
            //Sour port
            tcpHeader[0] = sourceIp[0];
            tcpHeader[1] = sourceIp[1];
            //Dest port
            tcpHeader[2] = targetIp[0];
            tcpHeader[3] = targetIp[1];
            
            //Sequence number 
            tcpHeader[4] = 0x00;
            tcpHeader[5] = 0x00;
            tcpHeader[6] = 0x00;
            tcpHeader[7] = 0x00;
            
            //Ack
            tcpHeader[8] = 0x00;
            tcpHeader[9] = 0x00;
            tcpHeader[10] = 0x00;
            tcpHeader[11] = 0x00;
            
            //Data offrest
            tcpHeader[12] = 0x50;
            tcpHeader[13] = 0x02;
            
            //Checksum
            tcpHeader[14] = 0x00;
            tcpHeader[15] = 0x00;
            
            //Urgent pointer
            tcpHeader[16] = 0x00;
            tcpHeader[17] = 0x00;
            
            //Pseudo header for TCP checksum
            byte[] pseudoHeader = new byte[40 + tcpHeader.Length];
            Array.Copy(sourceIp, 0, pseudoHeader, 0, 16);
            Array.Copy(targetIp, 0, pseudoHeader, 16, 16);
            pseudoHeader[32] = 0x00; //Reserved
            pseudoHeader[33] = 0x06; //TCP
            pseudoHeader[34] = (byte)(tcpHeader.Length >> 8);
            pseudoHeader[35] = (byte)(tcpHeader.Length & 0xFF);
            Array.Copy(tcpHeader, 0, pseudoHeader, 36, tcpHeader.Length);
            
            ushort tcpChecksum = _tcp.ComputeTcpChecksum(sourceIp, targetIp, pseudoHeader , false);
            tcpHeader[16] = (byte)(tcpChecksum >> 8);
            tcpHeader[17] = (byte)(tcpChecksum & 0xFF);
            
            Socket rawSocket = new Socket(address.AddressFamily, SocketType.Raw, ProtocolType.Tcp);
            rawSocket.Bind(new IPEndPoint(new IPAddress(sourceIp), 0));
            rawSocket.SendTo(tcpHeader, new IPEndPoint(IPAddress.Parse(address.ToString()), 0));
            
            rawSocket.Close();
        }
        
        socket.ReceiveTimeout = waitTime;
        
        int result = SendPacket(socket, address, port);
        
        socket.Close();
        return result;
    }

    private int CheckUDP(IPAddress address, string interace, int waitTime, int port)
    {
        int result = 0;
        do
        {
            _udp.SendPacket(address.ToString(), port);
            result = _udp.ReceivePacket(waitTime);
        } while (result == 0);
        return result;
    }
    
    public int SendPacket(Socket socket, IPAddress address, int port)
    {
        byte[] tcpPacket;
        byte[] buffer = new byte[4096];
        
        EndPoint endPoint = new IPEndPoint(address, port);
        // Manually craft a TCP SYN packet
        tcpPacket = CreateTcpPacket(address, port);
        // Send raw TCP packet
        socket.SendTo(tcpPacket, endPoint);
                    
        int result = Receive(socket, address, endPoint, buffer);
        if (result == 0)
        {
            socket.SendTo(tcpPacket, endPoint);
            return Receive(socket, address, endPoint, buffer);
        }
        return result;
    }
    
    private byte[] CreateTcpPacket(IPAddress address, int port)
    {

        byte[] packet;
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            // 20 bytes for IP header + 20 bytes for TCP header
            packet = new byte[40]; 
            _ip.IPv4_Header(ref packet, address.GetAddressBytes());
            _tcp.TCP_Header(ref packet, address.GetAddressBytes(), port, 20, address.AddressFamily == AddressFamily.InterNetwork);
        }
        else if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            // 40 bytes for IP header + 20 bytes for TCP header
            packet = new byte[60];
            //_ip.IPv6_Header(ref packet, address.GetAddressBytes());
            _tcp.TCP_Header(ref packet, address.GetAddressBytes(), port, 40, address.AddressFamily == AddressFamily.InterNetwork);
        }
        else
            throw new Exception("Invalid address family.");

        //Console.WriteLine($"Packet size: {packet.Length}");

        return packet;
    }
    
    private int Receive(Socket socket, IPAddress address, EndPoint endPoint, byte[] buffer)
    {
        try
        {
            while (true)
            {
                int received = socket.ReceiveFrom(buffer, ref endPoint);
                if (received > 0)
                    return IsAckOrRstPacket(buffer, address.ToString());
            }
        }
        catch
        {
            return 0;
        }
        
    }
    private int IsAckOrRstPacket(byte[] buffer, string targetIp)
    {
        // Extract source IP (Bytes 12-15 in IPv4 header)
        string sourceIp = $"{buffer[12]}.{buffer[13]}.{buffer[14]}.{buffer[15]}";

        // Extract TCP Flags (Byte 33 in IP + TCP header)
        int tcpFlags = buffer[33];

        bool isAck = (tcpFlags & 0x10) != 0; // ACK flag is 0x10 (00010000)
        bool isRst = (tcpFlags & 0x04) != 0; // RST flag is 0x04 (00000100)
        if(sourceIp == targetIp)
        {
            if (isRst)
                return 2;
            return isAck ? 1 : 0;
        }
        return 0;
    }
}