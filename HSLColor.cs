using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace COVID_19
{
    class HSLColor
    {
        double hue = 0;
        double saturation = 0;
        double luminence = 0;
        double halpha = 0;
        Color sColor;

        public HSLColor(int alpha, int red, int green, int blue)
        {
            sColor = Color.FromArgb((byte)alpha, (byte)red, (byte)green, (byte)blue);
            ColorToHSL(sColor);
        }

        public HSLColor(Color color)
        {
            sColor = color;
            ColorToHSL(sColor);
        }

        public HSLColor(double alpha, double hue, double saturation, double luminence)
        {
            this.halpha = alpha;
            this.hue = hue;
            this.saturation = saturation;
            this.luminence = luminence;
        }

        public Color ToColor()
        {
            Color color;

            if(saturation == 0)
            {
                byte l = (byte)(luminence * 255);
                color = Color.FromArgb((byte)Math.Round(halpha), l, l, l);
            } else
            {
                double temp1;
                double temp2;
                double thue;

                if(luminence < 0.5)
                {
                    temp1 = luminence * (1 + saturation);
                } else
                {
                    temp1 = (luminence + saturation) - (luminence * saturation);
                }

                temp2 = (2 * luminence) - temp1;

                thue = hue / 360;

                double r = thue + 0.333;
                double g = thue;
                double b = thue - 0.333;

                double[] values = { r, g, b };
                
                for(int i = 0; i < values.Count(); i++)
                {
                    if(values[i] < 0)
                    {
                        values[i] += 1;
                    }

                    if(values[i] > 1)
                    {
                        values[i] -= 1;
                    }

                    if(6 * values[i] < 1)
                    {
                        values[i] = temp2 + (temp1 - temp2) * 6 * values[i];
                    } else
                    {
                        if (2 * values[i] < 1) {
                            values[i] = temp1;
                        } else
                        {
                            if(3 * values[i] < 2)
                            {
                                values[i] = temp2 + (temp1 - temp2) * (0.666 - values[i]) * 6;
                            } else
                            {
                                values[i] = temp2;
                            }
                        }
                    }

                    values[i] = Math.Round(values[i] * 255, 2);
                }

                color = Color.FromArgb((byte)Math.Round(halpha * 255), (byte)values[0], (byte)values[1], (byte)values[2]);

            }

            return color;
        }

        private void ColorToHSL(Color color)
        {
            halpha = color.A /255;
            double red = color.R;
            double green = color.G;
            double blue = color.B;

            double min;
            double max;

            min = Math.Min(red, green);
            min = Math.Min(min, blue);

            max = Math.Max(red, green);
            max = Math.Max(max, blue);

            // luminence is defined as the average of the minimum and maximum RGB values
            luminence = (min + max) / 2;

            //if min and max are the same, there is no saturation, otherwise the saturation value is defined as below
            if(min != max)
            {
                if(luminence < 0.5)
                {
                    saturation = (max - min) / (max + min);
                } else
                {
                    saturation = (max - min) / (2.0 - max - min);
                }
            }

            //hue is defined depending on the max value.
            // if red is max, hue = (green - blue) / (max - min)
            // if green is max, hue = 2.0 + (blue - red) / (max - min)
            // if blue is max, hue = 4.0 + (red - green) / (max - min)

            if(red == max)
            {
                hue = (green - blue) / (max - min);
            }
            else if(green == max)
            {
                hue = 2.0 + (blue - red) / (max - min);
            }
            else if(blue == max)
            {
                hue = 4.0 + (red - green) / (max - min);
            }

            //convert hue to degrees
            hue = hue * 360;
        }


    }
}
