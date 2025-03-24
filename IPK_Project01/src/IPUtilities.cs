
namespace IPK_Project01;

public class IpUtilities : NetworkUtilities
{
    public IPAddress GetLocalIpFromDevice(string interfaceName, AddressFamily family)
    {
        var interfaces = NetworkInterface.GetAllNetworkInterfaces();

        foreach (var ni in interfaces)
        {
            if (ni.Name == interfaceName && ni.OperationalStatus == OperationalStatus.Up)
            {
                var ipProps = ni.GetIPProperties();
                var addresses = ipProps.UnicastAddresses;

                foreach (var addr in addresses)
                {
                    if (addr.Address.AddressFamily == family)
                    {
                        return addr.Address;
                    }
                }
            }
        }
        return null;
    }
    
    public void IPv4_Header(ref byte[] packet, byte[] srcIp, byte[] destIp)
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
        packet[12] = srcIp[0]; packet[13] = srcIp[1]; packet[14] = srcIp[2]; packet[15] = srcIp[3];
        // Destination IP
        packet[16] = destIp[0]; packet[17] = destIp[1]; packet[18] = destIp[2]; packet[19] = destIp[3];

        ushort checksum = ComputeIPv4Checksum(packet);
        packet[10] = (byte)(checksum >> 8);
        packet[11] = (byte)(checksum & 0xFF);
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