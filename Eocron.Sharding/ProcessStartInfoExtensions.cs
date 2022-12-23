using System;
using System.Diagnostics;
using System.Text;

namespace Eocron.Sharding
{
    public static class ProcessStartInfoExtensions
    {
        public static ProcessStartInfo ConfigureAsService(this ProcessStartInfo info, Encoding outputEncoding = null)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            outputEncoding = outputEncoding ?? Encoding.UTF8;
            info.CreateNoWindow = true;
            info.ErrorDialog = false;
            info.WindowStyle = ProcessWindowStyle.Hidden;
            info.RedirectStandardError = true;
            info.RedirectStandardInput = true;
            info.RedirectStandardOutput = true;
            info.UseShellExecute = false;
            info.StandardErrorEncoding = outputEncoding;
            info.StandardOutputEncoding = outputEncoding;
            return info;
        }
    }
}