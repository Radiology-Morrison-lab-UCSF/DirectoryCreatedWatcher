using System.Diagnostics;

namespace DirectoryCreatedWatcher;

/// <summary>
/// Runs a terminal command when a directory or subdirectory is added to a parent folder
/// </summary>
internal class ProcessRunningMonitor(DirectoryInfo toWatch, bool watchSubdirs, string command, string[] commandArgs) : Monitor(toWatch, watchSubdirs)
{
   readonly List<Process> _processes = new();

   public string Command { get; } = command;
   public string[] CommandArgs { get; } = commandArgs;
   public const string PathPlaceholder = "[path]";

   protected override void RunCommand(string dir)
   {
      Process? newProcess = null;
      try
      {
         char escapeChar = Environment.OSVersion.Platform == PlatformID.Win32NT ? '^' : '\\';
         dir = Path.GetFullPath(dir).Replace(" ", $"{escapeChar} ");
         IEnumerable<string> cmdArgs = CommandArgs.Select(a => a.Replace(PathPlaceholder, dir));
         ProcessStartInfo psi = new(Command, cmdArgs)
         {
            UseShellExecute = true,
            WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
         };

         Logger.Log($"Running command {Command} {string.Join(" ", cmdArgs)}");
         newProcess = Process.Start(psi);
      }
      catch (Exception ex)
      {
         Logger.Log("ERROR when initiating command:\n" + ex.ToString());
      }

      if (newProcess != null)
      {
         if (newProcess.HasExited)
         {
            OnProcessExit(dir, newProcess);
         }
         else
         {
            newProcess.Exited += (s, e) => OnProcessExit(dir, (Process)s!);
            _processes.Add(newProcess);
         }
      }
   }

   private void OnProcessExit(string dir, Process newProcess)
   {
      if (_processes.Remove(newProcess))
      {
         Logger.Log($"Process for path {dir} exited with status {newProcess.ExitCode}");
         newProcess.Dispose();
      }
   }


   protected override void Dispose(bool disposing)
   {
      base.Dispose(disposing);
      if (disposing)
      {
         foreach (Process p in _processes.ToList()) // dispose alters the collection
         {            
            p.Dispose();
         }
      }
   }
}
