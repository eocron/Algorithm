using System;

namespace Eocron.ProxyHost
{
    public class TcpProxySettings : ProxySettingsBase
    {
        public string RemoteServerHostNameOrAddress { get; set; }
        
        public int RemoteServerPort { get; set; }

        public int BufferSize { get; set; } = 81926;
        
        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromMinutes(1);
        public string LocalIpAddress { get; set; }
        public int LocalPort { get; set; }
    }
}