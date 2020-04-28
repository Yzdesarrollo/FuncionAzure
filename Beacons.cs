using Newtonsoft.Json;

namespace Beacons.Function
{
    public class Beacons
    {
        [JsonProperty(PropertyName = "id")]
        public string uuid { get; set; }
        public string mac { get; set; }
        public int major { get; set; }
        public int minor { get; set; }
        public string message { get; set; } 
        
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

}
