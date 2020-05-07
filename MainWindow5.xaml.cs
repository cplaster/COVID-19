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
        string[] cmbDataTypeItems = { "Confirmed", "Deaths", "Recovered", "Survival Rate", "Mortality Rate", "Resolved", "Deaths*", "Deaths Increase", 
            "Hospitalized Cumulative", "Hospitalized Currently", "Hospitalized Increase", "In ICU Cumulative", "In ICU Currently", "Negative",
            "Negative Increase", "On Ventilator Cumulative", "On Ventilator Currently", "Positive", "Positive Increase", "Recovered*",
            "Total Test Results", "Total Test Results Increase"
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
        private InteractiveDataDisplay.WPF.Legend legend;
        private InteractiveDataDisplay.WPF.Figure legendParent;
        private Random randomR = new Random();
        private bool rangeFilterApplied = false;
        private int rotationOffset = 20;
        private int rotation = -20;
        private int turns = 0;
        private Random randomHue = new Random();
        private Random randomSaturation = new Random();
        private Random randomLuminosity = new Random();


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
                    data = myLocation.Stats.SurvivalRate;
                    break;
                case 4:
                    data = myLocation.Stats.MortalityRate;
                    break;
                case 5:
                    data = myLocation.Stats.Resolved;
                    break;
                case 6:
                    data = myLocation.DataPoints.Deaths;
                    break;
                case 7:
                    data = myLocation.DataPoints.DeathsIncrease;
                    break;
                case 8:
                    data = myLocation.DataPoints.HospitalizedCumulative;
                    break;
                case 9:
                    data = myLocation.DataPoints.HospitalizedCurrently;
                    break;
                case 10:
                    data = myLocation.DataPoints.HospitalizedIncrease;
                    break;
                case 11:
                    data = myLocation.DataPoints.InIcuCumulative;
                    break;
                case 12:
                    data = myLocation.DataPoints.InIcuCurrently;
                    break;
                case 13:
                    data = myLocation.DataPoints.Negative;
                    break;
                case 14:
                    data = myLocation.DataPoints.NegativeIncrease;
                    break;
                case 15:
                    data = myLocation.DataPoints.OnVentilatorCumulative;
                    break;
                case 16:
                    data = myLocation.DataPoints.OnVentilatorCurrently;
                    break;
                case 17:
                    data = myLocation.DataPoints.Positive;
                    break;
                case 18:
                    data = myLocation.DataPoints.PositiveIncrease;
                    break;
                case 19:
                    data = myLocation.DataPoints.Recovered;
                    break;
                case 20:
                    data = myLocation.DataPoints.TotalTestResults;
                    break;
                case 21:
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

        private Color GetRandomColorOLD()
        {
            return Color.FromRgb((byte)randomR.Next(70, 200), (byte)randomR.Next(50, 225), (byte)randomR.Next(50, 230));
        }

        private Color GetRandomColor()
        {
            rotation += rotationOffset;

            double sat = 1;
            double lum = 0.40;

            // human eyes aren't terribly good at differentiating reds
            if(rotation == 20)
            {
                rotation = 40;
            }

            // human eyes have especial trouble differentiating greens
            if(rotation == 120)
            {
                rotation = 180;
            }


            if(rotation >= 360)
            {
                rotation -= 360;
                turns++;
            }

            if(turns > 0)
            {
                sat = 0.5;

                if(turns > 1)
                {
                    lum = 0.60;
                }
            }

            //var hsl = new HSLColor(1, randomHue.Next(0, 360), randomSaturation.Next(1, 2) * 0.5, randomLuminosity.Next(30, 50) / 100.0);

            var hsl = new HSLColor(1, rotation, sat, lum);

            var color = hsl.ToColor();

            return color;
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
                if (!lbSelectedItemsSource.Contains(itemName))
                {
                    lbSelectedItemsSource.Add(itemName);
                }
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

        private void RefreshGraph()
        {
            if (rangeFilterApplied)
            {
                btnApplyRange_Click(this, new RoutedEventArgs());
            } else
            {
                GenerateGraph();
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

            rangeFilterApplied = true;
            btnApplyRange.Visibility = Visibility.Collapsed;
            btnRemoveRange.Visibility = Visibility.Visible;

        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            rangeFilterApplied = false;
            lbSelectedItemsSource.Clear();
            ResetGraph();
        }

        private void btnRemoveRange_Click(object sender, RoutedEventArgs e)
        {
            btnRemoveRange.Visibility = Visibility.Collapsed;
            btnApplyRange.Visibility = Visibility.Visible;
            rangeFilterApplied = false;
            GenerateGraph();
        }

        private void chbLogScale_Checked(object sender, RoutedEventArgs e)
        {
            RefreshGraph();
        }

        private void chbLogScale_Unchecked(object sender, RoutedEventArgs e)
        {
            RefreshGraph();
        }

        private void chbNormalize_Checked(object sender, RoutedEventArgs e)
        {
            RefreshGraph();
        }

        private void chbNormalize_Unchecked(object sender, RoutedEventArgs e)
        {
            RefreshGraph();
        }

        private void chbPercentage_Checked(object sender, RoutedEventArgs e)
        {
            RefreshGraph();
        }

        private void chbPercentage_Unchecked(object sender, RoutedEventArgs e)
        {
            RefreshGraph();
        }

        private void cmbArea_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ResetGraph();
            UpdateGraphTitle();
            cmbCountry.Items.Clear();
            lbSelectedItemsSource.Clear();
            var sortedList = new List<string>();
            int stopIndex = 6;

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

                    stopIndex = 22;

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

                    stopIndex = 2;

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

                /*
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
                */

                if (linegraph1 != null)
                {
                    linegraph1.LeftTitle = t;
                    UpdateGraphTitle();
                    lines.Children.Clear();
                    RefreshGraph();
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

                            if(rangeFilterApplied)
                            {
                                btnApplyRange_Click(sender, new RoutedEventArgs());
                            }
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
            legend = FindVisualChild<InteractiveDataDisplay.WPF.Legend>(linegraph1);
            if (legend != null)
            {
                // this pushes the legend of the graph outside of the graph, to the right
                // and keeps it from jumping all over the place every time a new element is added.
                legend.HorizontalAlignment = HorizontalAlignment.Right;
                legendParent = legend.Parent as InteractiveDataDisplay.WPF.Figure;
                legend.RenderTransform = new TranslateTransform(250, 0);
                var x = legend.Content as InteractiveDataDisplay.WPF.LegendItemsPanel;
                x.Width = 200;
            }
        }


        #endregion

    }
}
