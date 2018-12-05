using System.Collections.Generic;
using CommandLine;

namespace winagent
{
    [Verb("run", HelpText = "Run a specific configuration command.")]
    class CommandOptions
    {
        // Options

        [Option('i', "input", Separator = ',', Default = new string[] { "updates" }, HelpText = "Input plugins separated by comma.")]
        public IEnumerable<string> Input { get; set; }

        [Option('o', "output", Separator = ',', Default = new string[] { "console" }, HelpText = "Output plugins separated by comma.")]
        public IEnumerable<string> Output { get; set; }
    }
}
