using System;
using System.Text;

namespace FastInsertsConsole;

internal class Helpers
{
    private static string _chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public static string GenerateRandomString(int fromLength, int toLength, string prefix="")
    {
        // максимально исключаем случайность 
        return prefix + Environment.CurrentManagedThreadId.ToString() + " static const value";
        var length = Random.Shared.Next(fromLength, toLength);
        var sb = new StringBuilder(length);
        for (int i = 0; i < length; i++)
            sb.Append(_chars[Random.Shared.Next(_chars.Length)]);
        return sb.ToString();
    }

    private static int _timeMsSinceMidnight;
    public static int GetTimeMsSinceMidnight()
    {   
        if (_timeMsSinceMidnight==0)
            _timeMsSinceMidnight = (int)DateTime.Now.TimeOfDay.TotalMilliseconds;
        return _timeMsSinceMidnight;

    }
}
