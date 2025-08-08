using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DapperExt
{
    public static class StrExtensions
    {
        public static string ToUnderscoreCase(this string str, bool isLower = false)
        {
            if (isLower)
            {
                return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
            }
            else
            {
                return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToUpper();
            }
        }
    }
}
