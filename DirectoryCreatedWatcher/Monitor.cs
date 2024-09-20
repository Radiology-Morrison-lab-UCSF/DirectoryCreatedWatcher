using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectoryCreatedWatcher;

/// <summary>
/// Runs a command when a directory or subdirectory is added to a parent folder
/// </summary>
internal abstract class Monitor : IDisposable
{
   private bool _isDisposed;
   readonly FileSystemWatcher _watcher;
   public TimeSpan DelayBeforeExecution { get; init; } = TimeSpan.Zero;
   public ILogger Logger { get; init; } = new ConsoleLogger();

   /// <summary>
   /// 
   /// </summary>
   /// <param name="toWatch"></param>
   /// <param name="watchSubdirs"></param>
   /// <param name="command"></param>
   /// <param name="commandArgs">Monitor.PathPlaceholder, if found, is replaced with the directory created</param>
   public Monitor(DirectoryInfo toWatch, bool watchSubdirs)
   {
      _watcher = new(toWatch.FullName)
      {
         IncludeSubdirectories = watchSubdirs,
         NotifyFilter = NotifyFilters.DirectoryName,
      };
      _watcher.Created += DirectoryCreated;
   }

   public async Task Start(CancellationToken? cancellationToken=null)
   {
      _watcher.EnableRaisingEvents = true;

      Logger.Log($"Waiting for new directories in {_watcher.Path}");

      if (cancellationToken.HasValue)
      {
         await Task.Delay(-1, cancellationToken.Value);
      }
      else
      {
         await Task.Delay(-1);
      }
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
   protected abstract void RunCommand(string dir);


   public void Dispose()
   {
      // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
      Dispose(disposing: true);
      GC.SuppressFinalize(this);
   }

   protected virtual void Dispose(bool disposing)
   {
      if (!_isDisposed)
      {
         if (disposing)
         {
            // dispose managed state (managed objects)
            _watcher?.Dispose();
         }
         _isDisposed = true;
      }
   }

}
