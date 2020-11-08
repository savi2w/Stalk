﻿using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Threading;

namespace Stalk
{
    class Program
    {
        static void Main(string[] args)
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);

            if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                Console.WriteLine("Elevating permissions...");

                using (Process execute = new Process())
                {
                    execute.StartInfo.FileName = "Akagi.exe";
                    execute.StartInfo.Arguments = "61 " + Process.GetCurrentProcess().MainModule.FileName;

                    execute.Start();
                    execute.WaitForExit();

                    Environment.Exit(0);
                }
            }

            Proxy proxy = new Proxy();

            proxy.Start();
            Console.WriteLine("Executing on 127.0.0.1:1219");

            Console.ReadLine();

            proxy.Stop();
            Console.WriteLine("Stopping...");

            Thread.Sleep(1024);
        }
    }
}
