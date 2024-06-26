using System;

namespace Eocron.ProxyHost;

public class ProxySettingsBase
{
    public TimeSpan WatcherStopTimeout { get; set; } = TimeSpan.FromSeconds(5);
        
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromMinutes(1);
}