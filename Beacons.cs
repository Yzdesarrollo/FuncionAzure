using Newtonsoft.Json;

namespace Beacons.Function
{
    public class Beacons
    {
        [JsonProperty(PropertyName = "id")]
        public string address { get; set; }
        public int klass { get; set; }
        public string mac { get; set; }
        public string name { get; set; }
        
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
