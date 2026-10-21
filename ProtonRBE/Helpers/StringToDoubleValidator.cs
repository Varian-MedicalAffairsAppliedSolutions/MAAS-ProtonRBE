using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProtonRBE
{
  public static class StringToDoubleValidator
  {
    public static bool ValidateString(string input, Tuple<double, double> limits, uint accuracy, out string errorMessage)
    {
      if (string.IsNullOrWhiteSpace(input))
      {
        errorMessage = "Value can't be empty.";
        return false;
      }
      if(input.Contains(','))
      {
        errorMessage = "Use '.' as decimal separator.";
        return false;
      }
      if (!double.TryParse(input, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double result))
      {
        errorMessage = "Value must be a number.";
        return false;
      }
      if (result < limits.Item1 || result > limits.Item2)
      {
        errorMessage = $"Value must be between {limits.Item1} and {limits.Item2}.";
        return false;
      }
      if (GetDecimalCount(input) > accuracy)
      {
        errorMessage = $"Value must have maximum {accuracy} decimals.";
        return false;
      }

      errorMessage = "";
      return true;
    }

    private static int GetDecimalCount(string input)
    {
      int separatorIndex = input.IndexOf('.');
      if (separatorIndex < 0)
        return 0; // No decimal point

      return input.Length - separatorIndex - 1;
    }
  }
}