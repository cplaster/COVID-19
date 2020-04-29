using AutoUpdaterDotNET;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
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

namespace COVID_19
{
    /// <summary>
    /// Interaction logic for MainWindow5.xaml
    /// </summary>
    public partial class MainWindow5 : Window
    {
        bool appStarting = true;
        string[] cmbAreaItems = { "World", "United States", "US County" };
        string[] cmbDataTypeItems = { "Confirmed", "Deaths", "Recovered", "Deaths*", "Deaths Increase", "Hospitalized Cumulative", "Hospitalized Currently",
            "Hospitalized Increase", "In ICU Cumulative", "In ICU Currently", "Negative", "Negative Increase", "On Ventilator Cumulative",
            "On Ventilator Currently", "Positive", "Positive Increase", "Recovered*", "Total Test Results", "Total Test Results Increase"
        };
        bool cmbDataType_isUpdating = false;
        Dictionary<string, Color> colors = new Dictionary<string, Color>();
        Dictionary<string, Location> countyStore = new Dictionary<string, Location>();
        string dataFile = "DataSet.bin";
        DataSet dataSet;
        Dictionary<string, Location> dataSource;
        bool dataUpdated = false;
        List<string> dateHeader;
        DateTime lastUpdate;
        bool lbSelectedItems_noUpdates = false;
        private ObservableCollection<string> lbSelectedItemsSource = new ObservableCollection<string>();
        private Random randomR = new Random();

        public MainWindow5()
        {
            InitializeComponent();

            AutoUpdater.Start("https://raw.githubusercontent.com/cplaster/COVID-19/master/Releases/Updater.xml");

            LoadData();
            lbSelected.ItemsSource = lbSelectedItemsSource;
            lbSelectedItemsSource.CollectionChanged += LbSelectedItemsSource_CollectionChanged;
            appStarting = false;          
        }

        #region Methods

        private void ApplyDateFilter(ref Dictionary<string, LocationData> data)
        {
            var startDate = dpStart.SelectedDate;
            var endDate = dpEnd.SelectedDate;
            var startIndex = dateHeader.IndexOf(GetDateString(startDate));
            var endIndex = dateHeader.IndexOf(GetDateString(endDate));

            foreach(var keyValuePair in data)
            {
                var item = keyValuePair.Value;
                var startRemoveLength = startIndex;
                if (startRemoveLength > 0)
                {
                    item.Data.RemoveRange(0, startRemoveLength);
                }
                var endRemoveIndex = endIndex - startRemoveLength;
                var endRemoveLength = item.Data.Count - (endRemoveIndex + 1);
                if (endRemoveLength > 0)
                {
                    item.Data.RemoveRange(endRemoveIndex + 1, item.Data.Count - (endRemoveIndex + 1));
                }

                int zzzz = 1;
            }


        }

        private void ApplyPercentileFilter(ref Dictionary<string, LocationData> data)
        {
            // the percentile numbers will be generated using the most recent datapoint in the dataset
            double top = 0.0;
            double rangeStart = 0.0;
            double rangeEnd = 0.0;
            var newDataSet = new Dictionary<string, LocationData>();

            Double.TryParse(txtPercentileStart.Text, out rangeStart);
            double.TryParse(txtPercentileEnd.Text, out rangeEnd);
            rangeStart = rangeStart / 100;
            rangeEnd = rangeEnd / 100;

            foreach (var keyValuePair in data)
            {
                var locationData = keyValuePair.Value;

                if (locationData.Data.Count != 0)
                {
                    var datum = locationData.Data[locationData.Data.Count - 1];
                    if (datum > top)
                    {
                        top = datum;
                    }
                }


            }

            foreach (var keyValuePair in data)
            {
                var locationData = keyValuePair.Value;

                if (locationData.Data.Count != 0)
                {
                    var datum = locationData.Data[locationData.Data.Count - 1];
                    var percentile = datum / top;

                    if (percentile >= rangeStart && percentile <= rangeEnd)
                    {
                        newDataSet.Add(keyValuePair.Key, keyValuePair.Value);
                    }
                }
            }

            data = newDataSet;
        }

        private void ApplyRangeFilters(ref Dictionary<string,LocationData> data)
        {
            //Totals, if any, aren't really relevant for range filtering, so we can remove it.
            //The cruise ships also severely skew certain percentils, so remove then as well.
            string[] removeThese = { "Totals", "Grand Princess", "Diamond Princess" };

            foreach(var removeThis in removeThese)
            {
                if(data.ContainsKey(removeThis))
                {
                    data.Remove(removeThis);
                }
            }

            ApplyDateFilter(ref data);
            ApplyPercentileFilter(ref data);

        }

        private void ApplyScaleFilters(ref LocationData locationData)
        {
            if (locationData.Data.Count == 0)
            {
                return;
            }

            var temp = locationData.Data.ToArray();
            var tdata = new List<double>();

            foreach (var t in temp)
            {
                tdata.Add(Convert.ToDouble(t));
            }

            if ((chbMortality.IsChecked.HasValue && chbMortality.IsChecked == true) || (chbSurvival.IsChecked.HasValue && chbSurvival.IsChecked == true))
            {
                for (int i = 0; i < tdata.Count; i++)
                {
                    long confirmed = locationData.Confirmed[i];

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

            if (chbNormalize.IsChecked.HasValue && chbNormalize.IsChecked == true)
            {
                double last = tdata[locationData.Data.Count - 1];
                double normalizer = double.Parse(txtNormalize.Text);

                if (last > normalizer)
                    while (tdata[0] < normalizer)
                    {
                        tdata.RemoveAt(0);
                        tdata.Add(last);
                    }
            }

            if (chbLogScale.IsChecked.HasValue && chbLogScale.IsChecked == true)
            {
                var ldata = new List<double>();
                foreach (var item in tdata)
                {
                    double log = Math.Log10(item);
                    if (Double.IsInfinity(log))
                    {
                        log = 0;
                    }
                    ldata.Add(log);
                }

                tdata = ldata;
            }

            if (chbPercentage.IsChecked.HasValue && chbPercentage.IsChecked == true)
            {
                long population = locationData.Population;
                if (population != 0)
                {
                    for (int i = 0; i < tdata.Count; i++)
                    {
                        // the percentage of cases per population is probably better expressed as cases per 1000 people
                        tdata[i] = (tdata[i] / population) * 1000;
                    }
                }
            }

            locationData.Data = tdata;

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

        private void GenerateGraph()
        {
            if (lines != null)
            {

                ResetGraph();

                
                foreach (var item in lbSelected.Items)
                {
                    var itemName = item as string;
                    PlotItem(itemName);
                }
                

                /*
                var itemNames = lbSelectedItemsSource.ToArray();
                if (itemNames.Length > 0)
                {
                    PlotItems(itemNames);
                }
                */
            }
        }

        private string GetDateString(DateTime? dateTime)
        {
            string ret = "";

            if(dateTime != null)
            {
                var date = dateTime.Value;
                ret = date.Month.ToString() + "/" + date.Day.ToString() + "/" + date.Year.ToString();
            }

            return ret;
        }

        private DateTime? GetDateTime(string date)
        {
            DateTime.TryParse(date, out DateTime ret);

            return ret;
        }

        private List<string> GetHeader(int width)
        {
            var list = new List<string>();
            for (int i = 0; i < width; i++)
            {
                list.Add(i.ToString());
            }

            return list;
        }

        private Color GetItemColor(string item)
        {
            Color ret;
            if (colors.ContainsKey(item))
            {
                ret = colors[item];
            }
            else
            {
                ret = GetRandomColor();
                colors.Add(item, ret);
            }

            return ret;
        }

        private LocationData GetLocationData(string itemName, Dictionary<string, Location> source = null)
        {

            if(source == null)
            {
                source = dataSource;
            }

            var myLocation = source[itemName];
            List<long> data = null;

            switch (cmbDataType.SelectedIndex)
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

            var locationData = new LocationData();
            locationData.Name = itemName;
            locationData.Population = myLocation.LocationInformation.Population;
            locationData.Confirmed = myLocation.Stats.Confirmed;
            locationData.Data = data.ConvertAll(x => (double)x);

            return locationData;
        }

        private Color GetRandomColor()
        {
            return Color.FromRgb((byte)randomR.Next(70, 200), (byte)randomR.Next(50, 225), (byte)randomR.Next(50, 230));
        }

        public static bool InRange(DateTime? dateToCheck, DateTime? startDate, DateTime? endDate)
        {
            return dateToCheck >= startDate && dateToCheck <= endDate;
        }

        private void LoadData(bool refresh = false)
        {
            var fi = new FileInfo(dataFile);

            if (!fi.Exists || refresh)
            {
                dataSet = new DataSet();
                dataUpdated = true;
                lastUpdate = DateTime.Now;
            } else
            {
                dataSet = DataSet.Deserialize(fi);
                lastUpdate = fi.LastWriteTime;
            }

            if (dataSet != null)
            {
                cmbArea.SelectedIndex = 0;
                cmbDataType.SelectedIndex = 0;
            }

        }

        private void PlotItems(string[] itemNames)
        {
            var locationItems = new Dictionary<string, LocationData>();

            foreach(var itemName in itemNames)
            {
                var locationData = GetLocationData(itemName);
                ApplyScaleFilters(ref locationData);
                locationItems.Add(itemName, locationData);
            }

            ApplyRangeFilters(ref locationItems);

            var temp = locationItems.Keys.ToArray()[0];
            var headerLength = locationItems[temp].Data.Count;
            var header = GetHeader(headerLength);
            lbSelectedItems_noUpdates = true;

            foreach(var keyValuePair in locationItems)
            {
                var itemName = keyValuePair.Key;
                var locationData = keyValuePair.Value;
                var lg = new InteractiveDataDisplay.WPF.LineGraph();
                lines.Children.Add(lg);
                lg.Stroke = new SolidColorBrush(GetItemColor(itemName));
                lg.Description = itemName;
                lg.StrokeThickness = 2;
                lg.Plot(header, locationData.Data);
                lbSelectedItemsSource.Add(itemName);
            }

            lbSelectedItems_noUpdates = false;

        }

        private void PlotItem(string itemName)
        {
            var locationData = GetLocationData(itemName);

            ApplyScaleFilters(ref locationData);

            var header = GetHeader(locationData.Data.Count);
            var lg = new InteractiveDataDisplay.WPF.LineGraph();
            lines.Children.Add(lg);
            lg.Stroke = new SolidColorBrush(GetItemColor(itemName));
            lg.Description = itemName;
            lg.StrokeThickness = 2;
            lg.Plot(header, locationData.Data);
        }


        private void ResetGraph()
        {
            lines.Children.Clear();
            linegraph1.IsAutoFitEnabled = true;
        }

        private void UpdateGraphTitle()
        {
            if (cmbArea.SelectedIndex > -1)
            {
                string t = cmbDataTypeItems[cmbDataType.SelectedIndex];
                linegraph1.Title = cmbAreaItems[cmbArea.SelectedIndex] + " " + t + " as of " + lastUpdate.ToString();
            }
        }

        #endregion

        #region Event Handlers 

        private void btnApplyRange_Click(object sender, RoutedEventArgs e)
        {
            string[] itemNames;

            if(lbSelectedItemsSource.Count > 0)
            {
                itemNames = lbSelectedItemsSource.ToArray();
            } else
            {
                itemNames = dataSource.Keys.ToArray();
            }

            ResetGraph();
            PlotItems(itemNames);

        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            lbSelectedItemsSource.Clear();
            ResetGraph();
        }

        private void chbLogScale_Checked(object sender, RoutedEventArgs e)
        {
            GenerateGraph();
        }

        private void chbLogScale_Unchecked(object sender, RoutedEventArgs e)
        {
            GenerateGraph();
        }

        private void chbMortality_Checked(object sender, RoutedEventArgs e)
        {
            GenerateGraph();
        }

        private void chbMortality_Unchecked(object sender, RoutedEventArgs e)
        {
            GenerateGraph();
        }

        private void chbNormalize_Checked(object sender, RoutedEventArgs e)
        {
            GenerateGraph();
        }

        private void chbNormalize_Unchecked(object sender, RoutedEventArgs e)
        {
            GenerateGraph();
        }

        private void chbPercentage_Checked(object sender, RoutedEventArgs e)
        {
            GenerateGraph();
        }

        private void chbPercentage_Unchecked(object sender, RoutedEventArgs e)
        {
            GenerateGraph();
        }

        private void chbSurvival_Checked(object sender, RoutedEventArgs e)
        {
            GenerateGraph();
        }

        private void chbSurvival_Unchecked(object sender, RoutedEventArgs e)
        {
            GenerateGraph();
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
                    dataSource = dataSet.World.Items;
                    dateHeader = dataSet.WorldHeader;
                    foreach (var item in dataSet.World.Items.Keys)
                    {
                        sortedList.Add(item);
                    }

                    break;

                case 1:
                    lblArea.Content = "State";
                    dataSource = dataSet.Us.Items;
                    dateHeader = dataSet.JsonHeader;
                    foreach (var item in dataSet.Us.Items.Keys)
                    {
                        sortedList.Add(item);
                    }

                    stopIndex = 19;

                    break;

                case 2:
                    lblArea.Content = "County";
                    dataSource = countyStore;
                    dateHeader = dataSet.UsHeader;
                    foreach (var state in dataSet.Us.Items.Values)
                    {
                        foreach (var item in state.Items.Values)
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

            cmbDataType_isUpdating = true;
            cmbDataType.Items.Clear();

            for (int i = 0; i < stopIndex; i++)
            {
                cmbDataType.Items.Add(cmbDataTypeItems[i]);
            }

            cmbDataType_isUpdating = false;
            cmbDataType.SelectedIndex = 0;

            var dateStart = GetDateTime(dateHeader[0]);
            var dateEnd = GetDateTime(dateHeader[dateHeader.Count - 1]);

            dpStart.DisplayDateStart = dateStart;
            dpStart.DisplayDateEnd = dateEnd;
            dpEnd.DisplayDateStart = dateStart;
            dpEnd.DisplayDateEnd = dateEnd;

            if(appStarting)
            {
                dpStart.SelectedDate = dateStart;
                dpEnd.SelectedDate = dateEnd;
            }

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

        private void cmbDataType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!cmbDataType_isUpdating)
            {
                string t = cmbDataTypeItems[cmbDataType.SelectedIndex];

                if (stpMortality != null)
                {
                    stpMortality.Visibility = Visibility.Collapsed;
                    chbMortality.IsChecked = false;
                }

                if (stpSurvival != null)
                {
                    stpSurvival.Visibility = Visibility.Collapsed;
                    chbSurvival.IsChecked = false;
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
                        if (stpSurvival != null)
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

        private void dpStart_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {

            if (dateHeader != null)
            {
                var dateStart = GetDateTime(dateHeader[0]);
                var dateEnd = GetDateTime(dateHeader[dateHeader.Count - 1]);
                if (dpStart != null)
                {
                    var s = sender as DatePicker;
                    if (!InRange(s.SelectedDate, dateStart, dateEnd))
                    {
                        if (dateStart != null)
                        {
                            s.SelectedDate = dateStart;
                        }
                    }
                }

                if (dpEnd != null)
                {
                    dpEnd.DisplayDateStart = dpStart.SelectedDate;
                    if (!InRange(dpEnd.SelectedDate, dpStart.SelectedDate, dateEnd))
                    {
                        dpEnd.SelectedDate = dateEnd;
                    }
                }
            }
        }

        private void dpEnd_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if(dateHeader != null)
            {
                var dateStart = GetDateTime(dateHeader[0]);
                var dateEnd = GetDateTime(dateHeader[dateHeader.Count - 1]);

                if(dpStart != null)
                {
                    dateStart = dpStart.SelectedDate;
                }

                if(dpEnd != null)
                {
                    var s = sender as DatePicker;
                    if(!InRange(s.SelectedDate, dateStart, dateEnd))
                    {
                        if(dateEnd != null)
                        {
                            s.SelectedDate = dateEnd;
                        }
                    }
                }

            }
        }

        private void LbSelectedItemsSource_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (!lbSelectedItems_noUpdates)
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
                            PlotItem(item);
                        }
                        break;
                }
            }
        }

        private void lbSelected_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            lbSelectedItemsSource.RemoveAt(lbSelected.SelectedIndex);
        }

        private void MenuItem_About_Click(object sender, RoutedEventArgs e)
        {
            Window w = new About();
            w.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            w.Show();
        }

        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MenuItem_Reload_Click(object sender, RoutedEventArgs e)
        {
            LoadData(true);
        }

        private void txtNormalize_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                GenerateGraph();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (dataUpdated)
            {
                dataSet.Serialize(new FileInfo(dataFile));
            }
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

        #endregion


    }
}
