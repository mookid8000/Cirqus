using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using d60.Cirqus.Extensions;
using d60.Cirqus.Numbers;

namespace d60.Cirqus.Events
{
    public class DisplayAttribute : Attribute
    {
        public DisplayAttribute(string template)
        {
            Template = template;
        }

        public string Template { get; private set; }
    }

}