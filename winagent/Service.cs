﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using plugin;
using Winagent.MessageHandling;
using Winagent.Options;

namespace Winagent
{
    class Service : ServiceBase
    {
        private const string AgentName = "Winagent";
        private const string Updater = @"winagent-updater.exe";
        private string[] serviceArgs;

        /// <summary>
        /// Service constructor without arguments
        /// </summary>
        public Service()
        {
            ServiceName = AgentName;

            // Set current directory as base directory
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
        }

        /// <summary>
        /// Service constructor with arguments
        /// </summary>
        /// <param name="args">Arguments used to execute the service comming from the installation "assemblypath"</param>
        public Service(string[] args)
        {
            ServiceName = AgentName;
            serviceArgs = args;

            // Set current directory as base directory
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
        }

        /// <summary>
        /// When implemented in a derived class, executes when a Start command is sent to
        /// the service by the Service Control Manager (SCM) or when the operating system
        /// starts (for a service that starts automatically). Specifies actions to take when
        /// the service starts.
        /// </summary>
        /// <param name="args">Arguments specified during the service start</param>
        protected override void OnStart(string[] args)
        {
            if (args.Length == 0 && serviceArgs != null)
            {
                args = serviceArgs;
            }

            try
            {
                // Parse CommandLine options
                // https://github.com/commandlineparser/commandline
                var options = Parser.Default.ParseArguments<ServiceOptions>(args);

                // Call the right method
                options.WithParsed<ServiceOptions>(opts => Start(opts));
            }
            catch (Exception e)
            {
                // EventID 1 => An error ocurred
                MessageHandler.HandleError(String.Format("General error during service execution."), 1, e);
                throw;
            }
        }

        /// <summary>
        /// Executes the service logic occording to the specified options
        /// </summary>
        /// <param name="options">Parsed options</param>
        private void Start(ServiceOptions options)
        {
            // Get application settings
            Settings.Agent settings = Agent.GetSettings(options.Config);

            // Create envent handlers
            Agent.SetEventReaders(settings.EventLogs);

            // Create tasks
            Agent.CreateTasks(settings.InputPlugins);

            // Create detached autoupdater if autoupdates are enabled
            if (settings.AutoUpdates.Enabled)
            {
                // Run the updater after 1 minute
                // The timer will run every 10 mins
                Timer updaterTimer = new Timer(new TimerCallback(RunUpdater), null, 60000, settings.AutoUpdates.Schedule.GetTime());
                // Save reference to avoid GC
                Agent.timersReference.Add(updaterTimer);
            }
        }

        /// <summary>
        /// Execute updater
        /// </summary>
        /// <param name="state">State object passed to the timer</param>
        /// <exception cref="UpdaterNotFoundException">Thrown when the executable of the updater does not exist</exception>
        /// <exception cref="Exception">General exception when the updater is executed</exception>
        internal static void RunUpdater(object state)
        {
            try
            {
                var tmpLocation = @".\tmp\" + Updater;

                // If there is a new version of the updater in .\tmp\ copy it
                if (File.Exists(tmpLocation))
                {
                    File.Copy(tmpLocation, Updater, true);
                    File.Delete(tmpLocation);

                    // EventID 3 => Application updated
                    MessageHandler.HandleInformation(String.Format("Application updated: \"{0}\".", Updater), 3);
                }

                if (File.Exists(Updater))
                {
                    Process.Start(Updater);
                }
                else
                {
                    throw new Exceptions.UpdaterNotFoundException("Could not find the updater.");
                }
            }
            catch (Exceptions.UpdaterNotFoundException unfe)
            {
                // EventID 12 => Could not find the updater executable
                MessageHandler.HandleError("An error ocurred while executing the updater.", 12, unfe);
            }
            catch (Exception e)
            {
                // EventID 2 => Error executing updater
                MessageHandler.HandleError("An error ocurred while executing the updater.", 2, e);
            }
        }

    }
}
