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
  public class BooleanToVisibilityConverter : IValueConverter
  {
    public Visibility TrueVisibility { get; set; } = Visibility.Visible;
    public Visibility FalseVisibility { get; set; } = Visibility.Hidden;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is bool booleanValue)
      {
        return booleanValue ? TrueVisibility : FalseVisibility;
      }

      return DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
