using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpMik.Attributes
{
    public class ModFileExtentionsAttribute : Attribute
    {
        public String[] FileExtentions { get; }


        public ModFileExtentionsAttribute(params String[] extentions)
        {
            FileExtentions = extentions;
        }
    }
}
