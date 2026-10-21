using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.Types;

namespace ProtonRBE
{
  public class Linear_RBE_Model : RBE_Model_Base, IRBE_Model
  {
    public Linear_RBE_Model() : base()
    {
      Name = "Linear RBE Model";

      Description = "Linear model to calculate RBE dose as \n" +
        "D_RBE = D_Phys * (1 + K * LET)";
      Reference = @"https://iopscience.iop.org/article/10.1088/1361-6560/aae8a5";

      CalculationParameters.Add(new Parameter_Model("K", 0.055, 3));

      PlanId = $"RBE_Lin";
      PlanName = Name;
      PlanComment = this.ToString();
    }

    public DoseValue CalculateRBEDose(DoseValue doseValue, double letValue)
    {
      if (CalculationParameters.Count(x => x.Name == "K") != 1)
        throw new InvalidOperationException("K value is not set for the Linear RBE Model");

      double KValue = CalculationParameters.First(x => x.Name == "K").Value;
      return new DoseValue(doseValue.Dose * (1 + KValue * letValue), doseValue.Unit);
    }

  }
}
