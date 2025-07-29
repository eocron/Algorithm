using System.Linq;
using System.Reflection;
using System.Threading;

namespace Eocron.DependencyInjection.Interceptors.Caching
{
    public static class KeyProviderHelper
    {
        public static object AllExceptCancellationToken(MethodInfo methodInfo, object[] args)
        {
            return new CompoundKey(args.Where(x => x is not CancellationToken).ToList());
        }
    }
}