using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectoryCreatedWatcher;

/// <summary>
/// Runs a command when a directory or subdirectory is added to a parent folder
/// </summary>
internal class Monitor : IDisposable
{
   private bool _isDisposed;
   readonly FileSystemWatcher _watcher;
   readonly List<Process> _processes = new();
   public TimeSpan DelayBeforeExecution { get; init; } = TimeSpan.Zero;
   public ILogger Logger { get; init; } = new ConsoleLogger();

   public string Command {  get;  }
   public string[] CommandArgs { get; }
   public const string PathPlaceholder = "<<path>>";

   /// <summary>
   /// 
   /// </summary>
   /// <param name="toWatch"></param>
   /// <param name="watchSubdirs"></param>
   /// <param name="command"></param>
   /// <param name="commandArgs">Monitor.PathPlaceholder, if found, is replaced with the directory created</param>
   public Monitor(DirectoryInfo toWatch, bool watchSubdirs, string command, string[] commandArgs)
   {
      Command = command;
      CommandArgs = commandArgs;
      _watcher = new(toWatch.FullName)
      {
         IncludeSubdirectories = watchSubdirs,
         NotifyFilter = NotifyFilters.DirectoryName,
         
      };
      _watcher.Created += DirectoryCreated;
   }

   private async void DirectoryCreated(object sender, FileSystemEventArgs e)
   {
      await RunCommandAfterDelay(e.FullPath);
   }
   private async Task RunCommandAfterDelay(string dir)
   {
      Logger.Log("Created: " + dir);
      await Task.Delay(DelayBeforeExecution);
      Logger.Log($"Delay completed for {dir}.");

      RunCommand(dir);
   }
   private void RunCommand(string dir)
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
            WorkingDirectory = dir
         };

         Logger.Log($"Running command {Command} {string.Join(" ",cmdArgs)}");
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
      Logger.Log($"Process for path {dir} exited with status {newProcess.ExitCode}");
      newProcess.Dispose();
      _processes.Remove(newProcess);

   }

   public async Task Start(CancellationToken cancellationToken)
   {
      _watcher.EnableRaisingEvents = true;

      Logger.Log($"Waiting for new directories in {_watcher.Path} to execute command {Command} {string.Join(" ", CommandArgs)}");

      await Task.Delay(-1, cancellationToken);
   }
   protected virtual void Dispose(bool disposing)
   {
      if (!_isDisposed)
      {
         if (disposing)
         {
            // dispose managed state (managed objects)
            _watcher?.Dispose();

            _processes.ToList().ForEach(p => p.Dispose());//run on copy as killing processes removes from list
         }
         _isDisposed = true;
      }
   }

   public void Dispose()
   {
      // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
      Dispose(disposing: true);
      GC.SuppressFinalize(this);
   }
}
