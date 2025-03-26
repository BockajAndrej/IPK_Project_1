namespace IPK_Project01;

internal class Program
{
    static void Print(int value, IPAddress address, int port, bool isTcpCon)
    {
        string protocol = isTcpCon ? "TCP" : "UDP";
        string status;
        if (value == 0)
            status = "filtered";
        else if (value == 1)
            status = "open";
        else if (value == 2)
            status = "closed";
        else if (value == 3)
            status = "wrong packet received";
        else
            status = "undefined value";
        Console.WriteLine("{0} {1} {2} {3}", address.ToString(), port, protocol, status);
    }

    private static int portIndex = 0;
    private static int portIndexInternal = 0;
    private static int portCounter = 0;
    static bool isPortInUse(in object[] objectPortarray, out int port, out bool isTcpCon)
    {
        for (; portIndex < objectPortarray.Length; portIndex++)
        {
            object portObject = objectPortarray[portIndex];
            if (portObject is int number)
            {
                if (objectPortarray[portIndex + 1] is int end_number)
                {
                    if (portCounter == end_number - number)
                    {
                        port = end_number;
                        isTcpCon = portIndex < 2;
                        portCounter = 0;
                        portIndex+=2;
                        return true;
                    }
                    while (portCounter < end_number - number)
                    {
                        port = number + portCounter;
                        isTcpCon = portIndex < 2;
                        portCounter++;
                        return true;
                    }
                }
                if (number > 0)
                {
                    port = number;
                    isTcpCon = portIndex < 2;
                    portIndex++;
                    return true;
                }
            }
            else if (portObject is int[] numbers)
            {
                while (portIndexInternal < numbers.Length)
                {
                    port = numbers[portIndexInternal];
                    isTcpCon = portIndex < 2;
                    portIndexInternal++;
                    return true;
                }
            }
        }

        port = 0;
        isTcpCon = false;
        return false;
    }
    
    static int numberOfPorts(object[] ports)
    {
        int result = 0;
        foreach (var port in ports)
        {
            if (port is int number)
                result++;
            else if (port is int[] numbers)
            {
                foreach (var num in numbers)
                {
                    result++;
                }
            }
        }
        return result;
    }
    
    private static void Main(string[] args)
    {
        string? url = null;
        string? interfaceName = null;
        int waitTime = 5000;
        object[] ports = new object[4];
        
        NetworkUtilities network = new NetworkUtilities();

        try
        {
            ArgumentProcessing argProcess = new ArgumentProcessing();
            if (argProcess.Parser(args, ref url, ref interfaceName, ref ports, ref waitTime) == false)
                return;

            IPAddress[] targetIpAddresses = network.ResolveDomain(url!);
            
            foreach (IPAddress targetIpAddress in targetIpAddresses)
            {
                bool isTcp;
                int port;
                while (isPortInUse(ports, out port, out isTcp))
                {
                    int result = network.CheckPort(targetIpAddress, interfaceName, port, isTcp, waitTime);
                    Print(result, targetIpAddress, port, isTcp);
                }
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
    }
}