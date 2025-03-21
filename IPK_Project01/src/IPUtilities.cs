
namespace IPK_Project01;

public class IpUtilities : NetworkUtilities
{
    protected static string GetLocalIpAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        return host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString() ?? "No IPv4 found";
    }
    public string GetLocalIPv6Address()
    {
        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up)
                continue;

            foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
            {
                if (ip.Address.AddressFamily == AddressFamily.InterNetworkV6 &&
                    !ip.Address.IsIPv6LinkLocal &&         // Skip link-local (fe80::)
                    !IPAddress.IsLoopback(ip.Address))     // Skip loopback (::1)
                {
                    return ip.Address.ToString();
                }
            }
        }

        throw new Exception("No valid IPv6 address found.");
    }
    public void IPv4_Header(ref byte[] packet, byte[] destIp)
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

        // Source IP 
        byte[] srcIp = IPAddress.Parse(GetLocalIpAddress()).GetAddressBytes();
        packet[12] = srcIp[0]; packet[13] = srcIp[1]; packet[14] = srcIp[2]; packet[15] = srcIp[3];
        // Destination IP
        packet[16] = destIp[0]; packet[17] = destIp[1]; packet[18] = destIp[2]; packet[19] = destIp[3];

        ushort checksum = ComputeIPv4Checksum(packet);
        packet[10] = (byte)(checksum >> 8);
        packet[11] = (byte)(checksum & 0xFF);
    }

    public void IPv6_Header(ref byte[] packet, byte[] destIp)
    {
        // Constructing IPv6 Header (40 bytes)
        packet[0] = 0x60; // Version (6) and Traffic Class
        packet[1] = 0x00; // Traffic Class (continued)
        packet[2] = 0x00; packet[3] = 0x00; // Flow Label
        
        // Payload Length (e.g., 20 bytes for TCP header only)
        ushort payloadLength = 20; // Replace with actual TCP+data length if needed
        packet[4] = (byte)(payloadLength >> 8);
        packet[5] = (byte)(payloadLength & 0xFF);
        
        packet[6] = 0x06; // Next Header (TCP)
        packet[7] = 0x40; // Hop Limit (64)

        // Source IP (16 bytes)
        byte[] srcIp = IPAddress.Parse(GetLocalIPv6Address()).GetAddressBytes();
        Buffer.BlockCopy(srcIp, 0, packet, 8, 16);

        // Destination IP (16 bytes)
        Buffer.BlockCopy(destIp, 0, packet, 24, 16);
    }
    private ushort ComputeIPv4Checksum(byte[] header)
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
}