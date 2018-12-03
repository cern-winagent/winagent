﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using plugin;
using Newtonsoft.Json.Linq;
using CommandLine;
using System.IO;
using System.ServiceProcess;

namespace winagent
{
    class EntryPoint
    {
        //static JObject config;

        // Entrypoint
        static void Main(string[] args)
        {
            // Parse CommandLine options
            // https://github.com/commandlineparser/commandline
            var options = Parser.Default.ParseArguments<Options>(args);

            // Call to overloaded Main method
            options.WithParsed(opts => Main(opts));
        }

        // Overloaded Main method with parsed options
        static void Main(Options options)
        {
            if (Environment.UserInteractive)
            {
                if (options.Service)
                {
                    SrvInstaller.Install(new string[] { });
                }
                else
                {
                    Agent.ExecuteCommand((String[])options.Input, (String[])options.Output, new String[] { "table" });
                }
            }
            else
            {
                using (var service = new Agent.Service())
                {
                    ServiceBase.Run(service);
                }
            }

            // Prevents the test console from closing itself
            // Console.ReadKey();
        }
    }
}
