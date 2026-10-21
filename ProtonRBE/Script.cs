using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Imaging;
using MAAS.Common.EulaVerification;
using ProtonRBE;

using Serilog;

using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

[assembly: ESAPIScript(IsWriteable = true)]

namespace VMS.TPS
{
  public class Script
  {
    // Define the project information for EULA verification
    private const string PROJECT_NAME = "ProtonRBE";
    private const string PROJECT_VERSION = "1.0.0";
    private const string LICENSE_URL = "https://varian-medicalaffairsappliedsolutions.github.io/MAAS-ProtonRBE";
    private const string GITHUB_URL = "https://github.com/Varian-MedicalAffairsAppliedSolutions/MAAS-ProtonRBE";

    private const string m_logFileDir = @"C:\VMSOS\Log\Application\ProtonRBE\";
#if DEBUG
    private const bool m_debug = true;
#else
    private const bool m_debug = false;
#endif

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Execute(ScriptContext context)
    {
      try
      {
        if (!VerifyLicenseAndExpiration())
          return;

        if (!CheckContext(context))
          return;

        StartLogging();
        var service = new RBE_Esapi_Service(context);
        Execute(service);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"An error occurred: {ex.Message}\n\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private bool VerifyLicenseAndExpiration()
    {
      // Check license acceptance first with version support
      var eulaVerifier = new EulaVerifier(PROJECT_NAME, PROJECT_VERSION, LICENSE_URL);

      var eulaConfig = EulaConfig.Load(PROJECT_NAME);
      if (eulaConfig.Settings == null)
      {
        eulaConfig.Settings = new ApplicationSettings();
      }

      if (!eulaVerifier.IsEulaAccepted())
      {
        MessageBox.Show(
            $"This version of {PROJECT_NAME} (v{PROJECT_VERSION}) requires license acceptance before first use.\n\n" +
            "You will be prompted to provide an access code. Please follow the instructions to obtain your code.",
            "License Acceptance Required",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        BitmapImage qrCode = null;
        try
        {
          string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
          qrCode = new BitmapImage(new Uri($"pack://application:,,,/{assemblyName};component/Resources/qrcode.png"));
        }
        catch (Exception ex)
        {
          System.Diagnostics.Debug.WriteLine($"Error loading QR code: {ex.Message}");
        }

        if (!eulaVerifier.ShowEulaDialog(qrCode))
        {
          MessageBox.Show(
              "License acceptance is required to use this application.\n\n" +
              "The application will now close.",
              "License Not Accepted",
              MessageBoxButton.OK,
              MessageBoxImage.Warning);
          return false;
        }
      }

      var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
      var noexp_path = Path.Combine(path, "NOEXPIRE");
      bool foundNoExpire = File.Exists(noexp_path);

      var settings = eulaConfig.Settings;

      var asmCa = Assembly.GetExecutingAssembly().CustomAttributes.FirstOrDefault(ca => ca.AttributeType == typeof(AssemblyExpirationDate));
      DateTime exp;
      var provider = new CultureInfo("en-US");
      DateTime.TryParse(asmCa.ConstructorArguments.FirstOrDefault().Value as string, provider, DateTimeStyles.None, out exp);

      if (exp < DateTime.Now && !foundNoExpire)
      {
        MessageBox.Show($"Application has expired. Newer builds with future expiration dates can be found here: {GITHUB_URL}");
        return false;
      }

      string msg = $"The current {PROJECT_NAME} application is provided AS IS as a non-clinical, research only tool in evaluation only. The current " +
      $"application will only be available until {exp.Date} after which the application will be unavailable. " +
      "By Clicking 'Yes' you agree that this application will be evaluated and not utilized in providing planning decision support\n\n" +
      $"Newer builds with future expiration dates can be found here: {GITHUB_URL}\n\n" +
      "See the FAQ for more information on how to remove this pop-up and expiration";

      string msg2 = $"Application will only be available until {exp.Date} after which the application will be unavailable. " +
      "By Clicking 'Yes' you agree that this application will be evaluated and not utilized in providing planning decision support\n\n" +
      $"Newer builds with future expiration dates can be found here: {GITHUB_URL} \n\n" +
      "See the FAQ for more information on how to remove this pop-up and expiration";

      if (!foundNoExpire)
      {
        var res = MessageBox.Show(settings.Validated ? msg2 : msg, "Agreement  ", MessageBoxButton.YesNo);
        if (res == MessageBoxResult.No)
        {
          return false;
        }
      }

      return true;
    }

    private bool CheckContext(ScriptContext context)
    {
      if (context.Patient is null || context.IonPlanSetup is null ||
         !context.IonPlanSetup.IsDoseValid || context.IonPlanSetup.LETMatrix is null)
      {
        MessageBox.Show("Open a proton plan setup with valid dose and LET before running the script.",
                        PROJECT_NAME);
        return false;
      }

      try
      {
        context.Patient.BeginModifications();
      }
      catch (Exception e)
      {
        MessageBox.Show(e.Message, PROJECT_NAME);
        return false;
      }

      return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Execute(IRBE_Esapi_Service service)
    {
      StartLogging();

      var viewModel = new MainViewModel(service);
      var mainWindow = new MainWindow(viewModel);

      mainWindow.ShowDialog();
    }

    public void StartLogging()
    {
      if (Log.Logger != Serilog.Core.Logger.None)
        return;

      var logFilePath = Path.Combine(m_logFileDir,
          $"RBE_{DateTime.Now:yyyy-MM-dd@HH.mm.ss}.txt");
      var logFlushInterval = new TimeSpan(0, 0, 5);

#pragma warning disable CS0162
      if (m_debug)
      {
        Log.Logger = new LoggerConfiguration()
          .MinimumLevel.Debug()
          .WriteTo.File(logFilePath, flushToDiskInterval: logFlushInterval)
          .WriteTo.Console()
          .CreateLogger();
      }
      else
      {
        Log.Logger = new LoggerConfiguration()
          .MinimumLevel.Information()
          .WriteTo.File(logFilePath, flushToDiskInterval: logFlushInterval)
          .WriteTo.Console()
          .CreateLogger();
      }
#pragma warning restore CS0162

      Log.Information("Logging Started");
    }
  }
}
