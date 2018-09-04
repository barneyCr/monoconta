using System;
namespace monoconta
{
    static class Extensions
    {
        public static double ToDouble(this string s) {
            return double.Parse(s);
        }
        public static int ToInt(this string s)
        {
            return int.Parse(s);
        }
        public static string ToF3Double(this double d)
        {
            return d.ToString("F3");
        }
    }
}
