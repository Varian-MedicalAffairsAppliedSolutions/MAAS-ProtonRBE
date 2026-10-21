using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using VMS.TPS.Common.Model.Types;

namespace ProtonRBE
{
  public interface IRBE_Model
  {
    string Name { get; }

    string Description { get; }
    string Reference { get; }

    /// <summary>
    /// Plan ID to which this RBE Dose has been calculated.
    /// </summary>
    string PlanId { get; set; }
    string PlanName { get; set; }
    string PlanComment { get; set; }

    List<Parameter_Model> CalculationParameters { get; }

    //void SetCalculationParameter(string name, double value);

    /// <summary>
    /// Generic method to calculate RBE Dose from given dose and LET values.
    /// </summary>
    /// <param name="doseValue"></param>
    /// <param name="letValue">In keV/um</param>
    /// <returns>DoseValue where the dose is RBE weighted.W</returns>
    DoseValue CalculateRBEDose(DoseValue doseValue, double letValue);

    string ToString();

    void TriggerPlanCommentChange();
  }
}
