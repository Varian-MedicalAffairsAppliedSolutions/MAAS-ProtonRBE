using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace ProtonRBE
{
  public class RBE_Esapi_Service : IRBE_Esapi_Service
  {
    private readonly IonPlanSetup m_plan;

    public RBE_Esapi_Service(ScriptContext context)
    {
      PlanDoseRBEWeight = 1.0;
      m_plan = context.IonPlanSetup;
    }

    public double PlanDoseRBEWeight { get; set; }

    public string CreatePlanSetup(string planId, string planName = "", string planComment = "")
    {
      var newPlan = m_plan.Course.AddExternalPlanSetup(m_plan.StructureSet);

      planId = FindFreePlanId(planId);
      newPlan.Id = planId;

      var targetStructure = newPlan.StructureSet.Structures.FirstOrDefault(
        structure => structure.Id == m_plan.TargetVolumeID);
      if (targetStructure != null) {
        newPlan.SetTargetStructureIfNoDose(targetStructure, new StringBuilder());
      }

      newPlan.SetPrescription(
        m_plan.NumberOfFractions ?? 1, 
        m_plan.DosePerFraction, 
        m_plan.TreatmentPercentage);

      if (!string.IsNullOrEmpty(planName))
      {
        newPlan.Name = planName;
      }

      if (!string.IsNullOrEmpty(planComment))
      {
        newPlan.Comment = planComment;
      }
      else
      {
        newPlan.Comment = $"Plan generated with RBE Calculator Script";
      }
       
      return newPlan.Id;
    }

    public string FindFreePlanId(string basePlanId)
    {
      if (string.IsNullOrWhiteSpace(basePlanId))
      {
        basePlanId = "RBE";
      }

      int index = basePlanId.Length - 1;
      while (index >= 0 && char.IsDigit(basePlanId[index]))
      {
        index--;
      }

      string basePart = basePlanId.Substring(0, index + 1);
      string numericSuffix = basePlanId.Substring(index + 1);

      int planIdPostfix = 1;
      if (!string.IsNullOrEmpty(numericSuffix) && int.TryParse(numericSuffix, out int parsedPostfix))
      {
        planIdPostfix = parsedPostfix;
      }

      string testPlanId = basePlanId;
      while (m_plan.Course.PlanSetups.Any(x => string.Equals(x.Id, testPlanId, StringComparison.OrdinalIgnoreCase)))
      {
        testPlanId = $"{basePart}{planIdPostfix}";
        planIdPostfix++;
      }

      return testPlanId;
    }
    
    public async Task CalculateRBEValuesAsync(string planId, Func<DoseValue, double, DoseValue> RBEFunction, Action<double> progress)
    {
      Log.Information($"Initializing RBE calculation");

      #region Set up stopwatches

      var sw_Global = new Stopwatch(); sw_Global.Start();
      var sw_GetVoxels = new Stopwatch();
      var sw_VoxelToDose = new Stopwatch();
      var sw_StoreValues = new Stopwatch();
      var sw_RBEFunction = new Stopwatch();
      var sw_DoseToVoxel = new Stopwatch();
      var sw_SetVoxels = new Stopwatch();

      #endregion

      #region Set up evaluation dose

      int xSize = m_plan.Dose.XSize;
      int ySize = m_plan.Dose.YSize;
      int zSize = m_plan.Dose.ZSize;

      var rbeSetup = m_plan.Course.ExternalPlanSetups.FirstOrDefault(x => x.Id == planId);
      if (rbeSetup == null)
      {
        throw new InvalidOperationException($"Plan with ID {planId} not found");
      }

      var rbeEvaluationDose = rbeSetup.CopyEvaluationDose(m_plan.Dose);

      #endregion

      Log.Information("Calculating RBEs");

      var doseVoxels = new uint[xSize, ySize];
      var letVoxels = new uint[xSize, ySize];

      var voxelToDoseConverter = new VoxelToDoseValue(m_plan.Dose);
      var voxelToLETConverter = new VoxelToLETValue(m_plan.LETMatrix);

      bool lowRBEDoseWarningShown = false;
      bool lowRBEVoxelWarningShown = false;

      for (int z = 0; z < zSize; z++)
      {
        {
          progress(z / (double)zSize * 100.0);
          await Task.Run(async () => { await Task.Delay(1); });
        }

        sw_GetVoxels.Start();
        m_plan.Dose.GetVoxels(z, doseVoxels);
        m_plan.LETMatrix.GetVoxels(z, letVoxels);
        sw_GetVoxels.Stop();

        var rbeEvaluationDoseVoxels = new int[xSize, ySize];

        for (int x = 0; x < xSize; x++)
        {
          for (int y = 0; y < ySize; y++)
          {
            sw_VoxelToDose.Start();
            var doseValue = voxelToDoseConverter.Convert(doseVoxels[x, y]);
            var letValue = voxelToLETConverter.Convert(letVoxels[x, y]);
            sw_VoxelToDose.Stop();

            if (doseValue.Dose < 1e-3)
              continue;

            sw_RBEFunction.Start();
            doseValue = doseValue / PlanDoseRBEWeight;
            var rbeDose = RBEFunction(doseValue, letValue.LETValue);
            if (rbeDose.Dose < 0)
            {
              if (!lowRBEDoseWarningShown)
              {
                Log.Warning($"Encountered negative RBE dose value in plan {planId}. Overriding value to 0 Gy." +
                            $"(Dose: {doseValue}, LET: {letValue}, RBE Dose: {rbeDose})");
                lowRBEDoseWarningShown = true;
              }

              rbeDose = new DoseValue(0, rbeDose.Unit);
            }
            sw_RBEFunction.Stop();

            sw_DoseToVoxel.Start();
            int voxelValue = rbeEvaluationDose.DoseValueToVoxel(rbeDose);
            if (voxelValue < 0)
            {
              if (!lowRBEVoxelWarningShown)
              {
                Log.Warning($"Encountered negative RBE Voxel value in plan {planId}. Overriding value to 0." +
                            $"(Dose: {doseValue}, LET: {letValue}, RBE Dose: {rbeDose}, RBE Voxel: {voxelValue})");
                lowRBEVoxelWarningShown = true;
              }

              voxelValue = 0;
            }
            rbeEvaluationDoseVoxels[x, y] = voxelValue;
            sw_DoseToVoxel.Stop();
          }
        }

        sw_SetVoxels.Start();
        rbeEvaluationDose.SetVoxels(z, rbeEvaluationDoseVoxels);
        sw_SetVoxels.Stop();

      }

      Log.Debug($"GetVoxels: {sw_GetVoxels.Elapsed}");
      Log.Debug($"VoxelToDose: {sw_VoxelToDose.Elapsed}");
      Log.Debug($"StoreValues: {sw_StoreValues.Elapsed}");
      Log.Debug($"RBEFunction: {sw_RBEFunction.Elapsed}");
      Log.Debug($"DoseToVoxel: {sw_DoseToVoxel.Elapsed}");
      Log.Debug($"SetVoxels: {sw_SetVoxels.Elapsed}");

      Log.Information("RBE calculation complete. (Took {0:D2}:{1:D2})",
        sw_Global.Elapsed.Minutes, sw_Global.Elapsed.Seconds);

    }

    public bool IsAsync => false;

    private class VoxelToDoseValue
    {
      private readonly double a;
      private readonly double b;

      private readonly DoseValue.DoseUnit unit;

      public VoxelToDoseValue(PlanningItemDose dose)
      {
        b = dose.VoxelToDoseValue(0).Dose;
        a = dose.VoxelToDoseValue(1).Dose - b;

        unit = dose.VoxelToDoseValue(0).Unit;
      }

      public DoseValue Convert(uint voxel)
      {
        return new DoseValue(a * voxel + b, unit);
      }

      public DoseValue Convert(int voxel)
      {
        return new DoseValue(a * voxel + b, unit);
      }
    }

    private class VoxelToLETValue
    {
      private readonly double a;
      private readonly double b;

      private readonly LETMatrixValue.LETMatrixUnit unit;

      public VoxelToLETValue(LETMatrix let)
      {
        b = let.VoxelToLETMatrixValue(0).LETValue;
        a = let.VoxelToLETMatrixValue(1).LETValue - b;

        unit = let.VoxelToLETMatrixValue(0).Unit;
      }

      public LETMatrixValue Convert(uint voxel)
      {
        return new LETMatrixValue(a * voxel + b, unit);
      }
      public LETMatrixValue Convert(int voxel)
      {
        return new LETMatrixValue(a * voxel + b, unit);
      }
    }
  }
}
