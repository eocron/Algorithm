namespace Eocron.ProxyHost;

public interface IConnectionWatcher
{
    void Watch(IProxyConnection connection);
}