namespace IPK_Project01;

public class UdpUtilities
{
    public void SendPacket(string targetIp, int port)
    {
        using (UdpClient udpClient = new UdpClient())
        {
            byte[] data = Encoding.ASCII.GetBytes("Hello World");
            udpClient.Send(data, data.Length, targetIp, port);
            Console.WriteLine($"Sent UDP packet to {targetIp}:{port}");
        }
    }

    public int ReceivePacket(int waitTime)
    {
        Socket icmpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);
        icmpSocket.Bind(new IPEndPoint(IPAddress.Any, 0));
        icmpSocket.ReceiveTimeout = waitTime; // 3 seconds
        
        byte[] buffer = new byte[512];
        EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
        
        try
        {
            int received = icmpSocket.ReceiveFrom(buffer, ref remoteEP);
    
            // ICMP header is typically at byte 20 (after IP header)
            int icmpHeaderOffset = 20;
            byte icmpType = buffer[icmpHeaderOffset];
            byte icmpCode = buffer[icmpHeaderOffset + 1];

            Console.WriteLine($"Received ICMP from {((IPEndPoint)remoteEP).Address}");
            Console.WriteLine($"ICMP Type: {icmpType}, Code: {icmpCode}");

            // Example checks
            if (icmpType == 3)
            {
                Console.WriteLine("ICMP Destination Unreachable");
                return 2;
            }
            return 1;
        }
        catch (SocketException)
        {
            Console.WriteLine("No ICMP response received (timeout)");
            return 0;
        }
    }
}