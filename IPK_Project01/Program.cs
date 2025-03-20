namespace IPK_Project01;

internal class Program
{
    static int IsAckOrRstPacket(byte[] buffer, string targetIp)
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

    static void Receive(Socket socket, IPAddress address, EndPoint endPoint, byte[] buffer)
    {
        try
        {
            while (true)
            {
                int received = socket.ReceiveFrom(buffer, ref endPoint);
                if (received > 0)
                {
                    int result = IsAckOrRstPacket(buffer, address.ToString());
                    if (result == 1)
                    {
                        Console.WriteLine("ACK received from " + address);
                        break;
                    }
                    else if (result == 2)
                    {
                        Console.WriteLine("RST received from " + address);
                        break;
                    }
                                
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        
    }
    
    private static void Main(string[] args)
    {
        string? url = null;
        object[] port = new object[4];

        string? interfaceName = null;
        int waitTime = 5000;
        
        NetworkUtilities network = new NetworkUtilities();

        try
        {
            ArgumentProcessing argProcess = new ArgumentProcessing();
            if (argProcess.Parser(args, ref url, ref interfaceName, ref port, ref waitTime) == false)
                return;

            //From there
            IPAddress[] addresses = network.ResolveDomain(url!);
            foreach (IPAddress address in addresses)
            {
                // Create raw TCP socket (only works on Linux)
                Socket rawSocket = new Socket(address.AddressFamily, SocketType.Raw, ProtocolType.Tcp);
                // Set socket options to include IP headers 
                rawSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, true);
                
                network.BindToInterface(rawSocket, interfaceName!);
                
                rawSocket.ReceiveTimeout = waitTime;

                // Destination (IP on port num)
                EndPoint endPoint;
                byte[] tcpPacket;
                byte[] buffer = new byte[4096];
                
                if (port[0] is int num)
                {
                    endPoint = new IPEndPoint(address, num);
                    // Manually craft a TCP SYN packet
                    tcpPacket = network.CreateTcpPacket(address, num);
                    // Send raw TCP packet
                    rawSocket.SendTo(tcpPacket, endPoint);
                    
                    Receive(rawSocket, address, endPoint, buffer);

                }
                else if (port[0] is int[] nums)
                {
                    endPoint = new IPEndPoint(address, nums[0]);
                }
                
                rawSocket.Close();
                Console.WriteLine("Raw socket was closed!");
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}