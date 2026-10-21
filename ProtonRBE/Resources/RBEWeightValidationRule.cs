using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ProtonRBE
{
  public class RBEWeightValidationRule : ValidationRule
  {
    //public Type ValidationType { get; set; } = typeof(double);
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
      string stringValue = Convert.ToString(value);
      if (string.IsNullOrEmpty(stringValue))
        return new ValidationResult(false, "");

      if (!double.TryParse(stringValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double doubleValue))
        return new ValidationResult(false, "Value must be a number");

      if (doubleValue < 0.1 || doubleValue > 10)
        return new ValidationResult(false, "Value must be between 0.1 and 10");

      return new ValidationResult(true, "");
    }
  }
}
