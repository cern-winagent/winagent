using System;
using CommandLine;
using System.ServiceProcess;

namespace winagent
{
    class EntryPoint
    {
        //static JObject config;

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
                using (var service = new Agent.Service())
                {
                    ServiceBase.Run(service);
                }
            }

            
        }

        // Overloaded Main method with parsed options
        static void Command(CommandOptions options)
        {
            Agent.ExecuteCommand((String[])options.Input, (String[])options.Output, new String[] { "table" });
        }

        // Overloaded Main method with parsed options
        static void Service(ServiceOptions options)
        {
            if (options.Install)
            {
                ServiceManager.Install();
            }
            else if (options.Uninstall)
            {
                ServiceManager.Uninstall();
            }
            else if (options.Start)
            {
                ServiceManager.Start();
            }
            else if (options.Stop)
            {
                ServiceManager.Stop();
            }
        }
    }
}
