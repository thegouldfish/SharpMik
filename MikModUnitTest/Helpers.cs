using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using System.Reflection;

namespace MikModUnitTest
{
	public class UnitTestHelpers
	{

		public static void FindRepeats()
		{
			Dictionary<String, int> repeatTest = new Dictionary<String, int>();

			foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (!ass.GlobalAssemblyCache)
				{
					Type[] types = ass.GetTypes();

					foreach (Type type in types)
					{
						if (repeatTest.ContainsKey(type.Name))
						{
							repeatTest[type.Name]++;
						}
						else
						{
							repeatTest.Add(type.Name, 1);
						}
					}
				}
			}

			Console.WriteLine("----");
			foreach (String key in repeatTest.Keys)
			{
				if (repeatTest[key] > 1)
				{
					Console.WriteLine(key);
				}
			}
			Console.WriteLine("----");
		}

		public static bool ReadXML<T>(String fileName, ref T obj)
		{
			FileStream xmlStream = null;

			if (File.Exists(fileName))
			{
				try
				{
					XmlSerializer xmlSer = new XmlSerializer(typeof(T));

					xmlStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.None);
					obj = (T)xmlSer.Deserialize(xmlStream);
					return true;
				}
				catch (Exception e)
				{
					Console.WriteLine(e.Message);
					return false;
				}
				finally
				{
					if (xmlStream != null)
						xmlStream.Close();
				}
			}

			return false;
		}

		public static bool WriteXML<T>(string fileName, T obj)
		{
			FileStream xmlStream = null;

			try
			{
				XmlSerializer xmlSer = new XmlSerializer(typeof(T));

				xmlStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
				xmlSer.Serialize(xmlStream, obj);
				return true;
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				return false;
			}
			finally
			{
				if (xmlStream != null)
					xmlStream.Close();
			}
		}
	}
}
