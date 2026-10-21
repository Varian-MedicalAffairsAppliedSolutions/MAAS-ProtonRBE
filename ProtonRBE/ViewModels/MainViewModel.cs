using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;

using Serilog;
using Prism.Commands;
using System.Threading.Tasks;

namespace ProtonRBE
{
  public class MainViewModel : INotifyPropertyChanged
  {
    private readonly IRBE_Esapi_Service m_EsapiService;
    private readonly RBE_Service m_RBEService;

    /// <summary>
    /// Displays a notification (title, message, kind). Set by the hosting window.
    /// </summary>
    public Func<string, string, NotificationKind, Task> NotificationHandler { get; set; }

    private async Task ShowNotification(string title, string message, NotificationKind kind)
    {
      if (NotificationHandler != null)
      {
        await NotificationHandler(title, message, kind);
      }
    }

    #region RBE Model Handling

    private ObservableCollection<IRBE_Model> m_RBEModels;
    public ObservableCollection<IRBE_Model> RBE_Models
    {
      get { return m_RBEModels; }
      set
      {
        m_RBEModels = value;
        OnPropertyChanged();
      }
    }

    private IRBE_Model m_SelectedRBEModel;
    public IRBE_Model SelectedRBEModel
    {
      get { return m_SelectedRBEModel; }
      set
      {
        m_SelectedRBEModel = value;
        OnPropertyChanged();
      }
    }

    #endregion

    #region State and Progress

    private bool m_Calculating;
    public bool Calculating
    {
      get { return m_Calculating; }
      set
      {
        m_Calculating = value;
        OnPropertyChanged(nameof(CanStartCalculation));
        OnPropertyChanged();
      }
    }

    public bool CanStartCalculation
    {
      get
      {
        return !Calculating
          && SelectedRBEModel != null
          && SelectedRBEModel.CalculationParameters != null
          && SelectedRBEModel.CalculationParameters.All(p => p.IsValueValid);
      }
    }

    private double m_PercentProgress;
    public double ProgressPercent
    {
      get { return m_PercentProgress; }
      set
      {
        m_PercentProgress = value;
        OnPropertyChanged();
        OnPropertyChanged(nameof(ProgressMessage));
      }
    }

    private string m_ProgressMessage;
    public string ProgressMessage {
      get
      {
        return m_ProgressMessage;
      }

      set
      {
        m_ProgressMessage = value;
        OnPropertyChanged();
      }
    }

    #endregion

    #region Plan Dose RBE Weight

    private bool m_PlanDoseIsRBEWeighted;
    public bool PlanDoseIsRBEWeighted
    {
      get { return m_PlanDoseIsRBEWeighted; }
      set
      {
        m_PlanDoseIsRBEWeighted = value;
        SetRBEServicePlanDoseRBEWeight();
        OnPropertyChanged();
      }
    }

    private double m_PlanDoseRBEWeight;
    public double PlanDoseRBEWeight
    {
      get 
      { 
        return m_PlanDoseRBEWeight;
      }
      set
      {
        var errorString = m_RBEService.ValidateRBEWeightString(value);
        if (!string.IsNullOrEmpty(errorString))
        {
          IsPlanDoseRBEWeightValid = false;
          PlanDoseRBEWeightValidationMessage = errorString;
          return;
        }

        IsPlanDoseRBEWeightValid = true;
        PlanDoseRBEWeightValidationMessage = String.Empty;

        m_PlanDoseRBEWeight = value;
        SetRBEServicePlanDoseRBEWeight();
        OnPropertyChanged();
      }
    }

    private bool m_IsPlanDoseRBEWeightValid;
    public bool IsPlanDoseRBEWeightValid
    {
      get { return m_IsPlanDoseRBEWeightValid; }
      private set
      {
        if (m_IsPlanDoseRBEWeightValid == value)
          return;

        m_IsPlanDoseRBEWeightValid = value;
        OnPropertyChanged();
      }
    }

    private string m_PlanDoseRBEWeightValidationMessage;
    public string PlanDoseRBEWeightValidationMessage
    {
      get { return m_PlanDoseRBEWeightValidationMessage; }
      private set
      {
        if (m_PlanDoseRBEWeightValidationMessage == value)
          return;

        m_PlanDoseRBEWeightValidationMessage = value;
        OnPropertyChanged();
      }
    }

    private void SetRBEServicePlanDoseRBEWeight()
    {
      if (PlanDoseIsRBEWeighted)
      {
        m_RBEService.PlanDoseRBEWeight = m_PlanDoseRBEWeight;
      }
      else
      {
        m_RBEService.PlanDoseRBEWeight = 1.0;
      }
    }

    private RBEWeightValidationRule m_RBEWeightValidationRule = new RBEWeightValidationRule();
    public RBEWeightValidationRule RBEWeightValidationRule => m_RBEWeightValidationRule;

    #endregion

    #region Plan ID Validation

    public bool IsPlanIdValid
    {
      get { return String.IsNullOrEmpty(m_PlanIdValidationMessage); }
    }

    private string m_PlanIdValidationMessage;
    public string PlanIdValidationMessage
    {
      get { return m_PlanIdValidationMessage; }
      private set
      {
        if (m_PlanIdValidationMessage == value) { return; }

        m_PlanIdValidationMessage = value;
        OnPropertyChanged(nameof(IsPlanIdValid));
        OnPropertyChanged();
      }
    }

    public void ValidatePlanId(string newId)
    {
      newId = newId?.Trim() ?? "";

      if (string.IsNullOrEmpty(newId))
      {
        PlanIdValidationMessage = "Enter plan ID";
        return;
      }

      if(newId.Count() > 12)
      {
        PlanIdValidationMessage = "Plan ID too long";
        return;
      }

      if (String.Compare(newId, SelectedRBEModel.PlanId) != 0 )
      {
        SelectedRBEModel.PlanId = newId;
      }

      PlanIdValidationMessage = String.Empty;
    }

    #endregion

    #region Parameter Validation

    public void SetParameterValue(string parameterName, string parameterValue)
    {
      var parameter = SelectedRBEModel?.CalculationParameters.FirstOrDefault(p => p.Name == parameterName);
      if(parameter is null)
      {
        return;
      }

      parameter.ValueString = parameterValue;
      SelectedRBEModel.TriggerPlanCommentChange();
      OnPropertyChanged(nameof(CanStartCalculation));
    }

    #endregion

    public DelegateCommand ApplyCommand { get; set; }

    public MainViewModel(IRBE_Esapi_Service esapi_service)
    {
      m_EsapiService = esapi_service;
      m_RBEService = new RBE_Service(m_EsapiService);

      RBE_Models = new ObservableCollection<IRBE_Model>(m_RBEService.GetModels());
      SelectedRBEModel = RBE_Models.Last(); // Not null checked in RBE_Service constructor
      ValidatePlanId(SelectedRBEModel.PlanId);

      m_Calculating = false;
      ProgressPercent = 0;
      ProgressMessage = "";

      m_PlanDoseRBEWeight = m_RBEService.PlanDoseRBEWeight;
      m_PlanDoseIsRBEWeighted = m_PlanDoseRBEWeight != 1.0;
      IsPlanDoseRBEWeightValid = true;

      ApplyCommand = new DelegateCommand(OnApply);
    }

    public bool ValidatePlanId(string planId, out string errorMsg)
    {
      errorMsg = "";

      var normalizedPlanId = planId ?? string.Empty;
      if (RBE_Models.Any(x => string.Equals(x.PlanId, normalizedPlanId, StringComparison.OrdinalIgnoreCase)))
      {
        errorMsg = Resources.ResourceStrings.Error_PlanIdExists;
        return false;
      }

      var esapiCheck = ""; // m_EsapiService.CheckNewPlanId(planId);
      if (!string.IsNullOrEmpty(esapiCheck))
      {
        errorMsg = esapiCheck;
        return false;
      }

      return true;
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private async void OnApply()
    {
      if (SelectedRBEModel is null)
      {
        ProgressMessage = "No RBE model selected.";
        return;
      }

      Calculating = true;

      m_RBEService.SetSelectedModel(SelectedRBEModel);
      m_RBEService.PlanDoseRBEWeight = PlanDoseIsRBEWeighted ? PlanDoseRBEWeight : 1.0;

      {
        ProgressPercent = 20;
        ProgressMessage = "Creating RBE Plan...";
        await Task.Run(async () => { await Task.Delay(10); }); // Trick to make the UI update before the calculation starts
      }

      string newPlanId = string.Empty;
      try
      {
        newPlanId = m_RBEService.CreateRBEPlan();
      }
      catch (Exception e)
      {
        Log.Error(e.Message);
        await ShowNotification("RBE Calculation Failed",
          $"Failed to create new plan \"{newPlanId}\".\n{e.Message}", NotificationKind.Error);
        Calculating = false;
        return;
      }
       
      Action<double> progressAction = (percent =>
      {
        ProgressPercent = 20 + 0.8 * percent;
        ProgressMessage = $"Calculating RBEs... {ProgressPercent:F1}%";
        //await Task.Run(async () => { await Task.Delay(10); }); // Trick to make the UI update before the calculation starts
      });

      try
      {
        await m_RBEService.CalculateRBEValuesAsync(progressAction);

        ProgressPercent = 100;
        ProgressMessage = "Done.";

        await ShowNotification("RBE Calculation Completed",
          $"Created new plan \"{newPlanId}\".", NotificationKind.Success);
      }
      catch (Exception e)
      {
        Log.Error(e.Message);
        await ShowNotification("RBE Calculation Failed",
          $"Failed to calculate RBE values for plan \"{newPlanId}\".\n{e.Message}", NotificationKind.Error);
      }
      finally
      {
        Calculating = false;
      }
    }
  }
}
