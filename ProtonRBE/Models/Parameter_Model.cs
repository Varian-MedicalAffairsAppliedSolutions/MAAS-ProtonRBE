using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ProtonRBE
{
  public class Parameter_Model : INotifyPropertyChanged
  {
    public Parameter_Model(string name, double value, uint accuracy)
    {
      Name = name;
      Value = value;
      m_ValueString = value.ToString($"F{accuracy}", CultureInfo.InvariantCulture);
      Accuracy = accuracy;
    }

    public string Name { get; private set; }

    public double Value { get; private set; }

    private string m_ValueString;
    public string ValueString
    {
      get
      {
        return m_ValueString;
      }

      set
      {
        m_ValueString = value;
        IsValueValid = StringToDoubleValidator.ValidateString(m_ValueString, new Tuple<double, double>(0, 10), Accuracy, out string errorMessage);
        ValueValidationMessage = errorMessage;

        if (IsValueValid)
        {
          Value = Math.Round(double.Parse(m_ValueString, CultureInfo.InvariantCulture), (int)Accuracy);
        }

        OnPropertyChanged();
      }
    }
    public uint Accuracy { get; private set; }

    private bool m_IsValueValid = true;
    public bool IsValueValid
    {
      get { return m_IsValueValid; }
      private set
      {
        m_IsValueValid = value;
        OnPropertyChanged();
      }
    }

    private string m_ValueValidationMessage = String.Empty;
    public string ValueValidationMessage
    {
      get { return m_ValueValidationMessage; }
      private set
      {
        m_ValueValidationMessage = value;
        OnPropertyChanged();
      }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
