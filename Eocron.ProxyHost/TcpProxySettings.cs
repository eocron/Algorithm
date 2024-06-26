namespace Eocron.ProxyHost;

public class TcpProxySettings : ProxySettingsBase
{
    public string DownStreamHost { get; set; }
    public int DownStreamPort { get; set; }
    public int DownStreamBufferSize { get; set; } = 81920;
        
    public string UpStreamHost { get; set; }
    public int UpStreamPort { get; set; }
    public int UpStreamBufferSize { get; set; } = 81920;
}