using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using SharpMik.Extentions;

namespace SharpMik.Drivers
{
    


	public class PushStreamDriver : VirtualSoftwareDriver
    {
		MemoryStream m_MemoryStream;
		sbyte[] m_Audiobuffer;

		public static uint BUFFERSIZE = 32768;


		public MemoryStream MemoryStream
		{
			get { return m_MemoryStream; }
		}

		public PushStreamDriver()
		{
			m_Next = null;
			m_Name = "Mem Writer";
			m_Version = "Mem stream writer";
			m_HardVoiceLimit = 0;
			m_SoftVoiceLimit = 255;
			m_AutoUpdating = false;
		}

		public override void CommandLine(string command)
		{
		}

		public override bool IsPresent()
		{
			return true;
		}

		public override bool Init()
		{			
			m_Audiobuffer = new sbyte[BUFFERSIZE];

			return base.Init();
		}

		public override void PlayStop()
		{
			base.PlayStop();
		}

		public override bool PlayStart()
		{			
			m_MemoryStream = new MemoryStream();
			return base.PlayStart();
		}

		public override void Exit()
		{

		}

		public override void Update()
		{
			uint done = WriteBytes(m_Audiobuffer, BUFFERSIZE);
			m_MemoryStream.Write(m_Audiobuffer, 0, (int)done);
		}
	}
}
