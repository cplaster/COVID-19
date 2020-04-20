using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.VisualBasic.FileIO;
using System.Reflection;
using System.Net;

namespace COVID_19.OLD
{
    [Serializable]
    public class DataSet
    {
        Location us;
        List<string> usHeader;
        Location world;
        List<string> worldHeader;
        string[] worldSpecialCases = { "China", "Canada", "Australia" };

        public Location Us { get { return us; } }
        public List<string> UsHeader { get { return usHeader; } }
        public Location World { get { return world; } }
        public List<string> WorldHeader { get { return worldHeader; } }
       
        public DataSet(Dictionary<string, FileInfo> worldData, Dictionary<string, FileInfo> usData)
        {
            ParseWorldDataNEW(worldData);
            ParseUsDataNEW(usData);
        }

        private void ParseUsDataNEW(Dictionary<string, FileInfo> usData)
        {
            bool firstRun = true;
            int dataWidth = 0;

            foreach (var filePair in usData)
            {
                //var key = filePair.Key;
                var data = ParseCsvWithHeader(filePair.Value);

                foreach(var item in data)
                {
                    int fips = -1;

                    if (item["FIPS"] != "")
                    {
                        fips = Int32.Parse((item["FIPS"].Split(".".ToCharArray()))[0]);
                    }
                    string stateName = item["Province_State"];
                    double latitude = Double.Parse(item["Lat"]);
                    double longitude = Double.Parse(item["Long_"]);
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
                        us = new Location("United States", dataWidth);
                        usHeader = BuildHeader(item);
                        firstRun = false;
                    }

                    if (!us.Items.ContainsKey(stateName))
                    {

                        state = new Location(stateName, dataWidth);
                        us.Items.Add(stateName, state);

                    } else
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

                    switch (filePair.Key)
                    {
                        case "Confirmed":
                            valueTargetCounty = county.Stats.Confirmed;
                            valueTargetState = state.Stats.Confirmed;
                            break;

                        case "Deaths":
                            county.LocationInformation.Population = population;
                            valueTargetCounty = county.Stats.Deaths;
                            valueTargetState = state.Stats.Deaths;
                            state.LocationInformation.Population += population;
                            break;

                        case "Recovered":
                            valueTargetCounty = county.Stats.Recovered;
                            valueTargetState = state.Stats.Recovered;
                            break;
                    }

                    for(int i = 0; i < dataWidth; i++)
                    {
                        var myVal = values[i];



                        valueTargetCounty[i] = myVal;
                        valueTargetState[i] += myVal;
                    }
                }
            }

            ParseJsonData();

            int dataPointWidth = -1;

            //once we have things populated, we calculated aggregates for each state
            foreach (var statePair in us.Items)
            {
                var state = statePair.Value;

                if(dataPointWidth == -1)
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


                for(int i = 0; i < dataPointWidth; i++)
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

            for(int i = 0; i < dataPointWidth; i++) {
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

        private void ParseJsonData()
        {
            List<QuickType.StateInfo.StateInfo> stateInfo;
            List<QuickType.StateData.StateData> stateData;

            var request = WebRequest.Create("https://covidtracking.com/api/v1/states/info.json");
            var response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            var reader = new StreamReader(dataStream);
            string jsonString = reader.ReadToEnd();
            stateInfo = QuickType.StateInfo.StateInfo.FromJson(jsonString);
            response.Close();

            request = WebRequest.Create("https://covidtracking.com/api/v1/states/daily.json");
            response = request.GetResponse();
            dataStream = response.GetResponseStream();
            reader = new StreamReader(dataStream);
            jsonString = reader.ReadToEnd();
            stateData = QuickType.StateData.StateData.FromJson(jsonString);
            response.Close();

           //first, figure out the datawidth, which is the number of unique days
            var widthList = new List<long>();
            foreach(var sData in stateData)
            {
                long date = sData.Date;
                if(!widthList.Contains(date))
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

                if(name == "District Of Columbia")
                {
                    name = "District of Columbia";
                }

                if(name == "US Virgin Islands")
                {
                    name = "Virgin Islands";
                }

                stateAbbr.Add(sInfo.State, name);
                us.Items[name].DataPoints.Resize(dataWidth);
            }

            foreach(var sData in stateData)
            {
                string stateName = stateAbbr[sData.State];
                var loc = us.Items[stateName];
                int index = widthList.IndexOf(sData.Date);
                loc.DataPoints.Deaths[index] = sData.Death.GetValueOrDefault(0);
                loc.DataPoints.DeathsIncrease[index] = sData.DeathIncrease.GetValueOrDefault(0);
                loc.DataPoints.HospitalizedCumulative[index] = sData.HospitalizedCumulative.GetValueOrDefault(0);
                loc.DataPoints.HospitalizedCurrently[index] = sData.HospitalizedCurrently.GetValueOrDefault(0);
                loc.DataPoints.HospitalizedIncrease[index] = sData.HospitalizedIncrease.GetValueOrDefault(0);
                loc.DataPoints.InIcuCumulative[index] = sData.InIcuCumulative.GetValueOrDefault(0);
                loc.DataPoints.InIcuCurrently[index] = sData.InIcuCurrently.GetValueOrDefault(0);
                loc.DataPoints.Negative[index] = sData.Negative.GetValueOrDefault(0);
                loc.DataPoints.NegativeIncrease[index] = sData.NegativeIncrease.GetValueOrDefault(0);
                loc.DataPoints.OnVentilatorCumulative[index] = sData.OnVentilatorCumulative.GetValueOrDefault(0);
                loc.DataPoints.OnVentilatorCurrently[index] = sData.OnVentilatorCurrently.GetValueOrDefault(0);
                loc.DataPoints.Positive[index] = sData.Positive.GetValueOrDefault(0);
                loc.DataPoints.PositiveIncrease[index] = sData.PositiveIncrease.GetValueOrDefault(0);
                loc.DataPoints.Recovered[index] = sData.Recovered.GetValueOrDefault(0);
                loc.DataPoints.TotalTestResults[index] = sData.TotalTestResults.GetValueOrDefault(0);
                loc.DataPoints.TotalTestResultsIncrease[index] = sData.TotalTestResultsIncrease.GetValueOrDefault(0);
            }

        }
        
        private void ParseWorldDataNEW(Dictionary<string, FileInfo> worldData)
        {
            bool firstRun = true;
            int dataWidth = 0;

            foreach(var filePair in worldData)
            {
                var data = ParseCsvWithHeader(filePair.Value);

                foreach(var item in data)
                {
                    // Province/State,Country/Region,Lat,Long

                    var stateName = item["Province/State"];
                    var countryName = item["Country/Region"];
                    double latitude = Double.Parse(item["Lat"]);
                    double longitude = Double.Parse(item["Long"]);
                    Location country;
                    Location state;
                    Location bigCountry = null;

                    string[] removeThese = { "Province/State", "Country/Region", "Lat", "Long" };
                    var removeAll = new List<string>(removeThese);

                    foreach (var remove in removeThese)
                    {
                        item.Remove(remove);
                    }

                    var values = StripValues(item);

                    if (firstRun)
                    {
                        dataWidth = values.Count;
                        world = new Location("World", dataWidth);
                        worldHeader = BuildHeader(item);
                        firstRun = false;
                    }

                    if(!world.Items.ContainsKey(countryName))
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

                    if(stateName != "")
                    {
                        if(!country.Items.ContainsKey(stateName))
                        {
                            state = new Location(stateName, dataWidth);
                            country.Items.Add(stateName, state);
                            state.LocationInformation.Latitude = latitude;
                            state.LocationInformation.Longitude = longitude;
                        } else
                        {
                            state = country.Items[stateName];
                        }

                        bigCountry = country;
                        country = state;
                    }

                    List<long> valueTargetCountry = null;
                    List<long> valueTargetBigCountry = null;

                    switch (filePair.Key)
                    {
                        case "Confirmed":
                            valueTargetCountry = country.Stats.Confirmed;
                            if (bigCountry != null)
                            {
                                valueTargetBigCountry = bigCountry.Stats.Confirmed;
                            }
                            break;

                        case "Deaths":
                            valueTargetCountry = country.Stats.Deaths;
                            if (bigCountry != null)
                            {
                                valueTargetBigCountry = bigCountry.Stats.Deaths;
                            }
                            break;

                        case "Recovered":
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

                        if(stateName != "")
                        {
                            valueTargetBigCountry[i] += myVal;
                        }
                    }
                }
            }

            //calculate aggregate world totals

            var worldTotals = new Location("Totals", dataWidth);

            foreach(var keyValuePair in world.Items)
            {
                var country = keyValuePair.Value;

                worldTotals.LocationInformation.Population += country.LocationInformation.Population;

                for(int i = 0; i < dataWidth; i++)
                {
                    worldTotals.Stats.Confirmed[i] += country.Stats.Confirmed[i];
                    worldTotals.Stats.Deaths[i] += country.Stats.Deaths[i];
                    worldTotals.Stats.Recovered[i] += country.Stats.Recovered[i];
                }
            }

            world.Items.Add("Totals", worldTotals);
        }

        private void ParseUsData(Dictionary<string, FileInfo> usData)
        {
            var hlines = File.ReadAllLines(usData["Confirmed"].FullName);
            var hlinesList = new List<string>(hlines);
            var header = hlinesList[0];
            usHeader = ParseUsHeader(header);
            int headerLength = usHeader.Count;
            us = new Location("United States", headerLength);
            ResizeList(us.Stats.Confirmed, headerLength);
            ResizeList(us.Stats.Deaths, headerLength);
            ResizeList(us.Stats.Recovered, headerLength);
            us.LocationInformation.Population = 0;

            foreach (var filePair in usData)
            {
                var key = filePair.Key;
                var lines = File.ReadAllLines(filePair.Value.FullName);
                var linesList = new List<string>(lines);
                linesList.RemoveAt(0);

                foreach (var line in linesList)
                {
                    // this list includes "quoted, comma, stuff" so we have to be a little tricky here.
                    var splitLine = line.Split("\"".ToCharArray());
                    var part1 = ParseCsv(splitLine[0]);
                    //part1.RemoveAt(part1.Count);
                    var part2 = splitLine[1];
                    var part3 = ParseCsv(splitLine[2]);
                    part3.RemoveAt(0);


                    Location county;
                    Location state;

                    int fips = -1;

                    if (part1[4] != "")
                    {
                        // for some reason, the fips number is a decimal now, it should be int *sigh*
                        if(part1[4].Contains("."))
                        {
                            var temp = part1[4].Split(".".ToCharArray());
                            part1[4] = temp[0];
                        }

                        fips = Int32.Parse(part1[4]);
                    }
                    string countyName = part1[5];
                    string stateName = part1[6];
                    string combinedKey = part2;
                    double latitude = Double.Parse(part1[8]);
                    double longitude = Double.Parse(part1[9]);
                    int population = 0;

                    //the deaths.csv has one extra column here, "Population" that the confirmed doesnt SMH...

                    if (key == "Deaths")
                    {
                        population = Int32.Parse(part3[0]);
                        part3.RemoveAt(0);
                    }

                    if (!us.Items.ContainsKey(stateName))
                    {
                        state = new Location(stateName, headerLength);
                        us.Items.Add(stateName, state);
                        ResizeList(state.Stats.Confirmed, headerLength);
                        ResizeList(state.Stats.Deaths, headerLength);
                        ResizeList(state.Stats.Recovered, headerLength);
                    }
                    else
                    {
                        state = us.Items[stateName];
                    }

                    if (!state.Items.ContainsKey(countyName))
                    {
                        county = new Location(countyName, headerLength);
                        state.Items.Add(countyName, county);

                        county.LocationInformation.CombinedName = combinedKey;
                        county.LocationInformation.Latitude = latitude;
                        county.LocationInformation.Longitude = longitude;
                        county.LocationInformation.FIPS = fips;
                        ResizeList(county.Stats.Confirmed, headerLength);
                        ResizeList(county.Stats.Deaths, headerLength);
                        ResizeList(county.Stats.Recovered, headerLength);
                    } else
                    {
                        county = state.Items[countyName];
                    }

                    switch (filePair.Key)
                    {
                        case "Confirmed":
                            PopulateUsData(county.Stats.Confirmed, part3);
                            break;

                        case "Deaths":
                            county.LocationInformation.Population = population;
                            PopulateUsData(county.Stats.Deaths, part3);
                            break;

                        case "Recovered":
                            PopulateUsData(county.Stats.Recovered, part3);
                            break;
                    }

                }

            }


            //once we have things populated, we calculated aggregates for each state
            foreach (var statePair in us.Items)
            {
                var state = statePair.Value;

                foreach(var countyPair in state.Items)
                {
                    var county = countyPair.Value;

                    state.LocationInformation.Population += county.LocationInformation.Population;

                    for(int i = 0; i < headerLength; i++)
                    {
                        state.Stats.Confirmed[i] += county.Stats.Confirmed[i];
                        state.Stats.Deaths[i] += county.Stats.Deaths[i];
                        state.Stats.Recovered[i] += county.Stats.Recovered[i];
                    }
                }

                for (int i = 0; i < headerLength; i++)
                {
                    us.Stats.Confirmed[i] += state.Stats.Confirmed[i];
                    us.Stats.Deaths[i] += state.Stats.Deaths[i];
                    us.Stats.Recovered[i] += state.Stats.Recovered[i];
                    us.LocationInformation.Population += state.LocationInformation.Population;
                }

            }
        }

        public void ParseWorldPopulation(FileInfo fi)
        {
            var lines = File.ReadAllLines(fi.FullName);
            var lineList = new List<string>(lines);
            lineList.RemoveAt(0);

            foreach(var line in lineList)
            {
                string name;
                int population;

                var l = line;

                if (line.Contains("\""))
                {
                    var lList = line.Split("\"".ToCharArray());
                    name = lList[lList.Length - 2];
                    Int32.TryParse(lList[lList.Length - 1].Replace(",", ""), out population);
                } else
                {
                    var lp = ParseCsv(line);
                    name = lp[10];
                    population = 0;
                    Int32.TryParse(lp[11], out population);
                }
                if(world.Items.ContainsKey(name))
                {
                    world.Items[name].LocationInformation.Population = population;
                }
            }
        }

        private List<string> ParseUsHeader(string header)
        {
            var list = ParseCsv(header);
            list.RemoveRange(0, 11);

            return list;
        }

        private void PopulateUsData(List<long> container, List<string> data)
        {
            if(data.Count > 0)
            {
                for(int i = 0; i < data.Count; i++)
                {
                    container[i] = Int32.Parse(data[i]);
                }
            }
        }

        private void ParseWorldData(Dictionary<string, FileInfo> worldData)
        {
            var hlines = File.ReadAllLines(worldData["Confirmed"].FullName);
            var hlinesList = new List<string>(hlines);
            var header = hlinesList[0];
            hlinesList.RemoveAt(0); // strip header;
            worldHeader = ParseWorldHeader(header);
            int headerLength = worldHeader.Count;

            world = new Location("World", headerLength);

            foreach(var filePair in worldData)
            {
                var lines = File.ReadAllLines(filePair.Value.FullName);
                var linesList = new List<string>(lines);
                linesList.RemoveAt(0);

                foreach(var line in linesList)
                {

                    //each line possibly has "quoted, comma, stuff", so we have to fix that. grr...
                    var line2 = line;

                    if (line.Contains("\""))
                    {
                        var splitLine = line.Split("\"".ToCharArray());
                        splitLine[1] = splitLine[1].Replace(",", "_");
                        line2 = splitLine[0] + splitLine[1] + splitLine[2];
                    }


                    var list = ParseCsv(line2);
                    string provinceName = list[0];
                    string countryName = list[1];
                    double latitude = Double.Parse(list[2]);
                    double longitude = double.Parse(list[3]);
                    list.RemoveRange(0, 4);

                    Location country;
                    Location province;

                    if(!world.Items.ContainsKey(countryName))
                    {
                        country = new Location(countryName, headerLength);
                        world.Items.Add(countryName, country);
                        country.LocationInformation.Latitude = latitude;
                        country.LocationInformation.Longitude = longitude;
                    } else
                    {
                        country = world.Items[countryName];
                    }

                    if(provinceName != null && provinceName != "")
                    {
                        if(!country.Items.ContainsKey(provinceName))
                        {
                            province = new Location(provinceName, headerLength);
                            country.Items.Add(provinceName, province);
                        } else
                        {
                            province = country.Items[provinceName];
                        }

                        province.LocationInformation.Latitude = latitude;
                        province.LocationInformation.Longitude = longitude;

                        switch(filePair.Key)
                        {
                            case "Confirmed":
                                PopulateWorldData(province.Stats.Confirmed, list);
                                break;
                            
                            case "Deaths":
                                PopulateWorldData(province.Stats.Deaths, list);
                                break;

                            case "Recovered":
                                PopulateWorldData(province.Stats.Recovered, list);
                                break;
                        }
                    } else
                    {
                        switch (filePair.Key)
                        {
                            case "Confirmed":
                                PopulateWorldData(country.Stats.Confirmed, list);
                                break;

                            case "Deaths":
                                PopulateWorldData(country.Stats.Deaths, list);
                                break;

                            case "Recovered":
                                PopulateWorldData(country.Stats.Recovered, list);
                                break;
                        }
                    }

                }
            }

            // china, canada, and austrailian provinces are a special case and should be aggregated

            foreach(var countryName in worldSpecialCases)
            {
                var country = world.Items[countryName];
                ResizeList(country.Stats.Confirmed, headerLength);
                ResizeList(country.Stats.Deaths, headerLength);
                ResizeList(country.Stats.Recovered, headerLength);

                foreach (var provincePair in country.Items)
                {
                    var province = provincePair.Value;

                    for(int i = 0; i < headerLength; i++)
                    {
                        var confirmed = province.Stats.Confirmed;
                        var deaths = province.Stats.Deaths;
                        var recovered = province.Stats.Recovered;

                        if(confirmed.Count == headerLength)
                        {
                            country.Stats.Confirmed[i] += province.Stats.Confirmed[i];
                        }

                        if(deaths.Count == headerLength)
                        {
                            country.Stats.Deaths[i] += province.Stats.Deaths[i];
                        }

                        if(recovered.Count == headerLength)
                        {
                            country.Stats.Recovered[i] += province.Stats.Recovered[i];
                        }                  
                    }
                }
            }
        }

        private void PopulateWorldData(List<long> container, List<string> data)
        {
            foreach (var d in data)
            {
                container.Add(Int32.Parse(d));
            }
        }

        private List<string> ParseWorldHeader(string header)
        {
            var list = ParseCsv(header);
            list.RemoveRange(0, 4);

            return list;
        }

        #region Utility Functions

        public static List<string> NewParseCsv(string line)
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

        public static List<Dictionary<string, string>> ParseCsvWithHeader(FileInfo fi)
        {
            var ret = new List<Dictionary<string, string>>();
            var lines = new List<string>(File.ReadAllLines(fi.FullName));
            List<string> header = NewParseCsv(lines[0]);
            lines.RemoveAt(0);

            foreach(var line in lines)
            {
                var lineList = NewParseCsv(line);
                var dict = new Dictionary<string, string>();
                for(int i = 0; i < header.Count; i++)
                {
                    dict.Add(header[i], lineList[i]);
                }
                ret.Add(dict);
            }

            return ret;
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

        private List<string> BuildHeader(Dictionary<string, string> item)
        {
            var ret = new List<string>();

            foreach (var key in item.Keys)
            {
                ret.Add(key);
            }

            return ret;
        }

        public static List<string> ParseCsv(string item)
        {
            var arr = item.Split(",".ToCharArray());

            return new List<string>(arr);
        }

        private void ResizeList(List<long> list, int headerLength)
        {
            list.Clear();
            for(int i = 0; i < headerLength; i++)
            {
                list.Add(0);
            }
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
            public List<long> NegativeIncrease{ get; set; }
            public List<long> HospitalizedIncrease{ get; set; }
            public List<long> TotalTestResultsIncrease{ get; set; }

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
                Type t = typeof(DataPoints);
                PropertyInfo[] propertyInfos = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach(var propertyInfo in propertyInfos)
                {
                    var list = propertyInfo.GetValue(this) as List<long>;
                    if(width > list.Count)
                    {
                        int diff = width - list.Count;
                        for(int i = 0; i < diff; i++)
                        {
                            list.Insert(0, 0);
                        }

                        propertyInfo.SetValue(this, list);
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
                for(int i = 0; i < length; i++)
                {
                    list.Add(0);
                }
            }

        }

        #endregion
    }
}
