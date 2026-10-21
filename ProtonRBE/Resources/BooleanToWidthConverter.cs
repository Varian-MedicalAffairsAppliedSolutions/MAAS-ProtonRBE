using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;

namespace ProtonRBE.Resources
{
  public class BooleanToWidthConverter : IValueConverter
  {
    public double TrueWidth { get; set; }
    public double FalseWidth { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is bool booleanValue)
      {
        return booleanValue ? TrueWidth : FalseWidth;
      }

      return DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
