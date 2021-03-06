﻿// <auto-generated />
//
// To parse this JSON data, add NuGet 'Newtonsoft.Json' then do:
//
//    using COVID_19.QuickType.StateData;
//
//    var stateData = StateData.FromJson(jsonString);

namespace COVID_19.QuickType.StateData
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public partial class StateData
    {
        [JsonProperty("date")]
        public long Date { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("positive")]
        public long? Positive { get; set; }

        [JsonProperty("negative")]
        public long? Negative { get; set; }

        [JsonProperty("pending")]
        public long? Pending { get; set; }

        [JsonProperty("hospitalizedCurrently")]
        public long? HospitalizedCurrently { get; set; }

        [JsonProperty("hospitalizedCumulative")]
        public long? HospitalizedCumulative { get; set; }

        [JsonProperty("inIcuCurrently")]
        public long? InIcuCurrently { get; set; }

        [JsonProperty("inIcuCumulative")]
        public long? InIcuCumulative { get; set; }

        [JsonProperty("onVentilatorCurrently")]
        public long? OnVentilatorCurrently { get; set; }

        [JsonProperty("onVentilatorCumulative")]
        public long? OnVentilatorCumulative { get; set; }

        [JsonProperty("recovered")]
        public long? Recovered { get; set; }

        [JsonProperty("hash")]
        public string Hash { get; set; }

        [JsonProperty("dateChecked")]
        public DateTimeOffset DateChecked { get; set; }

        [JsonProperty("death", NullValueHandling = NullValueHandling.Ignore)]
        public long? Death { get; set; }

        [JsonProperty("hospitalized")]
        public long? Hospitalized { get; set; }

        [JsonProperty("total", NullValueHandling = NullValueHandling.Ignore)]
        public long? Total { get; set; }

        [JsonProperty("totalTestResults", NullValueHandling = NullValueHandling.Ignore)]
        public long? TotalTestResults { get; set; }

        [JsonProperty("posNeg", NullValueHandling = NullValueHandling.Ignore)]
        public long? PosNeg { get; set; }

        [JsonProperty("fips")]
        public string Fips { get; set; }

        [JsonProperty("deathIncrease")]
        public long? DeathIncrease { get; set; }

        [JsonProperty("hospitalizedIncrease")]
        public long? HospitalizedIncrease { get; set; }

        [JsonProperty("negativeIncrease")]
        public long? NegativeIncrease { get; set; }

        [JsonProperty("positiveIncrease")]
        public long? PositiveIncrease { get; set; }

        [JsonProperty("totalTestResultsIncrease")]
        public long? TotalTestResultsIncrease { get; set; }
    }

    public partial class StateData
    {
        public static List<StateData> FromJson(string json) => JsonConvert.DeserializeObject<List<StateData>>(json, COVID_19.QuickType.StateData.Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this List<StateData> self) => JsonConvert.SerializeObject(self, COVID_19.QuickType.StateData.Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }
}
