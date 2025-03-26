namespace IPK_Project01;

public class UdpUtilities
{
    public byte[] UDP_Header(byte[] srcIp, byte[] destIp, int srcPort, int port, bool isIPv4)
    {
        byte[] udpSegment = new byte[12]; // 8-byte header + 4 bytes of data
        udpSegment[0] = (byte)(srcPort >> 8);
        udpSegment[1] = (byte)(srcPort & 0xFF);
        udpSegment[2] = (byte)(port >> 8);
        udpSegment[3] = (byte)(port & 0xFF);
        udpSegment[4] = 0;
        udpSegment[5] = 12; // Length = 12
        udpSegment[6] = 0;
        udpSegment[7] = 0;
        udpSegment[8] = 0xDE;
        udpSegment[9] = 0xAD;
        udpSegment[10] = 0xBE;
        udpSegment[11] = 0xEF;

        byte[] pseudoHeader = BuildPseudoHeader(srcIp, destIp, udpSegment.Length, isIPv4);
        
        // Combine pseudo-header + UDP segment
        byte[] checksumData = new byte[pseudoHeader.Length + udpSegment.Length];
        Buffer.BlockCopy(pseudoHeader, 0, checksumData, 0, pseudoHeader.Length);
        Buffer.BlockCopy(udpSegment, 0, checksumData, pseudoHeader.Length, udpSegment.Length);
        
        // Pad if odd number of bytes
        if (checksumData.Length % 2 != 0)
        {
            Array.Resize(ref checksumData, checksumData.Length + 1);
        }

        int checksum =  ComputeChecksum(checksumData);
        
        // Store checksum back in header
        udpSegment[6] = (byte)(checksum >> 8);
        udpSegment[7] = (byte)(checksum & 0xFF);

        return udpSegment;
    }
    
    public byte[] BuildPseudoHeader(byte[] srcIp, byte[] destIp, int udpLength, bool isIPv4)
    {
        byte[] pseudoHeader;
        if(isIPv4)
        {
            pseudoHeader = new byte[12];
            Buffer.BlockCopy(srcIp, 0, pseudoHeader, 0, 4);
            Buffer.BlockCopy(destIp, 0, pseudoHeader, 4, 4);
            pseudoHeader[8] = 0; // Zero
            pseudoHeader[9] = 17; // Protocol (UDP = 17)
            pseudoHeader[10] = (byte)(udpLength >> 8);
            pseudoHeader[11] = (byte)(udpLength & 0xFF);
        }
        else
        {
            pseudoHeader = new byte[40];
            Buffer.BlockCopy(srcIp, 0, pseudoHeader, 0, 16);
            Buffer.BlockCopy(destIp, 0, pseudoHeader, 16, 16);
            pseudoHeader[32] = (byte)((udpLength >> 24) & 0xFF);
            pseudoHeader[33] = (byte)((udpLength >> 16) & 0xFF);
            pseudoHeader[34] = (byte)((udpLength >> 8) & 0xFF);
            pseudoHeader[35] = (byte)(udpLength & 0xFF);
            pseudoHeader[36] = 0;
            pseudoHeader[37] = 0;
            pseudoHeader[38] = 0;
            pseudoHeader[39] = 17; // Next Header = UDP
        }
        return pseudoHeader;
    }
    
    private static ushort ComputeChecksum(byte[] data)
    {
        uint sum = 0;
        for (int i = 0; i < data.Length; i += 2)
        {
            ushort word = (ushort)((data[i] << 8) + (i + 1 < data.Length ? data[i + 1] : 0));
            sum += word;

            // Handle overflow by folding
            if ((sum & 0xFFFF0000) != 0)
            {
                sum = (sum & 0xFFFF) + (sum >> 16);
            }
        }

        // Final wrap-around
        while ((sum >> 16) != 0)
        {
            sum = (sum & 0xFFFF) + (sum >> 16);
        }

        return (ushort)~sum; // One's complement
    }
}