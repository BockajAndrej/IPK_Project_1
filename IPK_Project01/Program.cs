namespace IPK_Project01;

class Program
{
    // Funkcia na testovanie portu
    static string TestPort(IPAddress address, int port, int waitTime)
    {
        try
        {
            using (Socket socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.ReceiveTimeout = waitTime;
                socket.SendTimeout = waitTime;

                IPEndPoint endPoint = new IPEndPoint(address, port);

                // Pokúša sa pripojiť na adresu a port
                socket.Connect(endPoint);

                return "open";
            }
        }
        catch (SocketException ex)
        {
            // Ak je port filtrovaný alebo uzavretý, odchyti výnimku
            if (ex.SocketErrorCode == SocketError.ConnectionRefused)
                return "closed";
            if (ex.SocketErrorCode == SocketError.TimedOut)
                return "filtered"; 
            throw;
        }
    }

    static void IPv4_Header(ref byte[] packet)
    {
        // Constructing IP Header
        packet[0] = 0x45; // IPv4, header length = 5
        packet[1] = 0x00; // DSCP & ECN
        packet[2] = 0x00; packet[3] = 0x28; // Total Length (40 bytes)
        packet[4] = 0xAB; packet[5] = 0xCD; // Identification
        packet[6] = 0x40; packet[7] = 0x00; // Flags & Fragment Offset
        packet[8] = 0x40; // TTL = 64
        packet[9] = 0x06; // Protocol = TCP (6)
        packet[10] = 0; packet[11] = 0; // Header Checksum (computed later)
        
        // Source IP (192.168.1.100)
        packet[12] = 192; packet[13] = 168; packet[14] = 1; packet[15] = 100;
        // Destination IP (Google DNS)
        packet[16] = 8; packet[17] = 8; packet[18] = 8; packet[19] = 8;
        
        ushort checksum = ComputeIPv4Checksum(packet);
        packet[10] = (byte)(checksum >> 8);
        packet[11] = (byte)(checksum & 0xFF);
    }
    static void IPv6_Header(ref byte[] packet)
    {
        // Constructing IPv6 Header (40 bytes)
        packet[0] = 0x60; // Version (6) and Traffic Class
        packet[1] = 0x00; // Traffic Class (continued)
        packet[2] = 0x00; packet[3] = 0x00; // Flow Label
        packet[4] = 0x00; packet[5] = 0x14; // Payload Length (20 bytes for TCP header)
        packet[6] = 0x06; // Next Header (TCP)
        packet[7] = 0x40; // Hop Limit (64)

        // Source IPv6 Address (2001:db8::1)
        byte[] sourceIp = { 0x20, 0x01, 0x0d, 0xb8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 };
        // Copies source address into packet array at the correct position 
        Buffer.BlockCopy(sourceIp, 0, packet, 8, 16);

        // Destination IPv6 Address (Google DNS IPv6)
        byte[] destIp = { 0x20, 0x01, 0x48, 0x60, 0x48, 0x60, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x88 };
        // Copies destination address into packet array at the correct position
        Buffer.BlockCopy(destIp, 0, packet, 24, 16);
    }

    static void TCP_Header(ref byte[] packet, byte[] sourceIp, byte[] destIp)
    {
        // Constructing TCP Header
        packet[20] = 0xAA; packet[21] = 0xBB; // Source Port (random)
        packet[22] = 0x00; packet[23] = 0x50; // Destination Port (80)
        packet[24] = 0x00; packet[25] = 0x00; packet[26] = 0x00; packet[27] = 0x00; // Sequence Number
        packet[28] = 0x00; packet[29] = 0x00; packet[30] = 0x00; packet[31] = 0x00; // ACK Number
        packet[32] = 0x50; // Data offset, reserved, flags
        packet[33] = 0x02; // Flags (SYN)
        packet[34] = 0x72; packet[35] = 0x10; // Window Size
        packet[36] = 0x00; packet[37] = 0x00; // Checksum (to be computed)
        packet[38] = 0x00; packet[39] = 0x00; // Urgent Pointer
        // Compute TCP checksum
        ushort tcpChecksum = ComputeTcpChecksum(sourceIp, destIp, packet[20..40]);
        packet[36] = (byte)(tcpChecksum >> 8);
        packet[37] = (byte)(tcpChecksum & 0xFF);
    }
    static byte[] CreateTcpPacket(IPAddress address)
    {
        byte[] packet = null;
        if(address.AddressFamily == AddressFamily.InterNetwork)
            packet = new byte[40]; // 20 bytes for IP header + 20 bytes for TCP header
        else if(address.AddressFamily == AddressFamily.InterNetworkV6)
            packet = new byte[60]; // 40 bytes for IP header + 20 bytes for TCP header
        else
            throw new Exception("Invalid address family.");
        
        Console.WriteLine($"Packet size: {packet.Length}");
        
        return packet;
    }
    static ushort ComputeIPv4Checksum(byte[] header)
    {
        long sum = 0;
    
        // Sum 16-bit words
        for (int i = 0; i < header.Length; i += 2)
        {
            ushort word = (ushort)((header[i] << 8) + (i + 1 < header.Length ? header[i + 1] : 0));
            sum += word;
        }

        // Add carry bits
        while ((sum >> 16) > 0)
        {
            sum = (sum & 0xFFFF) + (sum >> 16);
        }

        return (ushort)~sum; // One's complement
    }
    static ushort ComputeTcpChecksum(byte[] sourceIP, byte[] destIP, byte[] tcpHeader)
    {
        long sum = 0;

        // IPv4 Pseudo-Header (Source IP + Destination IP)
        for (int i = 0; i < 4; i += 2)
        {
            sum += (ushort)((sourceIP[i] << 8) | sourceIP[i + 1]);
            sum += (ushort)((destIP[i] << 8) | destIP[i + 1]);
        }

        // Add Protocol (6 for TCP) and TCP Length (20 bytes)
        sum += (ushort)(6);
        sum += (ushort)tcpHeader.Length;

        // Sum TCP Header
        for (int i = 0; i < tcpHeader.Length; i += 2)
        {
            ushort word = (ushort)((tcpHeader[i] << 8) | (i + 1 < tcpHeader.Length ? tcpHeader[i + 1] : 0));
            sum += word;
        }

        // Add carry bits
        while ((sum >> 16) > 0)
        {
            sum = (sum & 0xFFFF) + (sum >> 16);
        }

        return (ushort)~sum; // One's complement
    }
    static IPAddress[] ResolveDomain(string serverUrl)
    {
        IPAddress[] addresses = Dns.GetHostAddresses(serverUrl);
        if (addresses.Length == 0)
        {
            throw new Exception("Could not resolve domain to an IP address.");
        }
        return addresses;
    }
    
    static void Main(string[] args)
    {
        string? url = null;
        object[] port = new object[4];
        
        string? interfaceName = null; 
        int waitTime = 5000;
        
        try
        {
            ArgumentProcessing argProcess = new ArgumentProcessing();
            if(argProcess.Parser(args, ref url, ref interfaceName, ref port, ref waitTime) == false)
                return;
            
            //From there
            IPAddress[] addresses = ResolveDomain(url);
            // IPAddress address = IPAddress.Parse(addresses[0].ToString());
            foreach (IPAddress address in addresses)
            {
                // Create raw TCP socket (only works on Linux)
                Socket rawSocket = new Socket(address.AddressFamily, SocketType.Raw, ProtocolType.Tcp);
                
                // Set socket options to include IP headers 
                rawSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, true);

                // Destination (IP on port num)
                IPEndPoint endPoint = null;
                if (port[0] is int num)
                {
                    endPoint = new IPEndPoint(address, num);
                }
                
                // Manually craft a TCP SYN packet
                byte[] tcpPacket = CreateTcpPacket(address);

                // Send raw TCP packet
                rawSocket.SendTo(tcpPacket, endPoint);
                Console.WriteLine("Raw TCP packet sent!");
            }
            
            /*
            IPAddress[] addresses = ResolveDomain(url);
            IPAddress address = IPAddress.Parse(addresses[0].ToString());
            
            if (port[0] is int num)
            {
                string result = TestPort(address, num, waitTime);
                Console.WriteLine($"Port {num} is {result}.");
            }
             */

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}