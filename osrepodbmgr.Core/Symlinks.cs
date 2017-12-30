using System;
using System.Runtime.InteropServices;
using System.Text;

namespace osrepodbmgr.Core
{
    public static class Symlinks
    {
        [DllImport("libc", CharSet = CharSet.Ansi, SetLastError = true)]
        static extern int readlink(string path, IntPtr buf, int bufsize);

        public static string ReadLink(string path)
        {
            IntPtr buf = Marshal.AllocHGlobal(16384);

            int ret = readlink(path, buf, 16384);

            if(ret < 0) return null;

            byte[] target = new byte[ret];
            Marshal.Copy(buf, target, 0, ret);

            return Encoding.UTF8.GetString(target);
        }

        [DllImport("libc", CharSet = CharSet.Ansi, SetLastError = true)]
        static extern int symlink(string target, string path);

        public static int Symlink(string target, string path)
        {
            return symlink(target, path);
        }
    }
}