﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Winagent.Settings
{
    public class AutoUpdater
    {
        [JsonProperty(PropertyName = "enabled")]
        public bool Enabled { get; set; }

        [JsonProperty(PropertyName = "source")]
        public string Source { get; set; }

        [JsonProperty(PropertyName = "uri")]
        public Uri Uri { get; set; }

        [JsonProperty(PropertyName = "schedule")]
        public Schedule Schedule { get; set; }
    }
}
