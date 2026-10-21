using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace ProtonRBE
{
  public interface IRBE_Esapi_Service
  {
    double PlanDoseRBEWeight { get; set; }

    string CreatePlanSetup(string planId, string planName = "", string planComment = "");

    Task CalculateRBEValuesAsync(string planId, Func<DoseValue, double, DoseValue> RBEFunction, Action<double> progressAction);

    bool IsAsync { get; }
  }
}
