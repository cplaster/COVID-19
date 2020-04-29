using System;
using System.Collections.Generic;
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

namespace COVID_19
{
    /// <summary>
    /// Interaction logic for FilterOptionsWindow.xaml
    /// </summary>
    public partial class FilterOptionsWindow : Window
    {
        MainWindow4 parent;

        public FilterOptionsWindow()
        {
            InitializeComponent();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            parent = this.Owner as MainWindow4;
            var dataSet = parent.GetRelevantDataSet();

            //we don't need the totals, if any, so throw it out
            //some also massively skew the results, so throw them out too...
            string[] removeAll = { "Totals", "Grand Princess", "Diamond Princess" };

            foreach(var rem in removeAll)
            {
                dataSet.Remove(rem);
            }


            ApplyScaleFilters(ref dataSet);
            ApplyDateRange(ref dataSet);
            ApplyPercentileRange(ref dataSet);
            parent.ResetGraph();
            parent.PlotGraph(dataSet);

        }

        private void ApplyScaleFilters(ref Dictionary<string, DataSet.LocationData> dataSet)
        {

            foreach(var key in dataSet.Keys.ToList())
            {
                var val = dataSet[key];
                parent.ApplyScaleFilters(ref val);
                dataSet[key] = val;
            }
        }

        private void ApplyPercentileRange(ref Dictionary<string, DataSet.LocationData> dataSet)
        {
            // the percentile numbers will be generated using the most recent datapoint in the dataset
            double top = 0.0;
            double rangeStart = 0.0;
            double rangeEnd = 0.0;
            var newDataSet = new Dictionary<string, DataSet.LocationData>();

            Double.TryParse(txtPercentileStart.Text, out rangeStart);
            double.TryParse(txtPercentileEnd.Text, out rangeEnd);
            rangeStart = rangeStart / 100;
            rangeEnd = rangeEnd / 100;

            foreach(var keyValuePair in dataSet)
            {
                var locationData = keyValuePair.Value;

                if(locationData.Data.Count != 0)
                {
                    var datum = locationData.Data[locationData.Data.Count - 1];
                    if (datum > top)
                    {
                        top = datum;
                    }
                }


            }

            foreach (var keyValuePair in dataSet)
            {
                var locationData = keyValuePair.Value;

                if(locationData.Data.Count != 0)
                {
                    var datum = locationData.Data[locationData.Data.Count - 1];
                    var percentile = datum / top;

                    if (percentile >= rangeStart && percentile <= rangeEnd)
                    {
                        newDataSet.Add(keyValuePair.Key, keyValuePair.Value);
                    }
                }
            }

            dataSet = newDataSet;
        }

        private void ApplyDateRange(ref Dictionary<string, DataSet.LocationData> dataSet)
        {
            //TODO: not implemented
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
