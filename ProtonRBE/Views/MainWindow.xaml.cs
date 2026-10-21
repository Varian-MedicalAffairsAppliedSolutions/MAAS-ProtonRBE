using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ProtonRBE
{
  public enum NotificationKind
  {
    None,
    Success,
    Error
  }

  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    private const int MaxNotifications = 2;
    private static readonly TimeSpan NotificationDuration = TimeSpan.FromSeconds(3);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int attrValue, int attrSize);

    public MainWindow(MainViewModel viewModel)
    {
      InitializeComponent();
      SourceInitialized += (s, e) => UseDarkTitleBar();
      this.DataContext = viewModel;
      if (viewModel != null)
        viewModel.NotificationHandler = ShowNotification;
    }

    private async void TextBlock_Reference_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
      var textBlock = sender as TextBlock;
      if (textBlock != null)
      {
        Clipboard.SetText(textBlock.Text);
        await ShowNotification("Copied to clipboard", string.Empty, NotificationKind.None);
      }
    }

    public async Task ShowNotification(string title, string message, NotificationKind kind)
    {
      Brush accent;
      switch (kind)
      {
        case NotificationKind.Success: accent = new SolidColorBrush(Color.FromRgb(0x00, 0x99, 0x99)); break;
        case NotificationKind.Error: accent = (Brush)FindResource("ErrorBrush"); break;
        default: accent = (Brush)FindResource("AccentBrush"); break;
      }

      var content = new StackPanel();
      content.Children.Add(new TextBlock
      {
        Text = title,
        FontSize = 14,
        FontWeight = FontWeights.SemiBold,
        TextWrapping = TextWrapping.Wrap,
        Foreground = (Brush)FindResource("PrimaryTextBrush")
      });
      if (!string.IsNullOrEmpty(message))
      {
        content.Children.Add(new TextBlock
        {
          Text = message,
          FontSize = 12,
          Margin = new Thickness(0, 4, 0, 0),
          TextWrapping = TextWrapping.Wrap,
          Foreground = (Brush)FindResource("SecondaryTextBrush")
        });
      }

      var toast = new Border
      {
        Width = 350,
        Margin = new Thickness(0, 8, 0, 0),
        Padding = new Thickness(16, 12, 16, 12),
        CornerRadius = new CornerRadius(4),
        Background = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A)),
        BorderBrush = accent,
        BorderThickness = new Thickness(4, 0, 0, 0),
        Opacity = 0,
        Child = content
      };

      while (NotificationArea.Children.Count >= MaxNotifications)
        NotificationArea.Children.RemoveAt(0);

      NotificationArea.Children.Add(toast);
      toast.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(200)));

      await Task.Delay(NotificationDuration);

      var fadeOut = new DoubleAnimation(0, TimeSpan.FromMilliseconds(200));
      fadeOut.Completed += (s, a) => NotificationArea.Children.Remove(toast);
      toast.BeginAnimation(OpacityProperty, fadeOut);
    }

    private void TextBox_PlanId_TextChanged(object sender, TextChangedEventArgs e)
    {
      (DataContext as MainViewModel).ValidatePlanId((sender as TextBox).Text);
    }

    private void TextBox_Parameter_TextChanged(object sender, TextChangedEventArgs e)
    {
      var textBox = sender as TextBox;
      (DataContext as MainViewModel).SetParameterValue(textBox.Tag.ToString(), textBox.Text);
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
      var textBox = (sender as FrameworkElement)?.TemplatedParent as TextBox;
      if (textBox != null)
      {
        textBox.Clear();
        textBox.Focus();
      }
    }

    private void SpinButton_Click(object sender, RoutedEventArgs e)
    {
      var button = sender as RepeatButton;
      var textBox = button?.TemplatedParent as TextBox;
      if (textBox == null)
        return;

      double.TryParse(textBox.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out double value);
      var direction = int.Parse(button.Tag.ToString(), CultureInfo.InvariantCulture);
      var step = TextBoxAssist.GetStep(textBox);
      var newValue = Math.Round(value + direction * step, 10);
      newValue = Math.Max(TextBoxAssist.GetMinimum(textBox), Math.Min(TextBoxAssist.GetMaximum(textBox), newValue));

      textBox.Text = newValue.ToString(CultureInfo.CurrentCulture);
      textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
    }

    private void UseDarkTitleBar()
    {
      var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
      int enabled = 1;
      try
      {
        if (DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref enabled, sizeof(int)) != 0)
          DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref enabled, sizeof(int));
      }
      catch (DllNotFoundException)
      {
      }
      catch (EntryPointNotFoundException)
      {
      }
    }

    private void Button_Close_Click(object sender, RoutedEventArgs e)
    {
      Close();
    }
  }
}
