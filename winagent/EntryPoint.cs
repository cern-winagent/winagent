﻿using System;
using CommandLine;
using System.ServiceProcess;

using Winagent.Options;

namespace Winagent
{
    class EntryPoint
    {
        // Entrypoint
        static void Main(string[] args)
        {

            if (Environment.UserInteractive)
            {
                // Parse CommandLine options
                // https://github.com/commandlineparser/commandline
                var options = Parser.Default.ParseArguments<CommandOptions, ServiceOptions>(args);

                // Call the right method
                options.WithParsed<CommandOptions>(opts => Command(opts));
                options.WithParsed<ServiceOptions>(opts => Service(opts));
            }
            else
            {
                using (var service = new Service())
                {
                    ServiceBase.Run(service);
                }
            }

        }

        // Command execution with parsed options
        static void Command(CommandOptions options)
        {
            // TODO: Specify config file
            if (options.ConfigFile != null)
            {
                // Execute with config
                CLI.ExecuteConfig(options.ConfigFile);
                Console.Error.WriteLine("Press any key to exit.");
                Console.ReadKey();
            }
            else
            {
                CLI.ExecuteCommand(options.Input, options.Output, (string[])options.InputOptions, (string[])options.OutputOptions);
            }
        }

        // Service management with parsed options
        static void Service(ServiceOptions options)
        {
            if (options.Install)
            {
                ServiceManager.Setup(ServiceManager.SetupOperation.Install);
            }
            else if (options.Uninstall)
            {
                ServiceManager.Setup(ServiceManager.SetupOperation.Uninstall);
            }
            else if (options.Start)
            {
                ServiceManager.ExecuteOperation(ServiceManager.ServiceOperation.Start);
            }
            else if (options.Stop)
            {
                ServiceManager.ExecuteOperation(ServiceManager.ServiceOperation.Stop);
            }
            else if (options.Restart)
            {
                ServiceManager.ExecuteOperation(ServiceManager.ServiceOperation.Restart);
            }
            else if (options.Status)
            {
                ServiceManager.ExecuteOperation(ServiceManager.ServiceOperation.Status);
            }
        }
    }
}
