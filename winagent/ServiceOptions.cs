using System;
using CommandLine;

namespace winagent
{
    [Verb("service", HelpText = "Execute the agent as a service.")]
    class ServiceOptions
    {
        // Options

        [Option("install", HelpText = "Install the service.")]
        public Boolean Install { get; set; }

        [Option("uninstall", HelpText = "Uninstall the service.")]
        public Boolean Uninstall { get; set; }

        [Option("start", HelpText = "Start the service if it's installed and stopped.")]
        public Boolean Start { get; set; }

        [Option("stop", HelpText = "Stop the service if it's installed and started.")]
        public Boolean Stop { get; set; }
    }
}
