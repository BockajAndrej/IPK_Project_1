namespace ArgumentProcessing.Tests;

public class ArgumentUnitTest1
{
    private string? _url;
    private object[] _port;
    private string? _interfaceName;
    private int _waitTime;

    private IPK_Project01.ArgumentProcessing _ap;

    public ArgumentUnitTest1()
    {
        _url = null;
        _port = new object[4];
        _interfaceName = null; 
        _waitTime = 0;
        
        _ap = new IPK_Project01.ArgumentProcessing();

    }
    
    [Fact]
    public void CorrectContinueTest1()
    {
        Assert.True(_ap.Parser(new [] { "./ipk-l4-scan", "-i", "eth0", "-u", "53,67", "www.vutbr.cz" }, ref _url, ref _interfaceName, ref _port, ref _waitTime));
    }
    
    [Fact]
    public void CorrectStopTest2()
    {
        Assert.False(_ap.Parser(new [] { "./ipk-l4-scan", "-i",  "-u", "53,67", "www.vutbr.cz" }, ref _url, ref _interfaceName, ref _port, ref _waitTime));
    }
    [Fact]
    public void IncorrectTest3()
    {
        Assert.Throws<Exception>(() => _ap.Parser(new [] { "./ipk-l4-scan", "-i", "eth0", "-u", "53,67", "www.vutbr.cz", "123" }, ref _url, ref _interfaceName, ref _port, ref _waitTime));
        Assert.Throws<Exception>(() => _ap.Parser(new [] { "./ipk-l4-scan", "-i", "eth0", "-u", "53,67", "www.vutbr.cz", "-i" ,"123" }, ref _url, ref _interfaceName, ref _port, ref _waitTime));
    }
}