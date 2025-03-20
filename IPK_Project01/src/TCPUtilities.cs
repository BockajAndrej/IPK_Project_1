namespace IPK_Project01;

public class TcpUtilities : IpUtilities
{
    public void TCP_Header(ref byte[] packet, byte[] destIp, int port)
    {
        // Constructing TCP Header
        packet[20] = 0xAA; packet[21] = 0xBB; // Source Port (random)
        packet[22] = 0x00; packet[23] = (byte)port; // Destination Port (80 = 0x50)
        packet[24] = 0x00; packet[25] = 0x00; packet[26] = 0x00; packet[27] = 0x00; // Sequence Number
        packet[28] = 0x00; packet[29] = 0x00; packet[30] = 0x00; packet[31] = 0x00; // ACK Number
        packet[32] = 0x50; // Data offset, reserved, flags
        packet[33] = 0x02; // Flags (SYN)
        packet[34] = 0x72; packet[35] = 0x10; // Window Size
        packet[36] = 0x00; packet[37] = 0x00; // Checksum (to be computed)
        packet[38] = 0x00; packet[39] = 0x00; // Urgent Pointer

        // Source IP 
        byte[] srcIp = IPAddress.Parse(GetLocalIpAddress()).GetAddressBytes();
        // Compute TCP checksum
        ushort tcpChecksum = ComputeTcpChecksum(srcIp, destIp, packet[20..40]);
        packet[36] = (byte)(tcpChecksum >> 8);
        packet[37] = (byte)(tcpChecksum & 0xFF);
    }
    
    private static ushort ComputeTcpChecksum(byte[] sourceIp, byte[] destIp, byte[] tcpHeader)
    {
        long sum = 0;

        // IPv4 Pseudo-Header (Source IP + Destination IP)
        for (int i = 0; i < 4; i += 2)
        {
            sum += (ushort)((sourceIp[i] << 8) | sourceIp[i + 1]);
            sum += (ushort)((destIp[i] << 8) | destIp[i + 1]);
        }

        // Add Protocol (6 for TCP) and TCP Length (20 bytes)
        sum += 6;
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
}