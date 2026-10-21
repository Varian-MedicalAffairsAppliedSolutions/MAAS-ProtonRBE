using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Serilog;

namespace ProtonRBE
{
  public static class LogHelp
  {
    public static void LogStart(string message)
    {
      Log.Debug($"Start: {message}");
    }

    public static void LogEnd(string message)
    {
      Log.Debug($"End: {message}");
    }

    public static void LogStart(MethodBase methodBase)
    {
      LogStart(methodBase.Name);
    }

    public static void LogEnd(MethodBase methodBase)
    {
      LogEnd(methodBase.Name);
    }
  }
}
