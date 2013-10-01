using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Patcher
{
    static class ReleasePackagePathRegex
    {
        const string Stuff = @".*";
        const string Number = @"\d{0,3}";
        const string Dot = @"\.";
        const string DirectorySeparator = @"[/\\]";
        
        const string Format = "/(d.d.d.d)/*$";

        static readonly string RegexString = Format
            .Replace(".", Dot)
            .Replace("*", Stuff)
            .Replace("d", Number)
            .Replace("/", DirectorySeparator);

        public static Regex Regex = new Regex(RegexString, RegexOptions.ECMAScript | RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    }
}
