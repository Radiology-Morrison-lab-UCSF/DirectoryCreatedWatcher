namespace DirectoryCreatedWatcher;

internal class Program
{
   static async Task Main(string[] args)
   {
      if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
      {
         PrintHelp();
         Environment.Exit(1);
      }

      if (args.Length < 3)
      {
         Console.WriteLine("Expected 3+ arguments");
         Environment.Exit(1);
      }

      string directoryToMonitor = Path.GetFullPath(args[0]);

      if (!Directory.Exists(directoryToMonitor))
      {
         Console.WriteLine($"{directoryToMonitor} not found");
         Environment.Exit(1);
      }

      // Pass Args
      TimeSpan delay = TimeSpan.Parse(args[1]);
      string command = args[2];
      string[] commandArgs = args[3..];

      var tokenSource = new CancellationTokenSource();

      ProcessRunningMonitor m = new(new DirectoryInfo(directoryToMonitor), true, command, commandArgs)
      {
         DelayBeforeExecution = delay
      };
      await m.Start(tokenSource.Token);

   }

   private static void PrintHelp()
   {
      Console.WriteLine(@"
Monitors a directory for changes. When a subdirectory is added, this will execute the provide command. 

Use
DirectoryCreatedWatcher </path/to/my/dir> <delay> <command> <command arguments>

For command arguments, write <<path>> to indicate where the full path of the directory should be substituted

Example:

./DirectoryCreatedWatcher /path/to/my/dir 1:00:00 7z a myzip.7z <<path>>

when /path/to/my/dir/newdir is created this will wait one hour, then execute 
7z a myzip.7z /path/to/my/dir/newdir/

");
   }

   private static void OnCreated(object source, FileSystemEventArgs e)
   {
      if (Directory.Exists(e.FullPath))
      {
         Console.WriteLine($"Directory created: {e.FullPath}");
      }
   }
}
