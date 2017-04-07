using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MikModUnitTest
{
	public class UnitTestOptions
	{
		public String MikModCExe { get; set; }
		public bool CopyBrokenMods { get; set; }
		public String TestModFolder { get; set; }
		public String TestDirectory { get; set; }
	}
}
