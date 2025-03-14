namespace IPK_Project01;

class Program
{
    static IPAddress[] ResolveDomain(string serverUrl)
    {
        IPAddress[] addresses = Dns.GetHostAddresses(serverUrl);
        if (addresses.Length == 0)
        {
            throw new Exception("Could not resolve domain to an IP address.");
        }
        return addresses;
    }
    
    static void Main(string[] args)
    {
        string? url = null;
        object[] port = new object[4];
        
        string? interfaceName = null; 
        int waitTime = 5000;
        
        try
        {
            ArgumentProcessing argProcess = new ArgumentProcessing();
            if(argProcess.Parser(args, ref url, ref interfaceName, ref port, ref waitTime) == false)
                return;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        
        
        
        
    }
}