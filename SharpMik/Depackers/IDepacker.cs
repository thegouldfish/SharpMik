using SharpMik.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SharpMik.Depackers
{
    public interface IDepacker
    {
        bool Unpack(ModuleReader reader, out Stream read);
    }
}
