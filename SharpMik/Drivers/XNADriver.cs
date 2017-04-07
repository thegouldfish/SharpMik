using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework.Audio;

using SharpMik.Interfaces;
using SharpMik.Drivers;
using SharpMik.Player;
using SharpMik;

namespace SharpMikXNA.Drivers
{
	public class XNADriver : VirtualDriver1
	{
		DynamicSoundEffectInstance m_SoundInstance;
		bool m_Playing = false;

		sbyte[] m_SignedAudiobuffer;
		byte[] m_Audiobuffer;
		public static int BUFFERSIZE = 8192;
		public static int BUFFERCOUNT = 3;

		public XNADriver()
		{
			m_Next = null;
			m_Name = "XNA Dynamic Sound Effect Driver";
			m_Version = "XNA Driver v1.0";
			m_HardVoiceLimit = 0;
			m_SoftVoiceLimit = 255;
			m_AutoUpdating = true;
			m_Playing = false;
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
			m_SignedAudiobuffer = new sbyte[BUFFERSIZE];
			m_Audiobuffer = new byte[BUFFERSIZE];

			m_SoundInstance = new DynamicSoundEffectInstance(ModDriver.MixFreq, (ModDriver.Mode & SharpMikCommon.DMODE_STEREO) == SharpMikCommon.DMODE_STEREO ? AudioChannels.Stereo: AudioChannels.Mono);
			m_SoundInstance.BufferNeeded += new EventHandler<EventArgs>(m_SoundInstance_BufferNeeded);

			return base.Init();
		}

		void m_SoundInstance_BufferNeeded(object sender, EventArgs e)
		{
			if (m_Playing)
			{
				// The Dynamic Sound instance seems to wait till its ran out of data before asking for more
				// which results in clicks in the sound between each request for audio if the buffer count is 1
				for (int i = 0; i < BUFFERCOUNT; i++)
				{
					uint done = VC_WriteBytes(m_SignedAudiobuffer, (uint)BUFFERSIZE);
					Buffer.BlockCopy(m_SignedAudiobuffer, 0, m_Audiobuffer, 0, BUFFERSIZE);
					m_SoundInstance.SubmitBuffer(m_Audiobuffer);
				}
			}
		}


		public override bool PlayStart()
		{
			m_Playing = true;
			m_SoundInstance.Play();
			return base.PlayStart();
		}

		public override void PlayStop()
		{
			m_Playing = false;
			m_SoundInstance.Stop();
			base.PlayStop();
		}


		public override void Pause()
		{
			m_SoundInstance.Pause();			
		}

		public override void Resume()
		{
			m_SoundInstance.Resume();
		}
	}
}
