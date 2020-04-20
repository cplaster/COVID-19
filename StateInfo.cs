using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COVID_19.QuickType.StateInfo
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public partial class StateInfo
    {
        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("covid19SiteOld")]
        public Uri Covid19SiteOld { get; set; }

        [JsonProperty("covid19Site")]
        public Uri Covid19Site { get; set; }

        [JsonProperty("covid19SiteSecondary")]
        public Uri Covid19SiteSecondary { get; set; }

        [JsonProperty("twitter")]
        public string Twitter { get; set; }

        [JsonProperty("pui")]
        public Pui Pui { get; set; }

        [JsonProperty("pum")]
        public bool Pum { get; set; }

        [JsonProperty("notes", NullValueHandling = NullValueHandling.Ignore)]
        public string Notes { get; set; }

        [JsonProperty("fips")]
        public string Fips { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public enum Pui { AllData, NoData, OnlyPositives, PositivesNegatives, PositivesOnly, PuiAllData, PuiNoData };

    public partial class StateInfo
    {
        public static List<StateInfo> FromJson(string json) => JsonConvert.DeserializeObject<List<StateInfo>>(json, QuickType.StateInfo.Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this List<StateInfo> self) => JsonConvert.SerializeObject(self, QuickType.StateInfo.Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                PuiConverter.Singleton,
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }

    internal class PuiConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(Pui) || t == typeof(Pui?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            switch (value)
            {
                case "All Data":
                    return Pui.PuiAllData;
                case "All data":
                    return Pui.AllData;
                case "No Data":
                    return Pui.PuiNoData;
                case "No data":
                    return Pui.NoData;
                case "Only positives":
                    return Pui.OnlyPositives;
                case "Positives + Negatives":
                    return Pui.PositivesNegatives;
                case "Positives Only":
                    return Pui.PositivesOnly;
            }
            throw new Exception("Cannot unmarshal type Pui");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (Pui)untypedValue;
            switch (value)
            {
                case Pui.PuiAllData:
                    serializer.Serialize(writer, "All Data");
                    return;
                case Pui.AllData:
                    serializer.Serialize(writer, "All data");
                    return;
                case Pui.PuiNoData:
                    serializer.Serialize(writer, "No Data");
                    return;
                case Pui.NoData:
                    serializer.Serialize(writer, "No data");
                    return;
                case Pui.OnlyPositives:
                    serializer.Serialize(writer, "Only positives");
                    return;
                case Pui.PositivesNegatives:
                    serializer.Serialize(writer, "Positives + Negatives");
                    return;
                case Pui.PositivesOnly:
                    serializer.Serialize(writer, "Positives Only");
                    return;
            }
            throw new Exception("Cannot marshal type Pui");
        }

        public static readonly PuiConverter Singleton = new PuiConverter();
    }
}

