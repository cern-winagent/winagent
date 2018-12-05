using System;
using Newtonsoft.Json.Linq;


namespace plugin
{
    [PluginAttribute(PluginName="HeartBeat")]
    public class IHeartBeat : IInputPlugin
    {
        public string Execute()
        {
            return "{\"heartbeat\": 1}";
        }
    }
}