using Newtonsoft.Json;

namespace Beacons.Function
{
    public class Users
    {
        [JsonProperty(PropertyName = "id")]
        public string id { get; set; }
        public string mac { get; set; }
        public string date { get; set; }
        public string hour { get; set; }
        public string user { get; set; } 
        
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

}
