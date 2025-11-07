using System;
using Wox.Plugin;

namespace QuickBrain.Tests;

public class TypeTest
{
    public static void TestTypes()
    {
        var result = new Result();
        Console.WriteLine($"Result type: {result.GetType()}");
        Console.WriteLine($"Available properties: {string.Join(", ", result.GetType().GetProperties().Select(p => p.Name))}");
    }
}