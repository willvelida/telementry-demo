using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace TelementryApp.Models
{
    public class DeviceReading
    {
        [JsonProperty("id")]
        public string DeviceId { get; set; }
        public decimal DamageTemperature { get; set; }
        public string DamageLevel { get; set; }
        public int DeviceAgeInDays { get; set; }
    }
}
