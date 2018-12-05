using System;
using ConsoleTableExt;
using System.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace plugin
{
    [PluginAttribute(PluginName="Console")]
    public class OConsole : IOutputPlugin {
        public void Execute(string json, string[] options)
        {
            switch (options[0])
            {
                case "table":
                    JObject jsonUpdatesInfo = JObject.Parse(json);

                    foreach (var property in jsonUpdatesInfo)
                    {
                        Console.WriteLine();
                        Console.Write(property.Key + ": ");
                        if (property.Value.HasValues)
                        {
                            Console.WriteLine();
                            DataTable datatable = JsonConvert.DeserializeObject<DataTable>(property.Value.ToString());
                            ConsoleTableBuilder.From(datatable).WithFormat(ConsoleTableBuilderFormat.Minimal).ExportAndWriteLine();
                        }
                        else
                        {
                            Console.WriteLine(property.Value);
                        }
                    }
                    break;
                case "json":
                case "default":
                    Console.Write(json);
                    break;
            }
        }
    }
}