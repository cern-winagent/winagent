﻿using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Threading;
using System.ServiceProcess;
using System.Diagnostics;

using plugin;

namespace winagent
{
    class Agent
    {
        static JObject config;

        /// <summary>List to keep a reference of each task
        /// <para>Keeping a reference avoid the timers to be garbage collected</para>
        /// <seealso cref="https://stackoverflow.com/questions/18136735/can-timers-get-automatically-garbage-collected"/>
        /// </summary>
        static List<Timer> timersReference;

        #region Nested class to support running as service
        public class Service : ServiceBase
        {
            public Service()
            {
                ServiceName = "Winagent";

                // Set current directory as base directory
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            }


            protected override void OnStart(string[] args)
            {
                try
                {
                    // Create reference List
                    timersReference = new List<Timer>();

                    // Load plugins after parse options
                    List<PluginDefinition> pluginList = Agent.LoadPlugins();

                    // Read config file
                    config = JObject.Parse(File.ReadAllText(@"config.json"));

                    foreach (JProperty input in ((JObject)config["input"]).Properties())
                    {
                        PluginDefinition inputPluginMetadata = pluginList.Where(t => ((PluginAttribute)t.Attribute).PluginName.ToLower() == input.Name.ToLower()).First();
                        IInputPlugin inputPlugin = Activator.CreateInstance(inputPluginMetadata.ImplementationType) as IInputPlugin;

                        foreach (JProperty output in ((JObject)input.Value).Properties())
                        {
                            PluginDefinition outputPluginMetadata = pluginList.Where(t => ((PluginAttribute)t.Attribute).PluginName.ToLower() == output.Name.ToLower()).First();
                            IOutputPlugin outputPlugin = Activator.CreateInstance(outputPluginMetadata.ImplementationType) as IOutputPlugin;

                            // To pass multiple objects to the timer
                            // It is necesary to create a custom object containing the others
                            TaskObject task = new TaskObject(inputPlugin, outputPlugin, output.Value["options"].ToObject<string[]>());

                            Timer timer = new Timer(new TimerCallback(ExecuteTask), task, 0, CalculateTime(
                                output.Value["hours"].ToObject<int>(),
                                output.Value["minutes"].ToObject<int>(),
                                output.Value["seconds"].ToObject<int>()
                            ));

                            // Save reference to avoid GC
                            Agent.timersReference.Add(timer);
                        }
                    }

                    // Run the updater after 1 minute
                    // TODO: Put interval from config
                    // The timer will run every 10 mins
                    Timer updaterTimer = new Timer(new TimerCallback(RunUpdater), null, 60000, CalculateTime(0, 2, 0));
                    // Save reference to avoid GC
                    Agent.timersReference.Add(updaterTimer);
                }
                catch (Exception e)
                {
                    // EventID 1 => "tmp" Directory not found
                    using (EventLog eventLog = new EventLog("Application"))
                    {
                        System.Text.StringBuilder message = new System.Text.StringBuilder("An error ocurred in the winagent service:");
                        message.Append(Environment.NewLine);
                        message.Append(e.ToString());

                        eventLog.Source = "Winagent";
                        eventLog.WriteEntry(message.ToString(), EventLogEntryType.Error, 0, 1);
                    }
                }
            }
        }
        #endregion


        // Load plugin assemblies
        public static List<PluginDefinition> LoadPlugins()
        {
            List<PluginDefinition> pluginList = new List<PluginDefinition>();

            foreach (String path in Directory.GetFiles("plugins"))
            {
                Assembly plugin = Assembly.LoadFrom(path);
                pluginList.AddRange(LoadAllClassesImplementingSpecificAttribute<PluginAttribute>(plugin));
            }

            return pluginList;
        }

        
        // Calculates the iteration time based in the config file
        public static int CalculateTime(int hours, int minutes, int seconds)
        {
            return hours * 3600000 + minutes * 60000 + seconds * 1000;
        }

        // Execute updater
        public static void RunUpdater(object state)
        {
            try
            {
                // Create detached autoupdater if autoupdates are enabled and it doesn't exists
                if (Boolean.Parse(config.GetValue("autoupdates").ToString()) && Process.GetProcessesByName("winagent-updater").Length < 1)
                {
                    Process.Start(@"winagent-updater.exe");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                // EventID 2 => Error executing updater
                using (EventLog eventLog = new EventLog("Application"))
                {
                    System.Text.StringBuilder message = new System.Text.StringBuilder("An error ocurred executing updater:");
                    message.Append(Environment.NewLine);
                    message.Append(e.ToString());

                    eventLog.Source = "Winagent";
                    eventLog.WriteEntry(message.ToString(), EventLogEntryType.Error, 0, 2);
                }
            }
        }

        // Executes a task
        public static void ExecuteTask(object state)
        {
            try
            {
                ((TaskObject)state).Execute();
            }
            catch(Exception e)
            {
                Console.WriteLine(e);

                // EventID 2 => Error executing plugin
                using (EventLog eventLog = new EventLog("Application"))
                {
                    System.Text.StringBuilder message = new System.Text.StringBuilder("An error ocurred executing a plugin:");
                    message.Append(Environment.NewLine);
                    message.Append(e.ToString());

                    eventLog.Source = "Winagent";
                    eventLog.WriteEntry(message.ToString(), EventLogEntryType.Error, 0, 2);
                }
            }
        }

        // Non-service execution
        public static void ExecuteConfig(string path = @"config.json")
        {
            // Set current directory as base directory
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            // Create reference List
            timersReference = new List<Timer>();

            // Load plugins after parse options
            List<PluginDefinition> pluginList = Agent.LoadPlugins();

            // Read config file
            config = JObject.Parse(File.ReadAllText(path));

            foreach (JProperty input in ((JObject)config["input"]).Properties())
            {
                PluginDefinition inputPluginMetadata = pluginList.Where(t => ((PluginAttribute)t.Attribute).PluginName.ToLower() == input.Name.ToLower()).First();
                IInputPlugin inputPlugin = Activator.CreateInstance(inputPluginMetadata.ImplementationType) as IInputPlugin;

                foreach (JProperty output in ((JObject)input.Value).Properties())
                {
                    PluginDefinition outputPluginMetadata = pluginList.Where(t => ((PluginAttribute)t.Attribute).PluginName.ToLower() == output.Name.ToLower()).First();
                    IOutputPlugin outputPlugin = Activator.CreateInstance(outputPluginMetadata.ImplementationType) as IOutputPlugin;

                    TaskObject task = new TaskObject(inputPlugin, outputPlugin, output.Value["options"].ToObject<string[]>());

                    Timer timer = new Timer(new TimerCallback(ExecuteTask), task, 0, CalculateTime(
                        output.Value["hours"].ToObject<int>(),
                        output.Value["minutes"].ToObject<int>(),
                        output.Value["seconds"].ToObject<int>()
                    ));

                    // Save reference to avoid GC
                    Agent.timersReference.Add(timer);
                }
            }
        }


        // Selects the specified plugin and executes it   
        public static void ExecuteCommand(String[] inputs, String[] outputs, String[] options)
        {
            // Set current directory as base directory
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            // Load plugins after parse options
            List<PluginDefinition> pluginList = Agent.LoadPlugins();

            foreach (String input in inputs)
            {
                PluginDefinition inputPluginMetadata = pluginList.Where(t => ((PluginAttribute)t.Attribute).PluginName.ToLower() == input.ToLower()).First();
                IInputPlugin inputPlugin = Activator.CreateInstance(inputPluginMetadata.ImplementationType) as IInputPlugin;
                string inputResult = inputPlugin.Execute();

                foreach (String output in outputs)
                {
                    PluginDefinition outputPluginMetadata = pluginList.Where(t => ((PluginAttribute)t.Attribute).PluginName.ToLower() == output.ToLower()).First();

                    IOutputPlugin outputPlugin = Activator.CreateInstance(outputPluginMetadata.ImplementationType) as IOutputPlugin;
            
                    outputPlugin.Execute(inputResult, options);
                }
            }
        }


        // Selects the classes with the specified attribute
        public static List<PluginDefinition> LoadAllClassesImplementingSpecificAttribute<T>(Assembly assembly)
        {
            IEnumerable<Type> typesImplementingAttribute = GetTypesWithSpecificAttribute<T>(assembly);

            List<PluginDefinition> attributeList = new List<PluginDefinition>();
            foreach (Type type in typesImplementingAttribute)
            {
                var attribute = type.GetCustomAttribute(typeof(T));
                attributeList.Add(new PluginDefinition(attribute, type));
            }

            return attributeList;
        }


        // Selects the types of the classes with the specified attribute
        private static IEnumerable<Type> GetTypesWithSpecificAttribute<T>(Assembly assembly)
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (type.GetCustomAttributes(typeof(T), true).Length > 0)
                {
                    yield return type;
                }
            }
        }

    }
}
