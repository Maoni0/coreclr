// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// Tests GC.Collect(0)

using System;

namespace Gen0BudgetExperiment
{
    class Program
    {
        static void Main(string[] args)
        {
            int startTicks = Environment.TickCount;
            string[] sa = new string[100 * 1000 * 1000];

            Console.WriteLine("testing");

            Random r = new Random();
            int totalCount = 0;
            for (int iter = 0; iter < 120; iter++)
            {
                int count = 0;
                for (int i = 0; i < 1000 * 1000; i++)
                {
                    int j = r.Next(sa.Length);
                    string s = new string('?', 20);
                    if (sa[j] == null)
                    {
                        sa[j] = s;
                        count++;
                    }
                    for (int k = 0; k < 20; k++)
                    {
                        s = new string('?', 25);
                    }
                }
                totalCount += count;
                Console.WriteLine("{0} objects stored in this iteration, {1} so far", count, totalCount);
            }
            int elapsedTicks = Environment.TickCount - startTicks;
            Console.WriteLine("{0} seconds", elapsedTicks * 0.001);
        }
    }
}
