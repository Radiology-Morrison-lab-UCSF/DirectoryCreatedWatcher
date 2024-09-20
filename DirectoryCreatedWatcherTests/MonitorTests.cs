
using System.Collections.Concurrent;

namespace DirectoryCreatedWatcherTests;

[TestClass]
public class MonitorTests
{

   class FuncRunnerMonitor(DirectoryInfo toWatch, bool watchSubdirs) : DirectoryCreatedWatcher.Monitor(toWatch, watchSubdirs)
   {
      public required Action<string> Action { get; init; }
      protected override void RunCommand(string dir)
      {
         Action.Invoke(dir);
      }
   }

   [TestMethod]
   [DataRow(false, false)]
   [DataRow(false, true)]
   [DataRow(true, false)]
   [DataRow(true, true)]
   public async Task TestMethod1(bool subdirs, bool taskShorterThanDelay)
   {
      ConcurrentBag<string> created = new ConcurrentBag<string>();
      int completingCount = 0;

      DirectoryInfo tempDir = Directory.CreateTempSubdirectory(nameof(MonitorTests));

      CancellationTokenSource cts = new CancellationTokenSource();
      Task monitoringTask;
      try
      {
         DirectoryInfo createIn = subdirs ? Directory.CreateDirectory(Path.Join(tempDir.FullName, "sub")) : tempDir;

         FuncRunnerMonitor monitor = new(tempDir, true)
         {
            Action = AddAndDelay,
            DelayBeforeExecution = TimeSpan.FromSeconds(1)
         };
         monitoringTask = monitor.Start(cts.Token);

         string pathA = Path.Join(createIn.FullName, "a");
         Directory.CreateDirectory(pathA);

         await Task.Delay(TimeSpan.FromMilliseconds(100));
         // The task should not have executed yet
         Assert.AreEqual(0, created.Count);

         await Task.Delay(TimeSpan.FromSeconds(1));
         // The task should have begun but not completed
         Assert.AreEqual(1, created.Count);
         Assert.AreEqual(pathA, created.Single());
         Assert.AreEqual(0, completingCount);

         await Task.Delay(TimeSpan.FromMilliseconds(2500));

         // All tasks should have completed by now
         Assert.AreEqual(1, completingCount);


         // Run two at a time
         string pathB = Path.Join(createIn.FullName, "b");
         string pathC = Path.Join(createIn.FullName, "c");
         Directory.CreateDirectory(pathB);
         Directory.CreateDirectory(pathC);
         await Task.Delay(TimeSpan.FromMilliseconds(100));
         // The tasks should not have executed yet
         Assert.AreEqual(1, created.Count);

         await Task.Delay(TimeSpan.FromSeconds(1));
         // The tasks should have begun but not completed
         CollectionAssert.AreEquivalent(new string[] { pathA, pathB, pathC }, created);
         Assert.AreEqual(1, completingCount);
         await Task.Delay(TimeSpan.FromMilliseconds(2500));

         // All tasks should have completed by now
         Assert.AreEqual(3, completingCount);

      }

      finally
      {
         Directory.Delete(tempDir.FullName, true);
      }
      cts.Cancel();
      await Task.Delay(TimeSpan.FromMilliseconds(50));
      Assert.IsTrue(monitoringTask.IsCanceled);


      void AddAndDelay(string dir)
      { 
            created.Add(dir);
         Thread.Sleep(taskShorterThanDelay ? 200 : 2000);
         completingCount++;
      }
   }
}