using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.Types;

namespace ProtonRBE
{
  public class Constant_RBE_Model : RBE_Model_Base, IRBE_Model
  {
    public Constant_RBE_Model() : base()
    {
      Name = $"Constant RBE Model";

      Description = "Constant model to calculate RBE dose as D_RBE = D_Phys * C";
      Reference = @"";

      CalculationParameters.Add(new Parameter_Model("C", 1.1, 2));

      PlanId = $"RBE_Const";
      PlanName = Name;
      PlanComment = this.ToString();
    }

    public DoseValue CalculateRBEDose(DoseValue doseValue, double letValue)
    {
      if (CalculationParameters.Count(x => x.Name == "C") != 1)
        throw new InvalidOperationException("C value is not set for the Constant RBE Model");

      double CValue = CalculationParameters.First(x => x.Name == "C").Value;
      return new DoseValue(doseValue.Dose * CValue, doseValue.Unit);
    }
  }
}
