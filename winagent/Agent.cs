﻿using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using System.ServiceProcess;
using System.Diagnostics;

using plugin;

namespace winagent
{
    class Agent
    {
        static JObject config;

        // TODO: This is a reference to avoid the timer removal (garbage collector), it needs to be changed
        static Timer timer;

        #region Nested class to support running as service
        public class Service : ServiceBase
        {
            // Thread control
            private ManualResetEvent _shutdownEvent = new ManualResetEvent(false);
            private Thread _thread;

            public Service()
            {
                ServiceName = "Winagent";
            }

            protected override void OnStart(string[] args)
            {
                _thread = new Thread(Excecution);
                _thread.Name = "Winagent thread";
                _thread.IsBackground = true;
                _thread.Start();
            }

            protected override void OnStop()
            {
                // Set flag to finalize thread
                _shutdownEvent.Set();

                // Give the thread 3 seconds to stop
                if (!_thread.Join(3000))
                { 
                    _thread.Abort();
                }
            }

            private void Excecution()
            {
                // Checks if the thread should continue
                while (!_shutdownEvent.WaitOne(0))
                {
                    ExecuteService();
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

        // Executes the windows service 
        public static void ExecuteService()
        {
            // Set current directory as base directory
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            // Load plugins after parse options
            List<PluginDefinition> pluginList = Agent.LoadPlugins();

            // Read config file
            config = JObject.Parse(File.ReadAllText(@"config.json"));

            foreach (JProperty input in ((JObject) config["input"]).Properties())
            {
                PluginDefinition inputPluginMetadata = pluginList.Where(t => ((PluginAttribute)t.Attribute).PluginName.ToLower() == input.Name.ToLower()).First();
                IInputPlugin inputPlugin = Activator.CreateInstance(inputPluginMetadata.ImplementationType) as IInputPlugin;

                foreach (JProperty output in ((JObject)input.Value).Properties())
                {
                    PluginDefinition outputPluginMetadata = pluginList.Where(t => ((PluginAttribute)t.Attribute).PluginName.ToLower() == output.Name.ToLower()).First();
                    IOutputPlugin outputPlugin = Activator.CreateInstance(outputPluginMetadata.ImplementationType) as IOutputPlugin;

                    TaskObject task = new TaskObject(inputPlugin, outputPlugin, output.Value["options"].ToObject<string[]>());
                    
                    timer = new Timer(
                        new TimerCallback(ExecuteTask), 
                        task, 
                        0, 
                        CalculateTime(
                            output.Value["hours"].ToObject<int>(), 
                            output.Value["minutes"].ToObject<int>(), 
                            output.Value["seconds"].ToObject<int>()
                        )
                    );
                }
            }
        }

        // Calculates the iteration time based in the config file
        public static int CalculateTime(int hours, int minutes, int seconds)
        {
            return hours * 3600000 + minutes * 60000 + seconds * 1000;
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
            }
        }


        // Selects the specified plugin and executes it   
        public static void ExecuteCommand(String[] inputs, String[] outputs, String[] options = null)
        {
            //options = new String[] { "epuentes-rabbitmq", "test", "test", "pcitcda30" };
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
