using System.Diagnostics;

namespace Eocron.Sharding
{
    public interface IProcessStateProvider
    {
        /// <summary>
        /// This method is for checking if process is healthy and ready to serve messages.
        /// Called frequently.
        /// </summary>
        /// <param name="process"></param>
        /// <returns>True - if process ready to process messages</returns>
        bool IsReadyForPublish(Process process);
    }
}