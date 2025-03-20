using System.Net.NetworkInformation;

namespace IPK_Project01;

public class ArgumentProcessing
{
    /// <summary>
    /// Function parse arguments 
    /// </summary>
    /// <param name="args"></param>
    /// <param name="url"></param>
    /// <param name="interfaceName"></param>
    /// <param name="port"></param>
    /// <param name="waitTime"></param>
    /// <returns></returns> Return false when program should be stopped otherwise true
    /// <exception cref="Exception"></exception> Intern error, Argument error (user made mistake)
    public bool Parser(string[] args, ref string? url, ref string? interfaceName, ref object[] port, ref int waitTime)
    {
        //args = args.Skip(1).ToArray();
        
        int argState = 0;
        foreach (string arg in args)
        {
            // Parse arguments
            if (argState == 0)
            {
                if (arg == "-h" || arg == "--help")
                {
                    PrintUsage();
                    return false;
                }
                if(arg == "-i" || arg == "--interface")
                    argState = 1;
                else if(arg == "-t" || arg == "--pt")
                    argState = 2;
                else if(arg == "-u" || arg == "--pu")
                    argState = 3;
                else if (arg == "-w" || arg == "--wait")
                    argState = 4;
                else
                {
                    if (url != null)
                        throw new Exception("Argument error: multiple server URLs");
                    url = arg;
                }
            }
            else
            {
                if (arg.StartsWith("-"))
                    break;
                switch (argState)
                {
                    case 1:
                        if(interfaceName != null)
                            throw new Exception("Argument error: multiple server interfaces");
                        interfaceName = GetInterface(arg);
                        break;
                    case 2 or 3:
                        int index = argState == 2 ? 0 : 2;
                        if(port[index] != null)
                            throw new Exception("Argument error: multiple server ports");
                        if (arg.Contains("-"))
                        {
                            int num;
                            int.TryParse(arg.Split("-")[0], out num);
                            port[index] = num;
                            int.TryParse(arg.Split("-")[1], out num);
                            port[index + 1] = num;
                        }
                        else if (arg.Contains(","))
                        {
                            int[] strArray = new int[arg.Split(",").Length];
                            int i = 0;
                            foreach (string currPort in arg.Split(","))
                                strArray[i++] = int.Parse(currPort);
                            port[index] = strArray;
                        }
                        else
                        {
                            int num;
                            int.TryParse(arg, out num);
                            port[index] = num;
                        }
                        break;
                    case 4:
                        waitTime = int.Parse(arg);
                        if(waitTime <= 0)
                            throw new Exception("Argument error: wait time must be positive integer");
                        break;
                    default:
                        throw new Exception("Internal error: invalid argument state");
                }
                argState = 0;
            }
        }
        
        foreach (object o in port)
        {
            if (o is int num) // Ak je to jedno číslo
            {
                if(num <= 0 || num >= 65536)
                    throw new Exception("Argument error: port must be positive integer and less than 65536");
            }
            else if (o is int[] numbers) // Ak je to pole čísel
            {
                foreach (int n in numbers)
                {
                    if(n <= 0 || n >= 65536)
                        throw new Exception("Argument error: port must be positive integer and less than 65536");
                }
            }
        }

        if (url == null || interfaceName == null || (port[0] == null && port[2] == null))
        {
            PrintAssessibleInterfaces();
            return false;
        }
        return true;
    }

    void PrintUsage()
    {
        Console.WriteLine("Usage: IPK_Project01.exe [options] <server_url>");
        Console.WriteLine("Options:");
        Console.WriteLine("  -i, --interface <interface>        Use the specified interface");
        Console.WriteLine("  -t, --pt <TCP - port>              Use the specified port");
        Console.WriteLine("  -u, --pu <UDP - port>              Use the specified protocol");
        Console.WriteLine("  -w, --wait <seconds>               Wait for the specified number of seconds");
        Console.WriteLine("  -h, --help                         Show this help message");
    }
    
    void PrintAssessibleInterfaces()
    {
        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            Console.WriteLine($"Name: {ni.Name}");
            Console.WriteLine($"Description: {ni.Description}");
            Console.WriteLine($"Type: {ni.NetworkInterfaceType}");
            Console.WriteLine($"Status: {ni.OperationalStatus}");
            Console.WriteLine($"MAC Address: {ni.GetPhysicalAddress()}");
            Console.WriteLine(new string('-', 40));
        }
    }

    string GetInterface(string interfaceName)
    {
        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.Name == interfaceName)
            {
                return ni.Name;
            }
        }
        throw new Exception("Interface not found");
    }
}