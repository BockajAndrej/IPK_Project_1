namespace IPK_Project01;

public class TcpUtilities : IpUtilities
{
    public void TCP_Header(ref byte[] packet, byte[] destIp, int port, int offset)
    {
        // Constructing TCP Header
        packet[offset + 0] = 0xAA; packet[offset + 1] = 0xBB; // Source Port
        packet[offset + 2] = 0x00; packet[offset + 3] = (byte)port; // Dest Port
        packet[offset + 4] = 0x00; packet[offset + 5] = 0x00; packet[offset + 6] = 0x00; packet[offset + 7] = 0x00; // Seq Num
        packet[offset + 8] = 0x00; packet[offset + 9] = 0x00; packet[offset + 10] = 0x00; packet[offset + 11] = 0x00; // Ack Num
        packet[offset + 12] = 0x50; // Data offset = 5 (<<4), Reserved + NS
        packet[offset + 13] = 0x02; // Flags = SYN
        packet[offset + 14] = 0x72; packet[offset + 15] = 0x10; // Window Size
        packet[offset + 16] = 0x00; packet[offset + 17] = 0x00; // Checksum
        packet[offset + 18] = 0x00; packet[offset + 19] = 0x00; // Urgent Pointer

        // Source IP 
        byte[] srcIp = IPAddress.Parse(GetLocalIpAddress()).GetAddressBytes();
        // Compute TCP checksum
        ushort tcpChecksum = ComputeTcpChecksum(srcIp, destIp, packet[offset..(offset+20)]);
        packet[offset + 16] = (byte)(tcpChecksum >> 8);
        packet[offset + 17] = (byte)(tcpChecksum & 0xFF);
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