namespace Eocron.ProxyHost
{
    public class TcpProxyBuilder : IProxyBuilder
    {
        public IProxy Build()
        {
            return new TcpProxy();
        }
    }
}