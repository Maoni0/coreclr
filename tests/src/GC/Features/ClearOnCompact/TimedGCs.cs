using System;
using System.Runtime.InteropServices;
using System.Diagnostics;

// This is to test the performance of the clear on compact feature
// which clears memory in the free space created and end of seg space 
// during a compacting GC.
// And we want to be able to config the amount of free spaces created
// by adjusting the pins and the survived bytes.
// 
// We maintain a gen2 array with elements pinned at a certain interval,
// and we start to replace the non pinned elements in this array.
// We induce GCs and measure their speed.
//
// Run with concurrent GC disabled.
namespace testClearOnCompact
{
    class Program
    {
        // this is what we allocate for each element in the array.
        static int objSize = 4 * 1024;
        static ulong totalOldGenSize = 100 * 1024 * 1024;
        static int pinPercent = 1; // 1%
        static ulong numElements;
        static int interval;
        static Object[] steadyStateArray;
        static GCHandle[] gcPinnedHandles;
        static Stopwatch stopwatch = new Stopwatch();
        static int gen0CountAfterInit = 0;
        static int gen1CountAfterInit = 0;
        static int gen2CountAfterInit = 0;
        static int[] startCollectionCounts = new int[3];
        static int[] endCollectionCounts = new int[3];
        static int[] triggeredCollectionCounts = new int[3];

        static void TouchPage(byte[] b)
        {
            int size = b.Length;

            int pageSize = 4096;

            int numPages = size / pageSize;

            for (int i = 0; i < numPages; i++)
            {
                b[i * pageSize] = (byte)i;
            }
        }

        static void Allocate()
        {
            numElements = totalOldGenSize / (ulong)objSize;
            interval = (int)(numElements * (ulong)pinPercent / (ulong)100);

            gcPinnedHandles = new GCHandle[numElements * (ulong)pinPercent / (ulong)100];
            ulong handleIndex = 0;

            Console.WriteLine("creating {0}mb steady state, {1} elements of size {2}, every {3} is pinned",
                (totalOldGenSize / 1024 / 1024), numElements, objSize, interval);

            steadyStateArray = new Object[numElements];
            for (ulong i = 0; i < numElements; i++)
            {
                steadyStateArray[i] = new byte[objSize - 3 * 8];
                TouchPage(steadyStateArray[i] as byte[]);

                if ((i % (ulong)interval) == 0)
                {
                    gcPinnedHandles[handleIndex] = GCHandle.Alloc(steadyStateArray[i], GCHandleType.Pinned);
                    handleIndex++;
                    //Console.WriteLine("array {0} is pinned", i);
                }
                else
                {
                    //Console.WriteLine();
                }
            }

            GC.Collect();
            GC.Collect();

            GetCurrentCollectionCounts(ref startCollectionCounts);
            Console.WriteLine("after init gen0: {0}, gen1: {1}, gen2: {2}, heapsize {3}",
                startCollectionCounts[0],
                startCollectionCounts[1],
                startCollectionCounts[2],
                GC.GetTotalMemory(false));
        }

        static void GetCurrentCollectionCounts(ref int[] collectionCounts)
        {
            for (int i = 0; i <= 2; i++)
            {
                collectionCounts[i] = GC.CollectionCount(i);
            }

            collectionCounts[1] -= collectionCounts[2];
            collectionCounts[0] -= collectionCounts[2];
            collectionCounts[0] -= collectionCounts[1];

            //Console.WriteLine("g0: {0}, g1: {1}, g2: {2}",
            //    collectionCounts[0],
            //    collectionCounts[1],
            //    collectionCounts[2]);
        }

        static int GetHighestGCGeneration()
        {
            GetCurrentCollectionCounts(ref endCollectionCounts);

            for (int i = 2; i >= 0; i--)
            {
                if (endCollectionCounts[i] > startCollectionCounts[i])
                    return i;
            }

            Console.WriteLine("no GCs happened?!");
            return -1;
        }

        // Note some of the gen0 GCs can be escalate to a gen2 so will take
        // a lot longer.
        static void TriggerGCTimed(int gen)
        {
            GetCurrentCollectionCounts(ref startCollectionCounts);
            stopwatch.Restart();
            //Console.WriteLine("triggering gen{0}!", gen);
            GC.Collect(gen, GCCollectionMode.Forced, true, true);
            stopwatch.Stop();
            int highestGenGC = GetHighestGCGeneration();
            triggeredCollectionCounts[highestGenGC]++;
            //if (gen > 0)
            {
                Console.WriteLine("requested gen{0}, actual gen{1} took {2}ms, {3}->{4}",
                    gen, highestGenGC, stopwatch.ElapsedMilliseconds,
                    startCollectionCounts[highestGenGC], endCollectionCounts[highestGenGC]);
            }
        }
        // We survive 1% of gen0, which means every 100th time we should collect to push 
        // the survivers into older gen.
        static void Churn()
        {
            int oldGenObjectReplaced = 0;
            int youngGenObjectCount = 10;
            int totalYoungGenAllocBeforeTrigger = (int)(totalOldGenSize / (ulong)30);
            byte[] newArray = null;
            ulong oldGenObjectIndex = 0;
            int totalYoungGenAllocated = 0;
            int totalFullGCsTriggered = 0;
            int totalYoungGenGCsTriggered = 0;
            ulong totalYoungGenObjCount = 0;
            Console.WriteLine("triggering a gen0 every {0} alloc; survive every {1} objects",
                totalYoungGenAllocBeforeTrigger, youngGenObjectCount);

            while (totalFullGCsTriggered < 3)
            {
                for (int objIndex = 0; objIndex < youngGenObjectCount; objIndex++)
                {
                    newArray = new byte[objSize - 3 * 8];
                    totalYoungGenObjCount++;
                    totalYoungGenAllocated += objSize;
                }

                if (totalYoungGenAllocated >= totalYoungGenAllocBeforeTrigger)
                {
                    //Console.WriteLine("allocated {0} ({1} objects), triggering a young gen GC, replaced {2} old gen objs", 
                    //    totalYoungGenAllocated, totalYoungGenObjCount, oldGenObjectReplaced);
                    TriggerGCTimed(0);
                    totalYoungGenAllocated = 0;
                    totalYoungGenObjCount = 0;
                    totalYoungGenGCsTriggered++;
                }

                if ((oldGenObjectIndex % (ulong)interval) == 0)
                {
                    // this is a pinned object, don't replace
                    //Console.WriteLine("don't replace array {0}, it's pinned", oldGenObjectIndex);
                    oldGenObjectIndex++;
                    if (oldGenObjectIndex == numElements)
                    {
                        oldGenObjectIndex = 0;
                    }
                }

                steadyStateArray[oldGenObjectIndex] = newArray;
                oldGenObjectIndex++;
                if (oldGenObjectIndex == numElements)
                {
                    oldGenObjectIndex = 0;
                }
                oldGenObjectReplaced++;

                if (oldGenObjectReplaced >= interval)
                {
                    Console.WriteLine("replaced {0} old gen objects, {1} gen0 GCs, triggering full GC", 
                        oldGenObjectReplaced, totalYoungGenGCsTriggered);
                    TriggerGCTimed(GC.MaxGeneration);
                    Console.WriteLine("actual gen0: {0}, gen1: {1}, gen2: {2}, heap size: {3}",
                        GC.CollectionCount(0) - gen0CountAfterInit,
                        GC.CollectionCount(1) - gen1CountAfterInit,
                        GC.CollectionCount(2) - gen2CountAfterInit,
                        GC.GetTotalMemory(false));

                    totalFullGCsTriggered++;
                    oldGenObjectReplaced = 0;
                }
            }
        }

        static void Main(string[] args)
        {
            if (args.Length == 2)
            {
                Stopwatch stopwatchTotal = new Stopwatch();
                int totalSizeMB = int.Parse(args[0]);
                totalOldGenSize = (ulong)totalSizeMB * (ulong)1024 * (ulong)1024;
                pinPercent = int.Parse(args[1]);
                Console.WriteLine("creating total {0}mb old gen size, {1}% is pinned",
                    totalSizeMB, pinPercent);

                stopwatchTotal.Restart();
                Allocate();
                stopwatchTotal.Stop();
                Console.WriteLine("init took {0}ms", stopwatchTotal.ElapsedMilliseconds);

                stopwatchTotal.Restart();
                Churn();
                stopwatchTotal.Stop();
                Console.WriteLine("churn took {0}ms", stopwatchTotal.ElapsedMilliseconds);
            }
            else
            {
                Console.WriteLine("Usage: testClearOnCompact.exe steady_state_total_size_mb pin_percent");
            }

            GetCurrentCollectionCounts(ref startCollectionCounts);

            Console.WriteLine("gen0: {0}({1}), gen1: {2}({3}), gen2: {4}({5})",
                startCollectionCounts[0], triggeredCollectionCounts[0],
                startCollectionCounts[1], triggeredCollectionCounts[1],
                startCollectionCounts[2], triggeredCollectionCounts[2]);
        }
    }
}
