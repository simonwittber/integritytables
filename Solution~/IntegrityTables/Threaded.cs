using System;
using System.Collections.Generic;
using System.Threading;

namespace IntegrityTables;

public class Threaded
{
    private static readonly int ProcessorCount = Environment.ProcessorCount;
    private static readonly Thread[] Workers = new Thread[ProcessorCount];
    private static readonly ManualResetEventSlim[] StartEvents = new ManualResetEventSlim[ProcessorCount];
    private static readonly ManualResetEventSlim[] DoneEvents = new ManualResetEventSlim[ProcessorCount];
    private static Action[] WorkItems = new Action[ProcessorCount];
    private static volatile bool _shutdown;

    static Threaded()
    {
        for (int i = 0; i < ProcessorCount; i++)
        {
            var threadIndex = i;
            StartEvents[i] = new ManualResetEventSlim(false);
            DoneEvents[i] = new ManualResetEventSlim(false);

            Workers[i] = new Thread(() => WorkerLoop(threadIndex))
            {
                IsBackground = true,
                Name = $"CustomParallel-{threadIndex}"
            };
            Workers[i].Start();
        }

        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
    }

    private static void WorkerLoop(int threadIndex)
    {
        while (!_shutdown)
        {
            StartEvents[threadIndex].Wait();
            if (_shutdown) break;

            var work = WorkItems[threadIndex];
            work?.Invoke();

            StartEvents[threadIndex].Reset();
            DoneEvents[threadIndex].Set();
        }
    }

    private static void OnProcessExit(object sender, EventArgs e)
    {
        Shutdown();
    }

    public static void Shutdown()
    {
        if (_shutdown) return;

        _shutdown = true;

        // Signal all threads to wake up and exit
        for (int i = 0; i < ProcessorCount; i++)
        {
            StartEvents[i].Set();
        }

        // Wait for threads to complete with timeout
        for (int i = 0; i < ProcessorCount; i++)
        {
            Workers[i].Join(TimeSpan.FromMilliseconds(100));
        }

        // Dispose synchronization objects
        for (int i = 0; i < ProcessorCount; i++)
        {
            StartEvents[i]?.Dispose();
            DoneEvents[i]?.Dispose();
        }
    }
    
    static IEnumerable<Tuple<int, int>> CreateBalancedPartitions(int start, int end, int partitionCount = -1)
    {
        if (partitionCount <= 0)
            partitionCount = Environment.ProcessorCount;

        var totalWork = end - start;
        var workPerPartition = totalWork / partitionCount;
        var remainder = totalWork % partitionCount;

        for (int i = 0; i < partitionCount; i++)
        {
            var partitionStart = start + i * workPerPartition + Math.Min(i, remainder);
            var partitionEnd = start + (i + 1) * workPerPartition + Math.Min(i + 1, remainder);

            if (partitionStart < end)
                yield return Tuple.Create(partitionStart, partitionEnd);
        }
    }

    /// <summary>
    /// Execute an action for each integer in the range [start, end) using multiple threads.
    /// The action receives the start and end of each partition.
    /// This method will partition the range into approximately equal parts based on the number of available processors
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="action"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public static void ForEach(int start, int end, Action<int, int> action)
    {
        var partition = CreateBalancedPartitions(start, end, ProcessorCount);
        if (action == null) throw new ArgumentNullException(nameof(action));

        int index = 0;
        foreach (var range in partition)
        {
            if (index >= ProcessorCount) break;

            WorkItems[index] = () => action(range.Item1, range.Item2);
            StartEvents[index].Set();
            index++;
        }

        for (int i = 0; i < index; i++)
        {
            DoneEvents[i].Wait();
            DoneEvents[i].Reset();
        }
    }
  

}


