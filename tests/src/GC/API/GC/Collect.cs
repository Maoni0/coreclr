// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// Tests GC.Collect()

using System;
using System.Diagnostics;
using System.Reflection;

public class Test 
{
    public static int Main() 
    {
        const string name = "GetAllocatedBytesForCurrentThread";
        var typeInfo = typeof(GC).GetTypeInfo();
        var method = typeInfo.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        long nBytesBefore = 0;
        long nBytesAfter = 0;

        int countBefore = GC.CollectionCount(0);

        for (int i = 0; i < 10000; ++i)
        {
            //Console.WriteLine("before");
            nBytesBefore = (long)method.Invoke(null, null);
            GC.Collect();
            nBytesAfter = (long)method.Invoke(null, null);
            //Console.WriteLine("after");

            if ((nBytesBefore + 24) != nBytesAfter)
            {
                int countAfter = GC.CollectionCount(0);
                Console.WriteLine("b: {0}, a: {1}, iter {2}, {3}->{4}", nBytesBefore, nBytesAfter, i, countBefore, countAfter);
                Debug.Assert(false);
            }
        }

/*
        for (int i = 0; i < 1000000; ++i)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            long after = GC.GetAllocatedBytesForCurrentThread();

            Debug.Assert (before == after);

            Console.WriteLine("b: {0}, a: {1}", before, after);
        }
*/        
        return 0;
    }
}
