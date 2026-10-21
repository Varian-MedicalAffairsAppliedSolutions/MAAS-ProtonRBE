using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Serilog;

using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;


namespace ProtonRBE
{
  public class RBE_Service
  {
    private IRBE_Esapi_Service m_EsapiService;

    public double PlanDoseRBEWeight 
    {
      get
      {
        return m_EsapiService.PlanDoseRBEWeight;
      }
      set
      {
        m_EsapiService.PlanDoseRBEWeight = value;
      }
    }

    public string ValidateRBEWeightString(double rbeWeight)
    {
      if(double.IsNaN(rbeWeight))
        return "Value must be a number.";

      if (rbeWeight < 0.1 || rbeWeight > 10)
        return "Value must be between 0.1 and 10.";

      return "";
    }

    #region RBE Models Handling

    private List<IRBE_Model> m_models;

    public List<IRBE_Model> GetModels()
    {
      return m_models;
    }

    private IRBE_Model m_selectedModel;
    public void SetSelectedModel(IRBE_Model model)
    {
      m_selectedModel = model;
    }

    #endregion

    public RBE_Service(IRBE_Esapi_Service EsapiService)
    {
      m_EsapiService = EsapiService;

      PlanDoseRBEWeight = 1.1;


      #region Init Models
      
      m_models = new List<IRBE_Model>();

      m_models.Add(new Constant_RBE_Model());
      m_models.Add(new Linear_RBE_Model());
      m_models.Add(new Quadratic_RBE_Model());

      if (m_models is null || m_models.Count == 0)
      {
        throw new Exception("No RBE models available.");
      }

      #endregion

    }

    public string CreateRBEPlan()
    {
      LogHelp.LogStart(MethodBase.GetCurrentMethod());

      var model = m_selectedModel;
        
      string newPlanId = "";
      try
      {
        newPlanId = m_EsapiService.CreatePlanSetup(model.PlanId, model.PlanName, 
          model.PlanComment + "\n\n" + model.ToString());
      }
      catch (Exception e)
      {
        Log.Error(e.Message);
        throw;
      }

      if (model.PlanId != newPlanId)
      {
        model.PlanId = newPlanId;
      }

      LogHelp.LogEnd(MethodBase.GetCurrentMethod());

      return newPlanId;
    }

    public async Task CalculateRBEValuesAsync(Action<double> progressAction)
    {
      LogHelp.LogStart(MethodBase.GetCurrentMethod());

      await m_EsapiService.CalculateRBEValuesAsync(m_selectedModel.PlanId, m_selectedModel.CalculateRBEDose, progressAction);

      LogHelp.LogEnd(MethodBase.GetCurrentMethod());
    }
  }
}
