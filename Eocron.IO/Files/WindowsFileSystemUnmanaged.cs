using System;
using System.Runtime.InteropServices;

namespace Eocron.IO.Files
{
    public static class WindowsFileSystemUnmanaged
    {
        public static bool IsSupported => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        
        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode )]
        public static extern bool CreateHardLink(
            string lpFileName,
            string lpExistingFileName,
            IntPtr lpSecurityAttributes
        );
    }
}