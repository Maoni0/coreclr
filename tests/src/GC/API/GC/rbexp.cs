// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// Tests GC.Collect(0)

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;

[StructLayout(LayoutKind.Explicit)]
class ClassWithGCRef
{
    [FieldOffset(0)] public int i;
    [FieldOffset(8)] public byte[] arr;
};

public class Test
{
    //[MethodImpl(MethodImplOptions.NoInlining)]
    //private static int LoadGCRefTestInstruction(ClassWithGCRef obj)
    //{
    //    byte[] arr = obj.arr;
    //    //Console.WriteLine("obj arr len is {0}", arr.Length);
    //    return arr.Length;
    //}

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int LoadGCRefTestInstruction(ClassWithGCRef obj0, ClassWithGCRef obj1)
    {
        obj1.arr = obj0.arr;
        //Console.WriteLine("obj1 arr len is {0}", obj1.arr.Length);
        return obj1.arr.Length;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object LoadArrayGCRefTestInstruction(object[] arr, int index)
    {
        return arr[index];
    }

    //[MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong GetSum(object[] arr, int len)
    {
        ulong sum = 0;
        for (int i = 0; i < len; i++)
        {
            byte[] b = (byte[])LoadArrayGCRefTestInstruction(arr, i);
            sum += (ulong)(b.Length);
        }

        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WriteGCRefTestInstruction(ClassWithGCRef obj)
    {
        obj.arr = new byte[211];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int LoadNonGCRefTestInstruction(ClassWithGCRef obj)
    {
        //Console.WriteLine("obj int field is {0}", obj.i);
        int i = obj.i;
        return i;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WriteNonGCRefTestInstruction(ClassWithGCRef obj)
    {
        obj.i = 5;
    }

    public static int Main(String[] args)
     {
        ulong iter = (ulong)1 * 1024 * 1024;
        int len = 10;
        if (args.Length > 0)
        {
            Console.WriteLine("iteration {0}mil times", args[0]);
            iter = (ulong)Int32.Parse(args[0]) * 1024 * 1024;
        }

        // array stuff begin
        object[] objArray = new object[len];
        for (int arrayIndex = 0; arrayIndex < len; arrayIndex++)
        {
            objArray[arrayIndex] = new byte[arrayIndex];
        }
        ulong sum = GetSum(objArray, len);
        Console.WriteLine("sum is {0}", sum);

        // array stuff end

        Stopwatch sw = new Stopwatch();

        ClassWithGCRef objWithGCRef = new ClassWithGCRef();
        objWithGCRef.arr = new byte[len];
        objWithGCRef.i = len;

        ClassWithGCRef objWithGCRef1 = new ClassWithGCRef();

        //Console.WriteLine("barr len is {0}, num is {1}", barr.Length, num);

        //WriteNonGCRefTestInstruction(objWithGCRef);

        ulong totalArrayLen = 0;

        int arrLen = LoadGCRefTestInstruction(objWithGCRef, objWithGCRef1);
        WriteGCRefTestInstruction(objWithGCRef);

        //sw.Reset();
        sw.Start();

        for (ulong iterIndex = 0; iterIndex < iter; iterIndex++)
        {
            arrLen = LoadGCRefTestInstruction(objWithGCRef, objWithGCRef1);

            //int arrLen = LoadGCRefTestInstruction(objWithGCRef);
            //int num = LoadNonGCRefTestInstruction(objWithGCRef);

            totalArrayLen += (ulong)arrLen;
        }

        sw.Stop();

        Console.WriteLine("{0}ms elapsed", sw.ElapsedMilliseconds);

        //Console.WriteLine("after write: barr len is {0}, num is {1}", barr.Length, num);

        Console.WriteLine("finished!!!! arr len {0}", totalArrayLen);
        return 0;
    }
}
