using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.NativeConsole;

class Program
{
    private static readonly LovableNativeInterface _interface = new();

    static async Task Main(string[] args)
    {
        Console.Clear();
        Console.CursorVisible = false;
        
        try
        {
            await _interface.RunAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
        finally
        {
            Console.CursorVisible = true;
        }
    }
}