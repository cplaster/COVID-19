using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static COVID_19.DataSet;
using COVID_19.QuickType;
using AutoUpdaterDotNET;

namespace COVID_19
{
    /// <summary>
    /// Interaction logic for MainWindow4.xaml
    /// </summary>
    public partial class MainWindow4 : Window
    {
        List<string> files;
        DataSet dataSet;
        DataSet defaultDataSet;
        string dataFile = "DataSet.bin";
        string[] cmbDataTypeItems = { "Confirmed", "Deaths", "Recovered", "Deaths*", "Deaths Increase", "Hospitalized Cumulative", "Hospitalized Currently",
            "Hospitalized Increase", "In ICU Cumulative", "In ICU Currently", "Negative", "Negative Increase", "On Ventilator Cumulative",
            "On Ventilator Currently", "Positive", "Positive Increase", "Recovered*", "Total Test Results", "Total Test Results Increase"
        };
        string[] cmbAreaItems = { "World", "United States", "US County" };
        private ObservableCollection<string> lbSelectedItemsSource = new ObservableCollection<string>();
        private Random randomR = new Random();
        private Random randomG = new Random();
        private Random randomB = new Random();
        bool normalize = false;
        bool useLogScale = true;
        bool usePopulationPercentage = false;
        bool useMortality = false;
        bool useSurvival = false;
        Dictionary<string, Color> colors = new Dictionary<string, Color>();
        Dictionary<string, Location> countyStore = new Dictionary<string, Location>();
        //string filepath = @"C:\Users\Chad\Documents\GitHub\COVID-19\csse_covid_19_data\csse_covid_19_time_series";
        string filepath = ".\\Data";
        DateTime lastUpdatedData;
        DateTime lastUpdatedFiles;
        bool isUpdating = false;

        public MainWindow4()
        {
            InitializeComponent();

            LoadData();
            lbSelected.ItemsSource = lbSelectedItemsSource;
            lbSelectedItemsSource.CollectionChanged += LbSelectedItemsSource_CollectionChanged;
        }

        private void LbSelectedItemsSource_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Remove:
                    lines.Children.RemoveAt(e.OldStartingIndex);
                    break;

                case NotifyCollectionChangedAction.Add:
                    string item = e.NewItems[0] as string;
                    if (item != null)
                    {
                        PlotGraph(item, GetItemColor(item));
                    }
                    break;
            }
        }

        private Color GetRandomColor()
        {

            return Color.FromRgb((byte)randomR.Next(70, 200), (byte)randomR.Next(50, 225), (byte)randomR.Next(50, 230));
        }

        private Color GetItemColor(string item)
        {
            Color ret;
            if(colors.ContainsKey(item))
            {
                ret = colors[item];
            } else
            {
                ret = GetRandomColor();
                colors.Add(item, ret);
            }

            return ret;
        }

        private void PlotGraph(string itemName, Color color)
        {
            List<string> header;
            Location dataSource;
            Location myLocation = null;
            List<long> data = null;

            if(cmbArea.SelectedIndex == 0)
            {
                header = dataSet.WorldHeader;
                dataSource = dataSet.World;
                myLocation = dataSource.Items[itemName];
            } else
            {
                header = dataSet.UsHeader;
                dataSource = dataSet.Us;
                if (cmbArea.SelectedIndex == 1)
                {
                    myLocation = dataSource.Items[itemName];
                } else
                {
                    myLocation = countyStore[itemName];
                }
            }


            switch(cmbDataType.SelectedIndex)
            {
                case 0:
                    data = myLocation.Stats.Confirmed;
                    break;

                case 1:
                    data = myLocation.Stats.Deaths;
                    break;

                case 2:
                    data = myLocation.Stats.Recovered;
                    break;
                case 3:
                    data = myLocation.DataPoints.Deaths;
                    break;
                case 4:
                    data = myLocation.DataPoints.DeathsIncrease;
                    break;
                case 5:
                    data = myLocation.DataPoints.HospitalizedCumulative;
                    break;
                case 6:
                    data = myLocation.DataPoints.HospitalizedCurrently;
                    break;
                case 7:
                    data = myLocation.DataPoints.HospitalizedIncrease;
                    break;
                case 8:
                    data = myLocation.DataPoints.InIcuCumulative;
                    break;
                case 9:
                    data = myLocation.DataPoints.InIcuCurrently;
                    break;
                case 10:
                    data = myLocation.DataPoints.Negative;
                    break;
                case 11:
                    data = myLocation.DataPoints.NegativeIncrease;
                    break;
                case 12:
                    data = myLocation.DataPoints.OnVentilatorCumulative;
                    break;
                case 13:
                    data = myLocation.DataPoints.OnVentilatorCurrently;
                    break;
                case 14:
                    data = myLocation.DataPoints.Positive;
                    break;
                case 15:
                    data = myLocation.DataPoints.PositiveIncrease;
                    break;
                case 16:
                    data = myLocation.DataPoints.Recovered;
                    break;
                case 17:
                    data = myLocation.DataPoints.TotalTestResults;
                    break;
                case 18:
                    data = myLocation.DataPoints.TotalTestResultsIncrease;
                    break;
            }

            // all except the first 3 indices need a resized header

            if(cmbDataType.SelectedIndex > 2)
            {
                header = GetNewHeader(data.Count);
            }


            var temp = data.ToArray();
            var tdata = new List<double>();

            foreach(var t in temp)
            {
                tdata.Add(Convert.ToDouble(t));
            }

            if (useMortality || useSurvival)
            {
                for (int i = 0; i < tdata.Count; i++)
                {
                    long confirmed = myLocation.Stats.Confirmed[i];

                    if (confirmed != 0)
                    {
                        tdata[i] = (tdata[i] / confirmed) * 100;
                    }
                    else
                    {
                        tdata[i] = 0;
                    }
                }
            }



            if (normalize)
            {
                double last = tdata[data.Count - 1];
                int normalizer = Int32.Parse(txtNormalize.Text);

                if (last > normalizer)
                while (tdata[0] < normalizer)
                {
                    tdata.RemoveAt(0);
                    tdata.Add(last);
                }
            }

            if (useLogScale)
            {
                var ldata = new List<double>();
                foreach(var item in tdata)
                {
                    double log = Math.Log10(item);
                    if(Double.IsInfinity(log))
                    {
                        log = 0;
                    }
                    ldata.Add(log);
                }

                tdata = ldata;
            }

            if(usePopulationPercentage)
            {
                int population = myLocation.LocationInformation.Population;
                if(population != 0)
                {
                    for(int i = 0; i < tdata.Count; i++)
                    {
                        // the percentage of cases per population is probably better expressed as cases per 1000 people
                        tdata[i] = (tdata[i] / population)*1000;
                    }
                }
            }


            var lg = new InteractiveDataDisplay.WPF.LineGraph();
            lines.Children.Add(lg);
            lg.Stroke = new SolidColorBrush(color);
            lg.Description = itemName;
            lg.StrokeThickness = 2;
            header = FormatHeader(header);
            lg.Plot(header, tdata);
        }

        private List<string> GetNewHeader(int width)
        {
            var list = new List<string>();
            for(int i = 0; i < width; i++)
            {
                list.Add(i.ToString());
            }

            return list;
        }

        private List<string> FormatHeader(List<string> header)
        {
            List<string> ret = new List<string>();
            
            for(int i = 0; i < header.Count; i++)
            {
                ret.Add(i.ToString());
            }

            return ret;
        }

        private void MenuItem_Open_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            var result = fbd.ShowDialog();

            if(result == System.Windows.Forms.DialogResult.OK)
            {
                filepath = fbd.SelectedPath;
                LoadDataOLD(true);
            }

        }

        private DataSet LoadFromFiles()
        {
            DirectoryInfo di = new DirectoryInfo(filepath);
            var ds = new DataSet();
            return ds;
        }

        private COVID_19.OLD.DataSet LoadFromFilesOLD()
        {
            DirectoryInfo di = new DirectoryInfo(filepath);

            GetDataFromGitHub(di);

            Dictionary<string, FileInfo> worldFiles = new Dictionary<string, FileInfo>();
            Dictionary<string, FileInfo> usFiles = new Dictionary<string, FileInfo>();

            FileInfo[] files = di.GetFiles("*.csv");
            lastUpdatedFiles = files[0].LastWriteTime;
            foreach (var fi in files)
            {
                var temp = fi.Name.Split("_".ToCharArray());

                if (temp[4] == "global.csv")
                {
                    switch (temp[3])
                    {
                        case "confirmed":
                            worldFiles.Add("Confirmed", fi);
                            break;

                        case "deaths":
                            worldFiles.Add("Deaths", fi);
                            break;

                        case "recovered":
                            worldFiles.Add("Recovered", fi);
                            break;
                    }
                }
                else if (temp[4] == "US.csv")
                {
                    switch (temp[3])
                    {
                        case "confirmed":
                            usFiles.Add("Confirmed", fi);
                            break;

                        case "deaths":
                            usFiles.Add("Deaths", fi);
                            break;

                        case "recovered":
                            usFiles.Add("Recovered", fi);
                            break;
                    }
                }
            }

            var ds = new COVID_19.OLD.DataSet(worldFiles, usFiles);

            // there is now global population information, which we can load as well

            FileInfo fips = new FileInfo(di.FullName + "\\UID_ISO_FIPS_LookUp_Table.csv");
            ds.ParseWorldPopulation(fips);

            return ds;
        }

        private void GetDataFromGitHub(DirectoryInfo di)
        {
            string githubPath = "https://raw.githubusercontent.com/CSSEGISandData/COVID-19/master/csse_covid_19_data/csse_covid_19_time_series/";
            string[] files = {"time_series_covid19_confirmed_US", "time_series_covid19_confirmed_global", "time_series_covid19_deaths_US",
                "time_series_covid19_deaths_global", "time_series_covid19_recovered_global"
            };

            if (!di.Exists)
            {
                di.Create();
            }

            string name = "";
            string fileData = "";

            foreach (var file in files)
            {
                name = file + ".csv";
                fileData = GetFileFromWeb(githubPath + name);
                File.WriteAllText(di.FullName + "\\" + name, fileData);
            }

            name = "UID_ISO_FIPS_LookUp_Table.csv";
            string fipsTableUrl = "https://raw.githubusercontent.com/CSSEGISandData/COVID-19/master/csse_covid_19_data/UID_ISO_FIPS_LookUp_Table.csv";
            fileData = GetFileFromWeb(fipsTableUrl);
            File.WriteAllText(di.FullName + "\\" + name, fileData);
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

        private void LoadData(bool refreshFiles = false)
        {
            var fi = new FileInfo(dataFile);
            var di = new DirectoryInfo(filepath);

            if (!di.Exists)
            {
                di.Create();
                refreshFiles = true;
            }

            if(refreshFiles)
            {
                dataSet = LoadFromFiles();
                lastUpdatedFiles = DateTime.Now; 
                /*
                try
                {
                    dataSet = LoadFromFiles();
                    lastUpdatedFiles = di.GetFiles("*.csv")[0].LastWriteTime;
                }
                catch (Exception ex)
                {
                    // if pulling data files fails for some reason, just load the 
                    // existing binfile, if there is one.
                    if (fi.Exists)
                    {
                        dataSet = DataSet.Deserialize(fi);
                        lastUpdatedData = fi.LastWriteTime;
                    }
                    else
                    {
                        MessageBox.Show("Can't locate suitable data files!", "Error");
                    }
                }
                */
            } else
            {
                if (fi.Exists)
                {
                    dataSet = DataSet.Deserialize(fi);
                    lastUpdatedData = fi.LastWriteTime;
                }
                else
                {
                    MessageBox.Show("Can't locate suitable data files!", "Error");
                }
            }

            if (dataSet != null)
            {
                defaultDataSet = dataSet;
                cmbDataType.SelectedIndex = 0;
                cmbArea.SelectedIndex = 0;
            }
        }

        private void LoadDataOLD(bool useFilepath = false)
        {
            if (useFilepath)
            {
                dataSet = LoadFromFiles();              
            } else
            {
                FileInfo fi = new FileInfo(dataFile);
                if(fi.Exists)
                {
                    DirectoryInfo di = new DirectoryInfo(filepath);

                    if (!di.Exists)
                    {
                        di.Create();
                        dataSet = LoadFromFiles();
                    }
                    else
                    {

                        FileInfo[] files = di.GetFiles("*.csv");

                        if (files.Count() > 0)
                        {
                            lastUpdatedFiles = files[0].LastWriteTime;
                            lastUpdatedData = fi.LastWriteTime;

                            //create a new dataset if the csv files are newer than our existing binfile
                            if (lastUpdatedFiles.Ticks > lastUpdatedData.Ticks)
                            {
                                dataSet = LoadFromFiles();

                            }
                            else
                            {
                                dataSet = DataSet.Deserialize(fi);
                            }
                        } else
                        {
                            dataSet = LoadFromFiles();
                        }
                    }
                } 
            }

            if(dataSet != null)
            {
                defaultDataSet = dataSet;
                cmbDataType.SelectedIndex = 0;
                cmbArea.SelectedIndex = 0;
            } else
            {
                MessageBox.Show("Please select data files.");
                lastUpdatedFiles = DateTime.Now;
                lastUpdatedData = lastUpdatedFiles;
            }
        }

        private void SaveData()
        {
            if(dataSet != defaultDataSet || (lastUpdatedData.Ticks < GetLastUpdate().Ticks))
            {
                dataSet.Serialize(new FileInfo(dataFile));
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveData();
        }

        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void cmbDataType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isUpdating)
            {
                string t = cmbDataTypeItems[cmbDataType.SelectedIndex];

                if (stpMortality != null)
                {
                    stpMortality.Visibility = Visibility.Collapsed;
                    chbMortality.IsChecked = false;
                    useMortality = false;
                }

                if (stpSurvival != null)
                {
                    stpSurvival.Visibility = Visibility.Collapsed;
                    chbSurvival.IsChecked = false;
                    useSurvival = false;
                }

                switch (t)
                {

                    case "Deaths":
                        if (stpMortality != null)
                        {
                            stpMortality.Visibility = Visibility.Visible;
                        }
                        break;

                    case "Recovered":
                        if(stpSurvival != null)
                        {
                            stpSurvival.Visibility = Visibility.Visible;
                        }
                        break;
                }

                if (linegraph1 != null)
                {
                    linegraph1.LeftTitle = t;
                    UpdateGraphTitle();
                    lines.Children.Clear();
                    GenerateGraph();
                }
            }
        }

        private DateTime GetLastUpdate()
        {
            if(lastUpdatedFiles.Ticks > lastUpdatedData.Ticks)
            {
                return lastUpdatedFiles;
            } else
            {
                return lastUpdatedData;
            }
        }

        private void UpdateGraphTitle()
        {
            if (cmbArea.SelectedIndex > -1)
            {
                string t = cmbDataTypeItems[cmbDataType.SelectedIndex];
                linegraph1.Title = cmbAreaItems[cmbArea.SelectedIndex] + " " + t + " as of " + GetLastUpdate().ToString();
            }
        }

        private void cmbArea_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ResetGraph();
            UpdateGraphTitle();
            cmbCountry.Items.Clear();
            lbSelectedItemsSource.Clear();
            var sortedList = new List<string>();
            int stopIndex = 3;

            switch (cmbArea.SelectedIndex)
            {
                case 0:
                    lblArea.Content = "Country";
                    foreach(var item in dataSet.World.Items.Keys)
                    {
                        sortedList.Add(item);
                    }

                    break;

                case 1:
                    lblArea.Content = "State";
                    foreach(var item in dataSet.Us.Items.Keys)
                    {
                        sortedList.Add(item);
                    }

                    stopIndex = 19;

                    break;

                case 2:
                    lblArea.Content = "County";
                    foreach(var state in dataSet.Us.Items.Values)
                    {
                        foreach(var item in state.Items.Values)
                        {
                            var combinedName = item.LocationInformation.CombinedName;
                            sortedList.Add(combinedName);
                            if (!countyStore.ContainsKey(combinedName))
                            {
                                countyStore.Add(combinedName, item);
                            }
                        }
                    }
                    break;
            }

            sortedList.Sort();
            foreach (var item in sortedList)
            {
                cmbCountry.Items.Add(item);
            }

            isUpdating = true;

            cmbDataType.Items.Clear();
            for(int i = 0; i < stopIndex; i++)
            {
                cmbDataType.Items.Add(cmbDataTypeItems[i]);
            }

            isUpdating = false;

            cmbDataType.SelectedIndex = 0;
            

        }

        private void chbNormalize_Checked(object sender, RoutedEventArgs e)
        {
            normalize = true;
            GenerateGraph();
        }

        private void chbNormalize_Unchecked(object sender, RoutedEventArgs e)
        {
            normalize = false;
            GenerateGraph();
        }

        private void GenerateGraph()
        {
            if (lines != null) {

                ResetGraph();

                foreach (var item in lbSelected.Items)
                {
                    var country = item as string;
                    PlotGraph(country, GetItemColor(country));
                }
            }
        }

        private void ResetGraph()
        {
            lines.Children.Clear();
            linegraph1.IsAutoFitEnabled = true;
        }

        private void cmbCountry_DropDownClosed(object sender, EventArgs e)
        {
            var item = cmbCountry.SelectedItem as string;

            if (item != null)
            {
                lbSelectedItemsSource.Add(item);
            }
        }

        private void cmbCountry_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    var selectedItem = cmbCountry.SelectedItem as string;
                    if (selectedItem != null)
                    {
                        lbSelectedItemsSource.Add(selectedItem);
                        var box = FindVisualChild<TextBox>(cmbCountry);
                        cmbCountry.Text = "";
                    }
                    break;
            }
        }



        public static childItem FindVisualChild<childItem>(DependencyObject obj) where childItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is childItem)
                    return (childItem)child;
                else
                {
                    childItem childOfChild = FindVisualChild<childItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        private void cmbCountry_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void lbSelected_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            lbSelectedItemsSource.RemoveAt(lbSelected.SelectedIndex);
        }

        private void chbLogScale_Unchecked(object sender, RoutedEventArgs e)
        {
            useLogScale = false;
            GenerateGraph();
        }

        private void chbLogScale_Checked(object sender, RoutedEventArgs e)
        {
            useLogScale = true;
            GenerateGraph();
        }

        private void txtNormalize_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                GenerateGraph();
            }
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            lbSelectedItemsSource.Clear();
            ResetGraph();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var vc = FindVisualChild<InteractiveDataDisplay.WPF.Legend>(linegraph1);
            if (vc != null)
            {
                //vc.VerticalAlignment = VerticalAlignment.Bottom;
                vc.HorizontalAlignment = HorizontalAlignment.Left;
            }
        }

        private void chbPercentage_Checked(object sender, RoutedEventArgs e)
        {
            usePopulationPercentage = true;
            GenerateGraph();
        }

        private void chbPercentage_Unchecked(object sender, RoutedEventArgs e)
        {
            usePopulationPercentage = false;
            GenerateGraph();
        }

        private void MenuItem_Reload_Click(object sender, RoutedEventArgs e)
        {
            LoadData(true);
            SaveData();
        }

        private void chbMortality_Checked(object sender, RoutedEventArgs e)
        {
            useMortality = true;
            GenerateGraph();
        }

        private void chbMortality_Unchecked(object sender, RoutedEventArgs e)
        {
            useMortality = false;
            GenerateGraph();
        }

        private void chbSurvival_Checked(object sender, RoutedEventArgs e)
        {
            useSurvival = true;
            GenerateGraph();
        }

        private void chbSurvival_Unchecked(object sender, RoutedEventArgs e)
        {
            useSurvival = false;
            GenerateGraph();
        }
    }
}
