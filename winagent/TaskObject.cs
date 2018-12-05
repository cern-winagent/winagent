using System;
using plugin;

namespace winagent
{
    public class TaskObject
    {
        private IInputPlugin inputPlugin;
        private IOutputPlugin outputPlugin;
        private string[] options;

        public TaskObject(IInputPlugin input, IOutputPlugin output, string[] args)
        {
            inputPlugin = input;
            outputPlugin = output;
            options = args;
        }

        public void run()
        {
            outputPlugin.Execute(inputPlugin.Execute(), options);
        }

    }
}