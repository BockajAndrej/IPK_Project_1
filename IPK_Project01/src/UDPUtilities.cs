namespace IPK_Project01;

public class UdpUtilities
{
    public void sendPacket(string targetIp, int port)
    {
        using (UdpClient udpClient = new UdpClient())
        {
            byte[] data = Encoding.ASCII.GetBytes("Hello World");
            udpClient.Send(data, data.Length, targetIp, port);
            Console.WriteLine($"Sent UDP packet to {targetIp}:{port}");
        }
    }
}