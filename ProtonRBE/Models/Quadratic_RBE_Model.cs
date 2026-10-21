using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.Types;

namespace ProtonRBE
{
  public class Quadratic_RBE_Model : RBE_Model_Base, IRBE_Model
  {
    public Quadratic_RBE_Model() : base()
    {
      Name = "Quadratic RBE Model";

      Description = @"Quadratic model to calculate RBE dose.";
      Reference = @"https://pmc.ncbi.nlm.nih.gov/articles/PMC4634882/ Eq. 7 (the second one)";
     
      CalculationParameters.Add(new Parameter_Model("a/B", 1.0, 2));

      PlanId = $"RBE_Quad";
      PlanName = Name;
      PlanComment = this.ToString();
    }

    public DoseValue CalculateRBEDose(DoseValue doseValue, double letValue)
    {
      if(CalculationParameters.Count(x => x.Name == "a/B") != 1)
        throw new InvalidOperationException("a/B value is not set for the Quadratic RBE Model.");

      double alphaPerBeta = CalculationParameters.First(x => x.Name == "a/B").Value;

      var let = letValue / 0.3; // Equation assumes LET relative to photon reference LET (~0.3keV/um).
      var dose = doseValue.Dose;
      if (dose <= 0)
      {
        return new DoseValue(0, doseValue.Unit);
      }

      // A-D part only defines the RBE multiplier to be applied to the physical dose
      var A = 1.0 / (2.0 * dose);
      var B = Math.Pow(alphaPerBeta, 2);
      var C = 4 * dose * alphaPerBeta * (0.999064 + 0.35605 / alphaPerBeta * let);
      var D = 4 * Math.Pow(dose, 2) * Math.Pow(1.1012 - 0.0038703 * Math.Sqrt(alphaPerBeta) * let, 2);
      
      var rbeDose = dose * (A * (Math.Sqrt(B + C + D) - alphaPerBeta));

      return new DoseValue(rbeDose, doseValue.Unit);
    }

  }
}
