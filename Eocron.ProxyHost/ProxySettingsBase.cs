using System;

namespace Eocron.ProxyHost;

public class ProxySettingsBase
{
    public TimeSpan StopTimeout { get; set; } = TimeSpan.FromSeconds(5);
        
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromMinutes(1);
    
    public TimeSpan WatcherCheckInterval { get; set; } = TimeSpan.FromSeconds(1);
}