using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpMik.Drivers;

namespace MikModUnitTest
{
	public class TestDriver : VirtualSoftwareDriver
	{
		byte[] m_CWav;

		sbyte[] m_Audiobuffer;
		public static uint BUFFERSIZE = 32768;

		long m_Place = 0;
		bool m_Failed = false;

		public TestDriver()
		{
			m_Next = null;
			m_Name = "Test Driver";
			m_Version = "Test Driver";
			m_HardVoiceLimit = 0;
			m_SoftVoiceLimit = 255;
			m_AutoUpdating = false;
		}


		public bool Failed
		{
			get { return m_Failed; }
		}

		public void SetCWav(byte[] data)
		{
			m_CWav = data;
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
			m_Place = 44;
			m_Failed = false;
			return base.PlayStart();
		}

		public override void Exit()
		{

		}

		public override void Update()
		{
			uint done = WriteBytes(m_Audiobuffer, BUFFERSIZE);

			for (uint i = 0; i < done; i++)
			{
				if((byte)(m_Audiobuffer[i]) != m_CWav[m_Place])
				{
					m_Failed = true;
					return;
				}
				m_Place++;
			}
		}


	}
}
