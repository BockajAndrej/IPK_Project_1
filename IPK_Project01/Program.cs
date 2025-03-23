using System.Security.Principal;

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
        else
            status = "undefined value";
        Console.WriteLine("{0} {1} {2} {3}", address.ToString(), port, protocol, status);
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

            IPAddress[] addresses = network.ResolveDomain(url!);
            
            foreach (IPAddress address in addresses)
            {
                if(address.AddressFamily != AddressFamily.InterNetwork)
                    continue;
                int portIndex = 0;
                foreach (object p in port)
                {
                    int result = 0;
                    if (p is int number) // Ak je to jedno číslo
                    {
                        if(number > 0)
                        {
                            result = network.CheckPort(portIndex<2, address, interfaceName, waitTime,number);
                            Print(result, address, result, (portIndex < 1));                        }
                    }
                    else if (p is int[] numbers) // Ak je to pole čísel
                    {
                        foreach (int num in numbers)
                        {
                            result = network.CheckPort(portIndex<2, address, interfaceName, waitTime,num);
                            Print(result, address, result, (portIndex < 1));
                        }
                    }
                    portIndex++;
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