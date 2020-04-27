using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace COVID_19
{
    [Serializable]
    public class DataSet
    {
        private Location us;
        private Location world;
        private List<string> usHeader;
        private List<string> worldHeader;
        private Dictionary<string, string> rawData = new Dictionary<string, string>();

        public Location Us { get { return us; } }
        public Location World { get { return world; } }
        public List<string> UsHeader { get { return usHeader; } }
        public List<string> WorldHeader { get { return worldHeader; } }

        public DataSet()
        {
            LoadData();
            ParseDataWorld();
            ParseDataUs();
        }

        #region Data Loading Methods

        public void DumpData(DirectoryInfo di)
        {
            if(!di.Exists)
            {
                di.Create();
            }

            foreach(var keyValuePair in rawData)
            {
                var filename = di.FullName + "\\" + keyValuePair.Key;
                File.WriteAllText(filename, keyValuePair.Value);
            }

            var binFile = new FileInfo(di.FullName + "\\Data.bin");
            this.Serialize(binFile);

        }

        private string GetFileFromWeb(string url)
        {
            var request = WebRequest.Create(url);
            var response = request.GetResponse();
            var dataStream = response.GetResponseStream();
            var reader = new StreamReader(dataStream);
            string ret = reader.ReadToEnd();
            response.Close();

            return ret;
        }

        private string GetShortName(string url)
        {
            string ret = "";
            if (url.Contains("_"))
            {
                ret = new string(url.ToCharArray().Reverse().ToArray());
                var temp = ret.Split("_".ToCharArray());
                string part1 = new string(temp[1].ToCharArray().Reverse().ToArray());
                string part2 = new string(temp[0].ToCharArray().Reverse().ToArray());
                ret = part1 + "_" + part2;
            } else
            {
                ret = new string(url.ToCharArray().Reverse().ToArray());
                var temp = ret.Split("/".ToCharArray());
                ret = new string(temp[0].ToCharArray().Reverse().ToArray());
            }

            return ret;
        }

        private void LoadData()
        {
            string[] urls =
            {
                "https://raw.githubusercontent.com/CSSEGISandData/COVID-19/master/csse_covid_19_data/csse_covid_19_time_series/time_series_covid19_confirmed_US.csv",
                "https://raw.githubusercontent.com/CSSEGISandData/COVID-19/master/csse_covid_19_data/csse_covid_19_time_series/time_series_covid19_confirmed_global.csv",
                "https://raw.githubusercontent.com/CSSEGISandData/COVID-19/master/csse_covid_19_data/csse_covid_19_time_series/time_series_covid19_deaths_US.csv",
                "https://raw.githubusercontent.com/CSSEGISandData/COVID-19/master/csse_covid_19_data/csse_covid_19_time_series/time_series_covid19_deaths_global.csv",
                "https://raw.githubusercontent.com/CSSEGISandData/COVID-19/master/csse_covid_19_data/csse_covid_19_time_series/time_series_covid19_recovered_global.csv",
                "https://raw.githubusercontent.com/CSSEGISandData/COVID-19/master/csse_covid_19_data/UID_ISO_FIPS_LookUp_Table.csv",
                "https://covidtracking.com/api/v1/states/info.json",
                "https://covidtracking.com/api/v1/states/daily.json"
            };

            foreach (var url in urls)
            {
                var shortUrl = GetShortName(url);
                rawData.Add(shortUrl, GetFileFromWeb(url));
            }
        }

        #endregion

        #region Main Data Parsing Methods

        private void ParseDataUs()
        {
            string[] files =
            {
                "confirmed_US.csv",
                "deaths_US.csv",
            };

            bool firstRun = true;
            int dataWidth = 0;

            foreach (var file in files)
            {
                var data = ParseCsvWithHeader(rawData[file]);

                foreach (var item in data)
                {
                    int fips = -1;

                    if (item["FIPS"] != "")
                    {
                        fips = Int32.Parse((item["FIPS"].Split(".".ToCharArray()))[0]);
                    }
                    string stateName = item["Province_State"];
                    double latitude = 0.0;
                    double longitude = 0.0;
                    Double.TryParse(item["Lat"], out latitude);
                    Double.TryParse(item["Long_"], out longitude);
                    string combinedKey = item["Combined_Key"];
                    string countyName = item["Admin2"];
                    int population = 0;
                    Location county;
                    Location state;

                    string[] removeThese = { "UID", "iso2", "iso3", "code3", "FIPS", "Admin2", "Province_State", "Country_Region", "Lat", "Long_", "Combined_Key" };
                    var removeAll = new List<string>(removeThese);

                    if (item.ContainsKey("Population"))
                    {
                        population = Int32.Parse(item["Population"]);
                        removeAll.Add("Population");
                    }

                    foreach (var remove in removeAll)
                    {
                        item.Remove(remove);
                    }

                    var values = StripValues(item);

                    //initialize the Location object for the us since it doesn't yet exist
                    //we waited until here so we had the dataWidth
                    if (firstRun)
                    {
                        dataWidth = values.Count;
                        usHeader = BuildHeader(item);
                        us = new Location("United States", dataWidth);
                        firstRun = false;
                    }

                    if (!us.Items.ContainsKey(stateName))
                    {

                        state = new Location(stateName, dataWidth);
                        us.Items.Add(stateName, state);

                    }
                    else
                    {
                        state = us.Items[stateName];
                    }

                    if (!state.Items.ContainsKey(countyName))
                    {
                        county = new Location(countyName, dataWidth);
                        state.Items.Add(countyName, county);

                        county.LocationInformation.CombinedName = combinedKey;
                        county.LocationInformation.Latitude = latitude;
                        county.LocationInformation.Longitude = longitude;
                        county.LocationInformation.FIPS = fips;
                    }
                    else
                    {
                        county = state.Items[countyName];
                    }

                    List<long> valueTargetCounty = null;
                    List<long> valueTargetState = null;

                    switch (file)
                    {
                        case "confirmed_US.csv":
                            valueTargetCounty = county.Stats.Confirmed;
                            valueTargetState = state.Stats.Confirmed;
                            break;

                        case "deaths_US.csv":
                            county.LocationInformation.Population = population;
                            valueTargetCounty = county.Stats.Deaths;
                            valueTargetState = state.Stats.Deaths;
                            state.LocationInformation.Population += population;
                            break;

                        case "Recovered":  // there is no JHU recovered data yet for individual states
                            valueTargetCounty = county.Stats.Recovered;
                            valueTargetState = state.Stats.Recovered;
                            break;
                    }

                    for (int i = 0; i < dataWidth; i++)
                    {
                        var myVal = values[i];



                        valueTargetCounty[i] = myVal;
                        valueTargetState[i] += myVal;
                    }
                }
            }

            ParseJsonDataUs();

            int dataPointWidth = -1;

            //once we have things populated, we calculated aggregates for each state
            foreach (var statePair in us.Items)
            {
                var state = statePair.Value;

                if (dataPointWidth == -1)
                {
                    dataPointWidth = state.DataPoints.Deaths.Count;
                    us.DataPoints.Resize(dataPointWidth);
                }

                for (int i = 0; i < dataWidth; i++)
                {
                    us.Stats.Confirmed[i] += state.Stats.Confirmed[i];
                    us.Stats.Deaths[i] += state.Stats.Deaths[i];
                    us.Stats.Recovered[i] += state.Stats.Recovered[i];
                }


                for (int i = 0; i < dataPointWidth; i++)
                {

                    if (state.DataPoints.Deaths.Count == dataPointWidth)
                    {
                        us.DataPoints.Deaths[i] += state.DataPoints.Deaths[i];
                        us.DataPoints.DeathsIncrease[i] += state.DataPoints.DeathsIncrease[i];
                        us.DataPoints.HospitalizedCumulative[i] += state.DataPoints.HospitalizedCumulative[i];
                        us.DataPoints.HospitalizedCurrently[i] += state.DataPoints.HospitalizedCurrently[i];
                        us.DataPoints.HospitalizedIncrease[i] += state.DataPoints.HospitalizedIncrease[i];
                        us.DataPoints.InIcuCumulative[i] += state.DataPoints.InIcuCumulative[i];
                        us.DataPoints.InIcuCurrently[i] += state.DataPoints.InIcuCurrently[i];
                        us.DataPoints.Negative[i] += state.DataPoints.Negative[i];
                        us.DataPoints.NegativeIncrease[i] += state.DataPoints.NegativeIncrease[i];
                        us.DataPoints.OnVentilatorCumulative[i] += state.DataPoints.OnVentilatorCumulative[i];
                        us.DataPoints.OnVentilatorCurrently[i] += state.DataPoints.OnVentilatorCurrently[i];
                        us.DataPoints.Positive[i] += state.DataPoints.Positive[i];
                        us.DataPoints.PositiveIncrease[i] += state.DataPoints.PositiveIncrease[i];
                        us.DataPoints.Recovered[i] += state.DataPoints.Recovered[i];
                        us.DataPoints.TotalTestResults[i] += state.DataPoints.TotalTestResults[i];
                        us.DataPoints.TotalTestResultsIncrease[i] += state.DataPoints.TotalTestResultsIncrease[i];
                    }

                }

                us.LocationInformation.Population += state.LocationInformation.Population;
            }

            var totals = new Location("Totals", dataWidth);
            us.Items.Add("Totals", totals);
            totals.DataPoints.Resize(dataPointWidth);
            totals.LocationInformation.Population = us.LocationInformation.Population;

            for (int i = 0; i < dataWidth; i++)
            {
                totals.Stats.Confirmed[i] = us.Stats.Confirmed[i];
                totals.Stats.Deaths[i] = us.Stats.Deaths[i];
                totals.Stats.Recovered[i] = us.Stats.Recovered[i];
            }

            for (int i = 0; i < dataPointWidth; i++)
            {
                totals.DataPoints.Deaths[i] += us.DataPoints.Deaths[i];
                totals.DataPoints.DeathsIncrease[i] += us.DataPoints.DeathsIncrease[i];
                totals.DataPoints.HospitalizedCumulative[i] += us.DataPoints.HospitalizedCumulative[i];
                totals.DataPoints.HospitalizedCurrently[i] += us.DataPoints.HospitalizedCurrently[i];
                totals.DataPoints.HospitalizedIncrease[i] += us.DataPoints.HospitalizedIncrease[i];
                totals.DataPoints.InIcuCumulative[i] += us.DataPoints.InIcuCumulative[i];
                totals.DataPoints.InIcuCurrently[i] += us.DataPoints.InIcuCurrently[i];
                totals.DataPoints.Negative[i] += us.DataPoints.Negative[i];
                totals.DataPoints.NegativeIncrease[i] += us.DataPoints.NegativeIncrease[i];
                totals.DataPoints.OnVentilatorCumulative[i] += us.DataPoints.OnVentilatorCumulative[i];
                totals.DataPoints.OnVentilatorCurrently[i] += us.DataPoints.OnVentilatorCurrently[i];
                totals.DataPoints.Positive[i] += us.DataPoints.Positive[i];
                totals.DataPoints.PositiveIncrease[i] += us.DataPoints.PositiveIncrease[i];
                totals.DataPoints.Recovered[i] += us.DataPoints.Recovered[i];
                totals.DataPoints.TotalTestResults[i] += us.DataPoints.TotalTestResults[i];
                totals.DataPoints.TotalTestResultsIncrease[i] += us.DataPoints.TotalTestResultsIncrease[i];
            }
        }

        private void ParseDataWorld()
        {
            string[] files =
            {
                "confirmed_global.csv",
                "deaths_global.csv",
                "recovered_global.csv"
            };

            bool firstRun = true;
            int dataWidth = 0;

            foreach (var file in files)
            {
                var data = ParseCsvWithHeader(rawData[file]);

                foreach (var item in data)
                {
                    //JHU lists the individual provinces of our specialCases countries instead of just the country alltogether.
                    //This is fundamentally different from say, Greenland also being listed as a province of Denmark.
                    //So, we have to do some goofy stuff here. In those special cases, the provinces will be added to its parent's Items list,
                    //and all that data will be aggregated into the parent. Otherwise, the province will be promoted to a full-blown country.

                    string[] specialCases = { "Australia", "Canada", "China" };
                    var stateName = item["Province/State"];
                    var countryName = item["Country/Region"];
                    double latitude = Double.Parse(item["Lat"]);
                    double longitude = Double.Parse(item["Long"]);
                    Location country;
                    Location state;
                    Location bigCountry = null;
                    string[] removeThese = { "Province/State", "Country/Region", "Lat", "Long" };

                    foreach (var remove in removeThese)
                    {
                        item.Remove(remove);
                    }

                    var values = StripValues(item);

                    if (firstRun)
                    {
                        dataWidth = values.Count;
                        worldHeader = BuildHeader(item);
                        world = new Location("World", dataWidth);
                        firstRun = false;
                    }

                    if (!world.Items.ContainsKey(countryName))
                    {
                        country = new Location(countryName, dataWidth);
                        world.Items.Add(countryName, country);
                        country.LocationInformation.Latitude = latitude;
                        country.LocationInformation.Longitude = longitude;
                    }
                    else
                    {
                        country = world.Items[countryName];
                    }

                    if (stateName != "")
                    {
                        if (specialCases.Contains(countryName)) {
                            if (!country.Items.ContainsKey(stateName))
                            {
                                state = new Location(stateName, dataWidth);
                                country.Items.Add(stateName, state);
                                state.LocationInformation.Latitude = latitude;
                                state.LocationInformation.Longitude = longitude;
                            }
                            else
                            {
                                state = country.Items[stateName];
                            }

                            bigCountry = country;
                            country = state;
                        } else
                        {
                            if (!world.Items.ContainsKey(stateName))
                            {
                                country = new Location(stateName, dataWidth);
                                world.Items.Add(stateName, country);
                                country.LocationInformation.Latitude = latitude;
                                country.LocationInformation.Longitude = longitude;
                            } else
                            {
                                country = world.Items[stateName];
                            }
                        }
                    }

                    List<long> valueTargetCountry = null;
                    List<long> valueTargetBigCountry = null;

                    switch (file)
                    {
                        case "confirmed_global.csv":
                            valueTargetCountry = country.Stats.Confirmed;
                            if (bigCountry != null)
                            {
                                valueTargetBigCountry = bigCountry.Stats.Confirmed;
                            }
                            break;

                        case "deaths_global.csv":
                            valueTargetCountry = country.Stats.Deaths;
                            if (bigCountry != null)
                            {
                                valueTargetBigCountry = bigCountry.Stats.Deaths;
                            }
                            break;

                        case "recovered_global.csv":
                            valueTargetCountry = country.Stats.Recovered;
                            if (bigCountry != null)
                            {
                                valueTargetBigCountry = bigCountry.Stats.Recovered;
                            }
                            break;
                    }

                    for (int i = 0; i < dataWidth; i++)
                    {
                        var myVal = values[i];
                        valueTargetCountry[i] += myVal;

                        if (stateName != "" && specialCases.Contains(countryName))
                        {
                            valueTargetBigCountry[i] += myVal;
                        }
                    }
                }
            }

            //grab population information for each country, and also add aggregate totals
            var worldTotals = new Location("Totals", dataWidth);
            var populationData = ParseCsvWithHeader(rawData["LookUp_Table.csv"]);

            foreach(var item in populationData)
            {
                var pStateName = item["Province_State"];
                var pCountryName = item["Country_Region"];
                double latitude = 0; Double.TryParse(item["Lat"], out latitude);
                double longitude = 0; Double.TryParse(item["Long_"], out longitude);

                Location country = null;

                if(pStateName != "")
                {
                    if(world.Items.ContainsKey(pStateName))
                    {
                        country = world.Items[pStateName];
                    }
                } else
                {
                    if (world.Items.ContainsKey(pCountryName))
                    {
                        country = world.Items[pCountryName];
                    }
                }

                if(country != null)
                {
                    var pop = item["Population"];
                    int population = 0;
                    Int32.TryParse(pop, out population);
                    country.LocationInformation.Latitude = latitude;
                    country.LocationInformation.Longitude = longitude;
                    country.LocationInformation.Population = population;
                    worldTotals.LocationInformation.Population += population;

                    for(int i = 0; i < dataWidth; i++)
                    {
                        worldTotals.Stats.Confirmed[i] += country.Stats.Confirmed[i];
                        worldTotals.Stats.Deaths[i] += country.Stats.Deaths[i];
                        worldTotals.Stats.Recovered[i] += country.Stats.Recovered[i];
                    }
                }
            }

            world.Items.Add("Totals", worldTotals);
        }

        #endregion

        #region Helper Data Parsing Methods

        private List<string> BuildHeader(Dictionary<string, string> item)
        {
            var ret = new List<string>();

            foreach (var key in item.Keys)
            {
                ret.Add(key);
            }

            return ret;
        }

        public static List<string> ParseCsv(string line)
        {
            TextFieldParser parser = new TextFieldParser(new StringReader(line));
            parser.HasFieldsEnclosedInQuotes = true;
            parser.SetDelimiters(",");
            string[] fields = null;

            while (!parser.EndOfData)
            {
                fields = parser.ReadFields();
            }

            parser.Close();

            return new List<string>(fields);

        }

        public static List<Dictionary<string, string>> ParseCsvWithHeader(string data)
        {
            var ret = new List<Dictionary<string, string>>();
            var lines = new List<string>(data.Split("\r\n".ToCharArray()));
            List<string> header = ParseCsv(lines[0]);
            lines.RemoveAt(0);

            foreach (var line in lines)
            {
                if (line != "")
                {
                    var lineList = ParseCsv(line);
                    var dict = new Dictionary<string, string>();
                    for (int i = 0; i < header.Count; i++)
                    {
                        dict.Add(header[i], lineList[i]);
                    }
                    ret.Add(dict);
                }
            }

            return ret;
        }

        private void ParseJsonDataUs()
        {
            var stateInfo = QuickType.StateInfo.StateInfo.FromJson(rawData["info.json"]);
            var stateData = QuickType.StateData.StateData.FromJson(rawData["daily.json"]);

            //first, figure out the datawidth, which is the number of unique days
            var widthList = new List<long>();
            foreach (var sData in stateData)
            {
                long date = sData.Date;
                if (!widthList.Contains(date))
                {
                    widthList.Insert(0, date);
                }
            }

            int dataWidth = widthList.Count;

            var stateAbbr = new Dictionary<string, string>();
            foreach (var sInfo in stateInfo)
            {
                string name = sInfo.Name;

                // the "of" isn't capitalized in the JHU data :-/

                if (name == "District Of Columbia")
                {
                    name = "District of Columbia";
                }

                // this one doesn't quite jibe either.

                if (name == "US Virgin Islands")
                {
                    name = "Virgin Islands";
                }

                stateAbbr.Add(sInfo.State, name);
                us.Items[name].DataPoints.Resize(dataWidth);
            }

            foreach (var sData in stateData)
            {
                string stateName = stateAbbr[sData.State];
                int index = widthList.IndexOf(sData.Date);
                var dataPoints = us.Items[stateName].DataPoints;

                dataPoints.Deaths[index] = sData.Death.GetValueOrDefault(0);
                dataPoints.DeathsIncrease[index] = sData.DeathIncrease.GetValueOrDefault(0);
                dataPoints.HospitalizedCumulative[index] = sData.HospitalizedCumulative.GetValueOrDefault(0);
                dataPoints.HospitalizedCurrently[index] = sData.HospitalizedCurrently.GetValueOrDefault(0);
                dataPoints.HospitalizedIncrease[index] = sData.HospitalizedIncrease.GetValueOrDefault(0);
                dataPoints.InIcuCumulative[index] = sData.InIcuCumulative.GetValueOrDefault(0);
                dataPoints.InIcuCurrently[index] = sData.InIcuCurrently.GetValueOrDefault(0);
                dataPoints.Negative[index] = sData.Negative.GetValueOrDefault(0);
                dataPoints.NegativeIncrease[index] = sData.NegativeIncrease.GetValueOrDefault(0);
                dataPoints.OnVentilatorCumulative[index] = sData.OnVentilatorCumulative.GetValueOrDefault(0);
                dataPoints.OnVentilatorCurrently[index] = sData.OnVentilatorCurrently.GetValueOrDefault(0);
                dataPoints.Positive[index] = sData.Positive.GetValueOrDefault(0);
                dataPoints.PositiveIncrease[index] = sData.PositiveIncrease.GetValueOrDefault(0);
                dataPoints.Recovered[index] = sData.Recovered.GetValueOrDefault(0);
                dataPoints.TotalTestResults[index] = sData.TotalTestResults.GetValueOrDefault(0);
                dataPoints.TotalTestResultsIncrease[index] = sData.TotalTestResultsIncrease.GetValueOrDefault(0);
            }

        }

        private List<int> StripValues(Dictionary<string, string> item)
        {
            var list = new List<int>();

            foreach (var value in item.Values)
            {
                list.Add(Int32.Parse(value));
            }

            return list;
        }

        #endregion

        #region Serialization Implementation

        public static DataSet Deserialize(FileInfo fi)
        {
            object o = null;

            if (fi.Exists && fi.Length > 0)
            {
                Stream s = File.OpenRead(fi.FullName);
                BinaryFormatter b = new BinaryFormatter();
                o = b.Deserialize(s);
                s.Close();
            }

            return o as DataSet;
        }

        public void Serialize(FileInfo fi)
        {
            Stream s = File.OpenWrite(fi.FullName);
            BinaryFormatter b = new BinaryFormatter();
            b.Serialize(s, this);
            s.Close();
        }

        #endregion

        #region Child Classes

        [Serializable]
        public class DataPoints
        {
            public List<long> Deaths { get; set; }
            public List<long> Recovered { get; set; }
            public List<long> Positive { get; set; }
            public List<long> Negative { get; set; }
            public List<long> HospitalizedCurrently { get; set; }
            public List<long> HospitalizedCumulative { get; set; }
            public List<long> InIcuCurrently { get; set; }
            public List<long> InIcuCumulative { get; set; }
            public List<long> OnVentilatorCurrently { get; set; }
            public List<long> OnVentilatorCumulative { get; set; }
            public List<long> TotalTestResults { get; set; }
            public List<long> DeathsIncrease { get; set; }
            public List<long> PositiveIncrease { get; set; }
            public List<long> NegativeIncrease { get; set; }
            public List<long> HospitalizedIncrease { get; set; }
            public List<long> TotalTestResultsIncrease { get; set; }

            public DataPoints()
            {
                Initalize();
            }

            public DataPoints(int dataWidth)
            {
                Initalize();
                Resize(dataWidth);
            }

            private void Initalize()
            {
                Deaths = new List<long>();
                Recovered = new List<long>();
                Positive = new List<long>();
                Negative = new List<long>();
                HospitalizedCumulative = new List<long>();
                HospitalizedCurrently = new List<long>();
                HospitalizedIncrease = new List<long>();
                InIcuCumulative = new List<long>();
                InIcuCurrently = new List<long>();
                OnVentilatorCumulative = new List<long>();
                OnVentilatorCurrently = new List<long>();
                TotalTestResults = new List<long>();
                DeathsIncrease = new List<long>();
                PositiveIncrease = new List<long>();
                NegativeIncrease = new List<long>();
                TotalTestResultsIncrease = new List<long>();
            }

            public void Resize(int width)
            {
                // this scans all public instance properties of type DataPoints, targets all properties of type List<long>, and
                // FRONT-PADS the list with zeroes if the list.Count is less than width.
                //
                // This uses reflection, so adding any public instance properties of type List<long> will be included in this behavior

                Type t = typeof(DataPoints);
                PropertyInfo[] propertyInfos = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var propertyInfo in propertyInfos)
                {
                    
                    if(propertyInfo.PropertyType == typeof(List<long>))
                    {
                        var list = propertyInfo.GetValue(this) as List<long>;
                        if (width > list.Count)
                        {
                            int diff = width - list.Count;
                            for (int i = 0; i < diff; i++)
                            {
                                list.Insert(0, 0);
                            }

                            propertyInfo.SetValue(this, list);
                        }
                    }
                }
            }

        }

        [Serializable]
        public class Location
        {
            Dictionary<string, Location> items = new Dictionary<string, Location>();
            LocationInfo locationInfo = new LocationInfo();
            string name;
            Statistics stats;
            DataPoints dataPoints;
            int dataWidth;

            public Dictionary<string, Location> Items { get { return items; } }
            public LocationInfo LocationInformation { get { return locationInfo; } }
            public string Name { get { return name; } }
            public Statistics Stats { get { return stats; } }
            public int DataWidth { get { return dataWidth; } }
            public DataPoints DataPoints { get { return dataPoints; } }

            public Location(string name, int dataWidth)
            {
                this.name = name;
                this.dataWidth = dataWidth;
                stats = new Statistics(dataWidth);
                dataPoints = new DataPoints();
            }
        }

        [Serializable]
        public class LocationInfo
        {
            public int FIPS { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public string CombinedName { get; set; }
            public int Population { get; set; }

            public LocationInfo(double latitude = -1, double longitude = -1, int fips = -1, string combinedName = null, int population = 0)
            {
                Latitude = latitude;
                Longitude = longitude;
                FIPS = fips;
                CombinedName = combinedName;
                Population = population;
            }
        }

        [Serializable]
        public class Statistics
        {
            List<long> confirmed;
            List<long> deaths;
            List<long> recovered;

            public List<long> Confirmed { get { return confirmed; } }
            public List<long> Deaths { get { return deaths; } }
            public List<long> Recovered { get { return recovered; } }

            public Statistics(int length)
            {
                confirmed = new List<long>(length);
                deaths = new List<long>(length);
                recovered = new List<long>(length);

                ResizeList(confirmed, length);
                ResizeList(deaths, length);
                ResizeList(recovered, length);
            }

            private void ResizeList(List<long> list, int length)
            {
                for (int i = 0; i < length; i++)
                {
                    list.Add(0);
                }
            }

        }

        #endregion
    }
}
