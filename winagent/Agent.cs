using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Threading;

using plugin;

namespace winagent
{
    class Agent
    {
        static JObject config;

        static void Main(string[] args)
        {
            Assembly consoleAssembly = Assembly.GetExecutingAssembly();
            List<PluginDefinition> pluginList = LoadAllClassesImplementingSpecificAttribute<PluginAttribute>(consoleAssembly);

            // Console.WriteLine("Available Plugins:");
            // pluginList.ForEach(t => Console.WriteLine(((PluginAttribute)t.Attribute).PluginName));

            if (args[0] == "service")
            {
                //TODO: Change absolute path
                config = JObject.Parse(File.ReadAllText(@"C:\Users\epuentes\Projects\nframework\winagent\winagent\config.json"));
                ExecuteService(pluginList);
            }
            else
            {
                ExecutePlugin(pluginList, "Updates", "Console", args);
            }

            // Prevents the test console from closing itself
            Console.ReadKey();
        }

        // Executes the windows service 
        public static void ExecuteService(List<PluginDefinition> plugins)
        {
            foreach (JProperty input in ((JObject) config["input"]).Properties())
            {
                PluginDefinition inputPluginMetadata = plugins.Where(t => ((PluginAttribute)t.Attribute).PluginName == input.Name).First();
                IInputPlugin inputPlugin = Activator.CreateInstance(inputPluginMetadata.ImplementationType) as IInputPlugin;

                foreach (JProperty output in ((JObject)input.Value).Properties())
                {
                    PluginDefinition outputPluginMetadata = plugins.Where(t => ((PluginAttribute)t.Attribute).PluginName == output.Name).First();
                    IOutputPlugin outputPlugin = Activator.CreateInstance(outputPluginMetadata.ImplementationType) as IOutputPlugin;

                    TaskObject task = new TaskObject(inputPlugin, outputPlugin, output.Value["args"].ToObject<string[]>());
                    
                    Timer testimer = new Timer(ExecuteTask, task, 1000, CalculateTime());
                    
                    outputPlugin.Execute(inputPlugin.Execute(), output.Value["args"].ToObject<string[]>());
                }
            }
                            
        }

        // Executes a task
        public static int CalculateTime()
        {
            return 1000;
        }


        // Executes a task
        public static void ExecuteTask(object state)
        {
            
        }


        // Selects the specified plugin and executes it   
        public static void ExecutePlugin(List<PluginDefinition> plugins, string input, string output, string[] options)
        {
            PluginDefinition inputPluginMetadata = plugins.Where(t => ((PluginAttribute)t.Attribute).PluginName == input).First();
            PluginDefinition outputPluginMetadata = plugins.Where(t => ((PluginAttribute)t.Attribute).PluginName == output).First();

            IInputPlugin inputPlugin = Activator.CreateInstance(inputPluginMetadata.ImplementationType) as IInputPlugin;
            IOutputPlugin outputPlugin = Activator.CreateInstance(outputPluginMetadata.ImplementationType) as IOutputPlugin;
            
            outputPlugin.Execute(inputPlugin.Execute(), options);
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
