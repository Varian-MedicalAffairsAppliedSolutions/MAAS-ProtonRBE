using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ProtonRBE
{
  public abstract class RBE_Model_Base : INotifyPropertyChanged
  {
    protected RBE_Model_Base()
    {
      CalculationParameters = new List<Parameter_Model>();
    }

    public string Name { get; protected set; }

    public string Description { get; protected set; }
    public string Reference { get; protected set; }

    public string PlanId { get; set; }
    public string PlanName { get; set; }
    public string PlanComment { get; set; }

    public List<Parameter_Model> CalculationParameters { get; protected set; }

    public override string ToString()
    {
      string parameters = string.Join(", ", CalculationParameters.Select(
        p => $"{p.Name}={p.Value.ToString($"F{p.Accuracy}", System.Globalization.CultureInfo.InvariantCulture)}"));
      return $"{Name} ({parameters})";
    }

    public void TriggerPlanCommentChange()
    {
      PlanComment = this.ToString();
      OnPropertyChanged(nameof(PlanComment));
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
