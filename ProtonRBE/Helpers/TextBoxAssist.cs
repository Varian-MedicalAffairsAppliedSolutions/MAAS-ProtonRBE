using System.Windows;

namespace ProtonRBE
{
  /// <summary>
  /// Attached properties used by the MainWindow TextBox styles (hint, error, clear button, numeric spinner).
  /// </summary>
  public static class TextBoxAssist
  {
    public static readonly DependencyProperty HintProperty =
      DependencyProperty.RegisterAttached("Hint", typeof(string), typeof(TextBoxAssist), new PropertyMetadata(string.Empty));
    public static string GetHint(DependencyObject o) => (string)o.GetValue(HintProperty);
    public static void SetHint(DependencyObject o, string value) => o.SetValue(HintProperty, value);

    public static readonly DependencyProperty HasErrorProperty =
      DependencyProperty.RegisterAttached("HasError", typeof(bool), typeof(TextBoxAssist), new PropertyMetadata(false));
    public static bool GetHasError(DependencyObject o) => (bool)o.GetValue(HasErrorProperty);
    public static void SetHasError(DependencyObject o, bool value) => o.SetValue(HasErrorProperty, value);

    public static readonly DependencyProperty ErrorMessageProperty =
      DependencyProperty.RegisterAttached("ErrorMessage", typeof(string), typeof(TextBoxAssist), new PropertyMetadata(string.Empty));
    public static string GetErrorMessage(DependencyObject o) => (string)o.GetValue(ErrorMessageProperty);
    public static void SetErrorMessage(DependencyObject o, string value) => o.SetValue(ErrorMessageProperty, value);

    public static readonly DependencyProperty ShowClearButtonProperty =
      DependencyProperty.RegisterAttached("ShowClearButton", typeof(bool), typeof(TextBoxAssist), new PropertyMetadata(false));
    public static bool GetShowClearButton(DependencyObject o) => (bool)o.GetValue(ShowClearButtonProperty);
    public static void SetShowClearButton(DependencyObject o, bool value) => o.SetValue(ShowClearButtonProperty, value);

    public static readonly DependencyProperty MinimumProperty =
      DependencyProperty.RegisterAttached("Minimum", typeof(double), typeof(TextBoxAssist), new PropertyMetadata(double.MinValue));
    public static double GetMinimum(DependencyObject o) => (double)o.GetValue(MinimumProperty);
    public static void SetMinimum(DependencyObject o, double value) => o.SetValue(MinimumProperty, value);

    public static readonly DependencyProperty MaximumProperty =
      DependencyProperty.RegisterAttached("Maximum", typeof(double), typeof(TextBoxAssist), new PropertyMetadata(double.MaxValue));
    public static double GetMaximum(DependencyObject o) => (double)o.GetValue(MaximumProperty);
    public static void SetMaximum(DependencyObject o, double value) => o.SetValue(MaximumProperty, value);

    public static readonly DependencyProperty StepProperty =
      DependencyProperty.RegisterAttached("Step", typeof(double), typeof(TextBoxAssist), new PropertyMetadata(1.0));
    public static double GetStep(DependencyObject o) => (double)o.GetValue(StepProperty);
    public static void SetStep(DependencyObject o, double value) => o.SetValue(StepProperty, value);
  }
}
