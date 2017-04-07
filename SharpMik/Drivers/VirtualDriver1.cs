using System;
using System.Collections.Generic;

using SharpMik.Interfaces;
using SharpMik.Player;
using System.Diagnostics;

namespace SharpMik.Drivers
{
	/*
	 * This is an implementation of the LQ Driver in mikMod
	 * Should be renamed to that and the variables tidied up.
	 * 
	 * Also not a fan of the drivers holding the samples rather then the mods.
	 * 
	 * Currently missing:
	 *	64bit mixers (not had any issues so far without them)
	 * 
	 */
	public abstract class VirtualDriver1 : IModDriver
	{
        class VirtualDriver1VoiceInfo
        {
            public byte Kick;                   // =1 -> sample has to be restarted
            public byte Active;                 // =1 -> sample is playing
            public ushort Flags;                // 16/8 bits looping/one-shot
            public short Handle;                // identifies the sample
            public uint Start;                  // start index
            public uint Size;                   // sample size
            public uint RepeatStartPosition;    // loop start
            public uint RepeatEndPosition;      // loop end
            public uint Frequency;              // current frequency
            public int Volume;                  // current volume
            public int Panning;                 // current panning position

            public int Click;
            public int LastValueLeft;
            public int LastValueRight;

            public int RampVolume;
            public int LeftVolumeFactor;
            public int RightVolumeFactor;       // Volume factor in range 0-255
            public int LeftVolumeOld;
            public int RightVolumeOld;

            public long CurrentSampleIndex;     // current index in the sample
            public long CurrentIncrement;       // increment value
        }


        short[][] m_Samples;
		VirtualDriver1VoiceInfo [] m_VoiceInfos = null;
		VirtualDriver1VoiceInfo m_CurrentVoiceInfo = null;
		long m_TickLeft = 0;
		long m_SamplesThatFit = 0;
		long m_VcMemory = 0;
		int m_VcSoftChannel;
		long m_IdxSize;
		long m_IdxlPos;
		long m_IdxlEnd;
		int[] m_VcTickBuf;
		ushort m_VcMode;

		bool m_IsStereo;

		// Reverb vars
		uint m_RvrIndex;
		int[][][] m_RvBuf = null;
		int[] m_Rvc;

		const int REVERBERATION = 110000;
		const int FRACBITS = 11;
		const int FRACMASK = ((1 << FRACBITS) - 1);
		const int TICKLSIZE = 8192;

		const int BITSHIFT = 9;
		const int CLICK_SHIFT = 6;
		const int CLICK_BUFFER = (1 << CLICK_SHIFT);


		// These are to aid in debugging
		public static bool s_TestModeOn = false;
		public static int s_TestPlace = 2;
		public static int s_TestChannel = 23;

		#region 32bit Mixers

		int Mix32MonoNormal(short[] srce, int[] dest, int index, int increment, int todo, int place)
		{
			short sample;
			int lvolsel = m_CurrentVoiceInfo.LeftVolumeFactor;
			

			while (todo-- != 0)
			{
				sample = srce[index >> FRACBITS];
				index += increment;

				dest[place++] += lvolsel * sample;
			}
			return index;
		}


		int Mix32StereoNormal(short[] srce, int[] dest, int index, int increment, int todo, int place)
		{
			unchecked
			{
				short sample;
				int lvolsel = m_CurrentVoiceInfo.LeftVolumeFactor;
				int rvolsel = m_CurrentVoiceInfo.RightVolumeFactor;
				
				while (todo-- != 0)
				{
					sample = srce[index >> FRACBITS];
					index += increment;

					dest[place++] += lvolsel * sample;
					dest[place++] += rvolsel * sample;
				}
				return index;
			}
		}


		int Mix32SurroundNormal(short[] srce, int[] dest, int index, int increment, int todo, int place)
		{
			short sample;
			int lvolsel = m_CurrentVoiceInfo.LeftVolumeFactor;
			int rvolsel = m_CurrentVoiceInfo.RightVolumeFactor;

			if (lvolsel >= rvolsel)
			{
				while (todo-- != 0)
				{
					sample = srce[index >> FRACBITS];
					index += increment;

					dest[place++] += lvolsel * sample;
					dest[place++] -= lvolsel * sample;
				}
			}
			else
			{
				while (todo-- != 0)
				{
					sample = srce[index >> FRACBITS];
					index += increment;

					dest[place++] -= rvolsel * sample;
					dest[place++] += rvolsel * sample;
				}
			}
			return index;
		}


		int Mix32MonoInterp(short[] srce, int[] dest, int index, int increment, int todo, int place)
		{
			int sample;
			int lvolsel = m_CurrentVoiceInfo.LeftVolumeFactor;
			int rampvol = m_CurrentVoiceInfo.RampVolume;

			if (rampvol != 0)
			{
				int oldlvol = m_CurrentVoiceInfo.LeftVolumeOld - lvolsel;
				while (todo-- != 0)
				{
					sample = (int)srce[index >> FRACBITS] +
						   ((int)(srce[(index >> FRACBITS) + 1] - srce[index >> FRACBITS])
							* (index & FRACMASK) >> FRACBITS);
					index += increment;

					dest[place++] += ((lvolsel << CLICK_SHIFT) + oldlvol * rampvol)
							   * sample >> CLICK_SHIFT;
					if (--rampvol == 0)
						break;
				}
				
				m_CurrentVoiceInfo.RampVolume = rampvol;

				if (todo < 0)
					return index;
			}

			while (todo-- != 0)
			{
				sample = (int)srce[index >> FRACBITS] +
					   ((int)(srce[(index >> FRACBITS) + 1] - srce[index >> FRACBITS])
						* (index & FRACMASK) >> FRACBITS);
				index += increment;

				dest[place++] += lvolsel * sample;
			}
			return index;
		}


		int Mix32StereoInterp(short[] srce, int[] dest, int index, int increment, int todo, int place)
		{
			int sample;
			int lvolsel = m_CurrentVoiceInfo.LeftVolumeFactor;
			int rvolsel = m_CurrentVoiceInfo.RightVolumeFactor;
			int rampvol = m_CurrentVoiceInfo.RampVolume;

			if (rampvol != 0)
			{
				int oldlvol = m_CurrentVoiceInfo.LeftVolumeOld - lvolsel;
				int oldrvol = m_CurrentVoiceInfo.RightVolumeOld - rvolsel;
				while (todo-- != 0)
				{
					sample = (int)srce[index >> FRACBITS] +
						   ((int)(srce[(index >> FRACBITS) + 1] - srce[index >> FRACBITS])
							* (index & FRACMASK) >> FRACBITS);
					index += increment;

					dest[place++] += ((lvolsel << CLICK_SHIFT) + oldlvol * rampvol)
							   * sample >> CLICK_SHIFT;
					dest[place++] += ((rvolsel << CLICK_SHIFT) + oldrvol * rampvol)
							   * sample >> CLICK_SHIFT;
					if (--rampvol == 0)
						break;
				}
				
				m_CurrentVoiceInfo.RampVolume = rampvol;
				
				if (todo < 0)
					return index;
			}

			while (todo-- != 0)
			{
				sample = (int)srce[index >> FRACBITS] +
					   ((int)(srce[(index >> FRACBITS) + 1] - srce[index >> FRACBITS])
						* (index & FRACMASK) >> FRACBITS);
				index += increment;

				dest[place++] += lvolsel * sample;
				dest[place++] += rvolsel * sample;
			}
			return index;
		}


		int Mix32SurroundInterp(short[] srce, int[] dest, int index, int increment, int todo, int place)
		{
			int sample;
			int lvolsel = m_CurrentVoiceInfo.LeftVolumeFactor;
			int rvolsel = m_CurrentVoiceInfo.RightVolumeFactor;
			int rampvol = m_CurrentVoiceInfo.RampVolume;
			int oldvol, vol;

			if (lvolsel >= rvolsel)
			{
				vol = lvolsel;
				oldvol = m_CurrentVoiceInfo.LeftVolumeOld;
			}
			else
			{
				vol = rvolsel;
				oldvol = m_CurrentVoiceInfo.RightVolumeOld;
			}

			if (rampvol != 0)
			{
				oldvol -= vol;
				while (todo-- != 0)
				{
					sample = (int)srce[index >> FRACBITS] +
						   ((int)(srce[(index >> FRACBITS) + 1] - srce[index >> FRACBITS])
							* (index & FRACMASK) >> FRACBITS);
					index += increment;

					sample = ((vol << CLICK_SHIFT) + oldvol * rampvol)
						   * sample >> CLICK_SHIFT;
					dest[place++] += sample;
					dest[place++] -= sample;

					if (--rampvol == 0)
						break;
				}
				m_CurrentVoiceInfo.RampVolume = rampvol;
				if (todo < 0)
					return index;
			}

			while (todo-- != 0)
			{
				sample = (int)srce[index >> FRACBITS] +
					   ((int)(srce[(index >> FRACBITS) + 1] - srce[index >> FRACBITS])
						* (index & FRACMASK) >> FRACBITS);
				index += increment;

				dest[place++] += vol * sample;
				dest[place++] -= vol * sample;
			}
			return index;
		}

		#endregion

		public override short SampleLoad(SAMPLOAD sload, int type)
		{
			SAMPLE s = sload.sample;
			
			int handle;
			uint t, length, loopstart, loopend;

			if (type == (int)SharpMikCommon.MDDecodeTypes.MD_HARDWARE)
			{
				return 0;
			}

			/* Find empty slot to put sample address in */
			for (handle = 0; handle < SharpMikCommon.MAXSAMPLEHANDLES; handle++)
			{
				if (m_Samples[handle] == null) 
					break;
			}

			if (handle == SharpMikCommon.MAXSAMPLEHANDLES)
			{
				// Throw an exception so it reaches all the way up to the loader to show the load failed.
				throw new Exception("Out of handles");
			}

			/* Reality check for loop settings */
			if (s.loopend > s.length)
			{
				s.loopend = s.length;
			}

			if (s.loopstart >= s.loopend)
			{
				int flags = s.flags;
				flags &= ~SharpMikCommon.SF_LOOP;

				s.flags = (ushort)flags;
			}

			length = s.length;
			loopstart = s.loopstart;
			loopend = s.loopend;

			SampleLoader.SL_SampleSigned(sload);
			SampleLoader.SL_Sample8to16(sload);

			uint len =((length + 20) << 1);
			m_Samples[handle] = new short[len];

			/* read sample into buffer */
			if (SampleLoader.SL_Load(m_Samples[handle], sload, length))
				return -1;

			/* Unclick sample */
			if ((s.flags & SharpMikCommon.SF_LOOP) == SharpMikCommon.SF_LOOP)
			{
				if ((s.flags & SharpMikCommon.SF_BIDI) == SharpMikCommon.SF_BIDI)
				{
					for (t = 0; t < 16; t++)
					{
						m_Samples[handle][loopend + t] = m_Samples[handle][(loopend - t) - 1];
					}
				}
				else
				{
					for (t = 0; t < 16; t++)
					{
						m_Samples[handle][loopend + t] = m_Samples[handle][t + loopstart];
					}
				}
			}
			else
			{
				for (t = 0; t < 16; t++)
				{
					m_Samples[handle][t + length] = 0;
				}
			}

			return (short)handle;
		}

		public override void SampleUnload(short handle)
		{
			if (handle < SharpMikCommon.MAXSAMPLEHANDLES)
			{
				m_Samples[handle] = null;
			}
		}

		public override uint FreeSampleSpace(int value)
		{
			return (uint)m_VcMemory;
		}

		public override uint RealSampleLength(int value, SAMPLE sample)
		{
			if (sample == null)
			{
				return 0;
			}

			return (uint)((sample.length * ((sample.flags & SharpMikCommon.SF_16BITS) == SharpMikCommon.SF_16BITS ? 2 : 1)) + 16);
		}

		public override bool Init()
		{
			m_Samples = new short[SharpMikCommon.MAXSAMPLEHANDLES][];

			m_VcTickBuf = new int[TICKLSIZE];

			m_VcMode = ModDriver.Mode;


			m_Rvc = new int[8];

			m_Rvc[0] = (5000 * ModDriver.MixFreq) / REVERBERATION;
			m_Rvc[1] = (5078 * ModDriver.MixFreq) / REVERBERATION;
			m_Rvc[2] = (5313 * ModDriver.MixFreq) / REVERBERATION;
			m_Rvc[3] = (5703 * ModDriver.MixFreq) / REVERBERATION;
			m_Rvc[4] = (6250 * ModDriver.MixFreq) / REVERBERATION;
			m_Rvc[5] = (6953 * ModDriver.MixFreq) / REVERBERATION;
			m_Rvc[6] = (7813 * ModDriver.MixFreq) / REVERBERATION;
			m_Rvc[7] = (8828 * ModDriver.MixFreq) / REVERBERATION;


			m_RvBuf = new int[2][][];
			for (int side = 0; side < 2; side++)
			{
				m_RvBuf[side] = new int[8][];
				for (int channel = 0; channel < 8; channel++)
				{
					m_RvBuf[side][channel] = new int[m_Rvc[channel] + 1];
				}
			}
			m_IsStereo = (m_VcMode & SharpMikCommon.DMODE_STEREO) == SharpMikCommon.DMODE_STEREO;

			return false;
		}

		public override void Exit()
		{
			m_VcTickBuf = null;
			m_VoiceInfos = null;
			m_Samples = null;
			m_RvBuf = null;
		}

		public override bool Reset()
		{
			return false;
		}

		public override bool SetNumVoices()
		{
			int t;

			if ((m_VcSoftChannel = ModDriver.SoftChn) == 0)
			{
				return true;
			}

			if (m_VoiceInfos != null)
				m_VoiceInfos = null;

			m_VoiceInfos = new VirtualDriver1VoiceInfo[m_VcSoftChannel];

			for (t = 0; t < m_VcSoftChannel; t++)
			{
				m_VoiceInfos[t] = new VirtualDriver1VoiceInfo();

				m_VoiceInfos[t].Frequency = 10000;
				m_VoiceInfos[t].Panning = (t & 1) == 1 ? SharpMikCommon.PAN_LEFT : SharpMikCommon.PAN_RIGHT;
			}

			return false;
		}

		public override bool PlayStart()
		{
			m_SamplesThatFit = TICKLSIZE;
			m_IsStereo = (m_VcMode & SharpMikCommon.DMODE_STEREO) == SharpMikCommon.DMODE_STEREO;

			if (m_IsStereo)
			{
				m_SamplesThatFit >>= 1;
			}

			m_TickLeft = 0;
			m_RvrIndex = 0;
			return false;
		}

		public override void PlayStop()
		{			
		}

		// This must be over ridden by the driver that implements this class
		public override void Update()
		{
			throw new NotImplementedException();
		}

		public override void Pause()
		{

		}

		public override void Resume()
		{
			
		}


		public override void VoiceSetVolume(byte voice, ushort volume)
		{
			/* protect against clicks if volume variation is too high */
			if (Math.Abs((int)m_VoiceInfos[voice].Volume - (int)volume) > 32)
			{
				m_VoiceInfos[voice].RampVolume = CLICK_BUFFER;
			}

			m_VoiceInfos[voice].Volume = volume;
		}

		public override ushort VoiceGetVolume(byte voice)
		{
			return (ushort)m_VoiceInfos[voice].Volume;
		}

		public override void VoiceSetFrequency(byte voice, uint freq)
		{
			m_VoiceInfos[voice].Frequency = freq;
		}

		public override uint VoiceGetFrequency(byte voice)
		{
			return m_VoiceInfos[voice].Frequency;
		}

		public override void VoiceSetPanning(byte voice, uint panning)
		{
			/* protect against clicks if panning variation is too high */
			if (Math.Abs((int)m_VoiceInfos[voice].Panning - (int)panning) > 48)
			{
				m_VoiceInfos[voice].RampVolume = CLICK_BUFFER;
			}
			m_VoiceInfos[voice].Panning = (int)panning;
		}

		public override uint VoiceGetPanning(byte voice)
		{
			return (uint)m_VoiceInfos[voice].Panning;
		}

		public override void VoicePlay(byte voice, short handle, uint start, uint size, uint reppos, uint repend, ushort flags)
		{
			m_VoiceInfos[voice].Flags = flags;
			m_VoiceInfos[voice].Handle = handle;
			m_VoiceInfos[voice].Start = start;
			m_VoiceInfos[voice].Size = size;
			m_VoiceInfos[voice].RepeatStartPosition = reppos;
			m_VoiceInfos[voice].RepeatEndPosition = repend;
			m_VoiceInfos[voice].Kick = 1;
		}

		public override void VoiceStop(byte voice)
		{
			m_VoiceInfos[voice].Active = 0;
		}

		public override bool VoiceStopped(byte voice)
		{
			return (m_VoiceInfos[voice].Active == 0);
		}

		public override int VoiceGetPosition(byte voice)
		{
			return (int)(m_VoiceInfos[voice].CurrentIncrement >> FRACBITS);
		}

		public override uint VoiceRealVolume(byte voice)
		{
			int i, s, size;
			int k, j;
			int t;

			t = (int)(m_VoiceInfos[voice].CurrentIncrement >> FRACBITS);
			if (m_VoiceInfos[voice].Active == 0)
			{
				return 0;
			}

			s = m_VoiceInfos[voice].Handle;
			size = (int)m_VoiceInfos[voice].Size;

			i = 64; t -= 64; k = 0; j = 0;
			if (i > size) i = size;
			if (t < 0) t = 0;
			if (t + i > size) t = size - i;

			i &= ~1;  /* make sure it's EVEN. */

			int place = t;
			for (; i != 0; i--, place++)
			{
				if (k < m_Samples[s][place])
				{
					k = m_Samples[s][place];
				}
				if (j > m_Samples[s][place])
				{
					j = m_Samples[s][place];
				}
			}
			return (uint)Math.Abs(k - j);
		}



		uint samples2bytes(uint samples)
		{
			if ((m_VcMode & SharpMikCommon.DMODE_16BITS) == SharpMikCommon.DMODE_16BITS)
			{
				samples <<= 1;
			}

			if (m_IsStereo)
			{
				samples <<= 1;
			}
			return samples;
		}


		uint bytes2samples(uint bytes)
		{
			if ((m_VcMode & SharpMikCommon.DMODE_16BITS) == SharpMikCommon.DMODE_16BITS)
			{
				bytes >>= 1;
			}

			if (m_IsStereo)
			{
				bytes >>= 1;
			}
			return bytes;
		}

		void MixReverb_Mono(int count)
		{
			uint speedup;
			int ReverbPct;
			uint[] loc = new uint[8];

			ReverbPct = 92 + (ModDriver.Reverb << 1);

			for (int i = 0; i < 8; i++)
			{
				loc[i] = (uint)(m_RvrIndex % m_Rvc[i]);
			}

			int place = 0;
			while (count-- != 0)
			{
				/* Compute the left channel echo buffers */
				speedup = (uint)(m_VcTickBuf[place] >> 3);

				int side = 0;
				speedup = (uint)(m_VcTickBuf[place + side] >> 3);
				for (int channel = 0; channel < 8; channel++)
				{
					m_RvBuf[side][channel][loc[channel]] = (int)(speedup + ((ReverbPct * m_RvBuf[side][channel][loc[channel]]) >> 7));
				}

				/* Prepare to compute actual finalized data */
				m_RvrIndex++;

				for (int i = 0; i < 8; i++)
				{
					loc[i] = (uint)(m_RvrIndex % m_Rvc[i]);
				}

				int value = m_RvBuf[side][0][loc[0]] - m_RvBuf[side][1][loc[1]];
				value += (m_RvBuf[side][2][loc[2]] - m_RvBuf[side][3][loc[3]]);
				value += (m_RvBuf[side][4][loc[4]] - m_RvBuf[side][5][loc[5]]);
				value += (m_RvBuf[side][6][loc[6]] - m_RvBuf[side][7][loc[7]]);

				m_VcTickBuf[place++] += value;
			}
		}

		void MixReverb_Stereo(int count)
		{
			uint speedup;
			int          ReverbPct;
			uint[] loc = new uint[8];

			ReverbPct = 92+(ModDriver.Reverb<<1);

			for (int i = 0; i < 8; i++)
			{
				loc[i] = (uint)(m_RvrIndex % m_Rvc[i]);
			}

			int place = 0;
			while(count-- != 0) 
			{
				/* Compute the left channel echo buffers */
				speedup = (uint)(m_VcTickBuf[place] >> 3);

				for (int side = 0; side < 2; side++)
				{
					speedup = (uint)(m_VcTickBuf[place + side] >> 3);
					for (int channel = 0; channel < 8; channel++)
					{
						m_RvBuf[side][channel][loc[channel]]=(int)(speedup+((ReverbPct*m_RvBuf[side][channel][loc[channel] ])>>7));
					}
				}
			
				/* Prepare to compute actual finalized data */
				m_RvrIndex++;


				for (int i = 0; i < 8; i++)
				{
					loc[i] = (uint)(m_RvrIndex % m_Rvc[i]);
				}


				for (int side = 0; side < 2; side++)
				{
					int value = m_RvBuf[side][0][loc[0]] - m_RvBuf[side][1][loc[1]];
					value += (m_RvBuf[side][2][loc[2]] - m_RvBuf[side][3][loc[3]]);
					value += (m_RvBuf[side][4][loc[4]] - m_RvBuf[side][5][loc[5]]);
					value += (m_RvBuf[side][6][loc[6]] - m_RvBuf[side][7][loc[7]]);

					m_VcTickBuf[place++] += value;
				}
			}
		}


		void AddChannel(int[] buff, int todo)
		{
			long end, done;

			int place = 0;

			short[] s = m_Samples[m_CurrentVoiceInfo.Handle];

			if (s == null)
			{
				m_CurrentVoiceInfo.CurrentSampleIndex = m_CurrentVoiceInfo.Active = 0;
				return;
			}
				
			while(todo>0) 
			{
				long endpos;

				if((m_CurrentVoiceInfo.Flags & SharpMikCommon.SF_REVERSE) == SharpMikCommon.SF_REVERSE)
				{
					/* The sample is playing in reverse */
					if((m_CurrentVoiceInfo.Flags&SharpMikCommon.SF_LOOP) == SharpMikCommon.SF_LOOP && (m_CurrentVoiceInfo.CurrentSampleIndex<m_IdxlPos)) 
					{
						/* the sample is looping and has reached the loopstart index */
						if((m_CurrentVoiceInfo.Flags & SharpMikCommon.SF_BIDI) == SharpMikCommon.SF_BIDI)
						{
							/* sample is doing bidirectional loops, so 'bounce' the
							   current index against the idxlpos */
							m_CurrentVoiceInfo.CurrentSampleIndex = m_IdxlPos+(m_IdxlPos-m_CurrentVoiceInfo.CurrentSampleIndex);
							int value = m_CurrentVoiceInfo.Flags;
							value &= ~SharpMikCommon.SF_REVERSE;
							m_CurrentVoiceInfo.Flags = (ushort)value;
							m_CurrentVoiceInfo.CurrentIncrement = -m_CurrentVoiceInfo.CurrentIncrement;
						} 
						else
						{
							/* normal backwards looping, so set the current position to
							   loopend index */
							m_CurrentVoiceInfo.CurrentSampleIndex=m_IdxlEnd-(m_IdxlPos-m_CurrentVoiceInfo.CurrentSampleIndex);
						}
					} 
					else 
					{
						/* the sample is not looping, so check if it reached index 0 */
						if(m_CurrentVoiceInfo.CurrentSampleIndex < 0) 
						{
							/* playing index reached 0, so stop playing this sample */
							m_CurrentVoiceInfo.CurrentSampleIndex = m_CurrentVoiceInfo.Active  = 0;
							break;
						}
					}
				} else {
					/* The sample is playing forward */
					if((m_CurrentVoiceInfo.Flags & SharpMikCommon.SF_LOOP) == SharpMikCommon.SF_LOOP &&   (m_CurrentVoiceInfo.CurrentSampleIndex >= m_IdxlEnd)) 
					{
						/* the sample is looping, check the loopend index */
						if((m_CurrentVoiceInfo.Flags & SharpMikCommon.SF_BIDI) == SharpMikCommon.SF_BIDI)
						{
							/* sample is doing bidirectional loops, so 'bounce' the
							   current index against the idxlend */
							m_CurrentVoiceInfo.Flags |= SharpMikCommon.SF_REVERSE;
							m_CurrentVoiceInfo.CurrentIncrement = -m_CurrentVoiceInfo.CurrentIncrement;
							m_CurrentVoiceInfo.CurrentSampleIndex = m_IdxlEnd-(m_CurrentVoiceInfo.CurrentSampleIndex-m_IdxlEnd);
						} 
						else
						{
							/* normal backwards looping, so set the current position
							   to loopend index */
							m_CurrentVoiceInfo.CurrentSampleIndex=m_IdxlPos+(m_CurrentVoiceInfo.CurrentSampleIndex-m_IdxlEnd);
						}
					} 
					else 
					{
						/* sample is not looping, so check if it reached the last
						   position */
						if(m_CurrentVoiceInfo.CurrentSampleIndex >= m_IdxSize) 
						{
							/* yes, so stop playing this sample */
							m_CurrentVoiceInfo.CurrentSampleIndex = m_CurrentVoiceInfo.Active  = 0;
							break;
						}
					}
				}

				end=(m_CurrentVoiceInfo.Flags&SharpMikCommon.SF_REVERSE) == SharpMikCommon.SF_REVERSE ?(m_CurrentVoiceInfo.Flags&SharpMikCommon.SF_LOOP) == SharpMikCommon.SF_LOOP?m_IdxlPos:0:(m_CurrentVoiceInfo.Flags&SharpMikCommon.SF_LOOP) == SharpMikCommon.SF_LOOP?m_IdxlEnd:m_IdxSize;

				/* if the sample is not blocked... */
				if((end==m_CurrentVoiceInfo.CurrentSampleIndex)||(m_CurrentVoiceInfo.CurrentIncrement == 0))
				{
					done=0;
				}
				else 
				{
					done=Math.Min((end-m_CurrentVoiceInfo.CurrentSampleIndex)/m_CurrentVoiceInfo.CurrentIncrement+1,todo);
					if(done<0) 
					{
						done=0;
					}
				}

				if(done == 0) 
				{
					m_CurrentVoiceInfo.Active = 0;
					break;
				}

				endpos=m_CurrentVoiceInfo.CurrentSampleIndex+done*m_CurrentVoiceInfo.CurrentIncrement;

				if (m_CurrentVoiceInfo.Volume != 0)
				{
					/* use the 32 bit mixers as often as we can (they're much faster) */
					if ((m_CurrentVoiceInfo.CurrentSampleIndex < 0x7fffffff) && (endpos < 0x7fffffff))
					{
						if ((ModDriver.Mode & SharpMikCommon.DMODE_INTERP) == SharpMikCommon.DMODE_INTERP)
						{
							if (m_IsStereo)
							{
								if ((m_CurrentVoiceInfo.Panning == SharpMikCommon.PAN_SURROUND) && (ModDriver.Mode & SharpMikCommon.DMODE_SURROUND) == SharpMikCommon.DMODE_SURROUND)
								{
									m_CurrentVoiceInfo.CurrentSampleIndex = (long)Mix32SurroundInterp(s, buff, (int)m_CurrentVoiceInfo.CurrentSampleIndex, (int)m_CurrentVoiceInfo.CurrentIncrement, (int)done, place);
								}
								else
								{
									m_CurrentVoiceInfo.CurrentSampleIndex = (long)Mix32StereoInterp(s, buff, (int)m_CurrentVoiceInfo.CurrentSampleIndex, (int)m_CurrentVoiceInfo.CurrentIncrement, (int)done, place);
								}
							}
							else
							{
								m_CurrentVoiceInfo.CurrentSampleIndex = (long)Mix32MonoInterp(s, buff, (int)m_CurrentVoiceInfo.CurrentSampleIndex, (int)m_CurrentVoiceInfo.CurrentIncrement, (int)done, place);
							}
						}
						else if (m_IsStereo)
						{
							if ((m_CurrentVoiceInfo.Panning == SharpMikCommon.PAN_SURROUND) && (ModDriver.Mode & SharpMikCommon.DMODE_SURROUND) == SharpMikCommon.DMODE_SURROUND)
							{
								m_CurrentVoiceInfo.CurrentSampleIndex = Mix32SurroundNormal(s, buff, (int)m_CurrentVoiceInfo.CurrentSampleIndex, (int)m_CurrentVoiceInfo.CurrentIncrement, (int)done, place);
							}
							else
							{
								m_CurrentVoiceInfo.CurrentSampleIndex = Mix32StereoNormal(s, buff, (int)m_CurrentVoiceInfo.CurrentSampleIndex, (int)m_CurrentVoiceInfo.CurrentIncrement, (int)done, place);
							}
						}
						else
						{
							m_CurrentVoiceInfo.CurrentSampleIndex = Mix32MonoNormal(s, buff, (int)m_CurrentVoiceInfo.CurrentSampleIndex, (int)m_CurrentVoiceInfo.CurrentIncrement, (int)done, place);
						}
					}
					else
					{
						// do I need to implement the 64bit functions? I hope not!
						throw new NotImplementedException();
						/*
						if((md_mode & DMODE_INTERP)) {
							if(vc_mode & DMODE_STEREO) {
								if((vnf->pan==PAN_SURROUND)&&(md_mode&DMODE_SURROUND))
									vnf->current=MixSurroundInterp
											   (s,ptr,vnf->current,vnf->increment,done);
								else
									vnf->current=MixStereoInterp
											   (s,ptr,vnf->current,vnf->increment,done);
							} else
								vnf->current=MixMonoInterp
											   (s,ptr,vnf->current,vnf->increment,done);
						} else if(vc_mode & DMODE_STEREO) {
							if((vnf->pan==PAN_SURROUND)&&(md_mode&DMODE_SURROUND))
								vnf->current=MixSurroundNormal
											   (s,ptr,vnf->current,vnf->increment,done);
							else
								vnf->current=MixStereoNormal
											   (s,ptr,vnf->current,vnf->increment,done);
						} else
							vnf->current=MixMonoNormal
											   (s,ptr,vnf->current,vnf->increment,done);
						 */
					}
				}
				else
				{
					/* update sample position */
					m_CurrentVoiceInfo.CurrentSampleIndex = endpos;
				}
				
				todo-=(int)done;
				place += (int)(m_IsStereo ? (done << 1) : done);
			}

		}


		uint VC_WriteSamples(sbyte[] buf, uint todo)
		{
			int left, portion = 0, count;			
			int t, pan, vol;
			
			uint bufferPlace = 0;
			uint bufPlace = 0;

			uint total = todo;

			if (todo > buf.Length)
			{
				throw new Exception("Asked for more then the dest buffer.");
			}

			while (todo != 0)
			{
				if (m_TickLeft == 0)
				{
					if ((m_VcMode & SharpMikCommon.DMODE_SOFT_MUSIC) == SharpMikCommon.DMODE_SOFT_MUSIC)
					{
						ModPlayer.Player_HandleTick();
					}

					m_TickLeft = (ModDriver.MixFreq * 125) / (ModDriver.Bpm * 50);
				}

				left = (int)Math.Min(m_TickLeft, todo);

				bufferPlace = bufPlace;
				m_TickLeft -= left;
				todo -= (uint)left;
				bufPlace += samples2bytes((uint)left);

				while (left != 0)
				{
					portion = (int)Math.Min(left, m_SamplesThatFit);
					count = m_IsStereo ? (portion << 1) : portion;

					Array.Clear(m_VcTickBuf, 0, TICKLSIZE);
						
					for (t = 0; t < m_VcSoftChannel; t++)
					{
						m_CurrentVoiceInfo = m_VoiceInfos[t];

						if (m_CurrentVoiceInfo.Kick != 0)
						{
							m_CurrentVoiceInfo.CurrentSampleIndex = ((long)m_CurrentVoiceInfo.Start) << FRACBITS;
							m_CurrentVoiceInfo.Kick = 0;
							m_CurrentVoiceInfo.Active = 1;
						}

						if (m_CurrentVoiceInfo.Frequency == 0)
						{
							m_CurrentVoiceInfo.Active = 0;
						}

						if (m_CurrentVoiceInfo.Active != 0)
						{
							m_CurrentVoiceInfo.CurrentIncrement = ((long)(m_CurrentVoiceInfo.Frequency << FRACBITS)) / ModDriver.MixFreq;

							if ((m_CurrentVoiceInfo.Flags & SharpMikCommon.SF_REVERSE) != 0)
							{
								m_CurrentVoiceInfo.CurrentIncrement = -m_CurrentVoiceInfo.CurrentIncrement;
							}

							vol = m_CurrentVoiceInfo.Volume;
							pan = m_CurrentVoiceInfo.Panning;

							m_CurrentVoiceInfo.LeftVolumeOld = m_CurrentVoiceInfo.LeftVolumeFactor; 
							m_CurrentVoiceInfo.RightVolumeOld = m_CurrentVoiceInfo.RightVolumeFactor;

							if (m_IsStereo)
							{
								if (pan != SharpMikCommon.PAN_SURROUND)
								{
									m_CurrentVoiceInfo.LeftVolumeFactor = (vol * (SharpMikCommon.PAN_RIGHT - pan)) >> 8;
									m_CurrentVoiceInfo.RightVolumeFactor = (vol * pan) >> 8;
								}
								else
								{
									m_CurrentVoiceInfo.LeftVolumeFactor = m_CurrentVoiceInfo.RightVolumeFactor = vol / 2;
								}
							}
							else
							{
								m_CurrentVoiceInfo.LeftVolumeFactor = vol;
							}

							m_IdxSize = (m_CurrentVoiceInfo.Size != 0) ? ((long)m_CurrentVoiceInfo.Size << FRACBITS) - 1 : 0;
							m_IdxlEnd = (m_CurrentVoiceInfo.RepeatEndPosition != 0) ? ((long)m_CurrentVoiceInfo.RepeatEndPosition << FRACBITS) - 1 : 0;

							m_IdxlPos = (long)m_CurrentVoiceInfo.RepeatStartPosition << FRACBITS;

							if (s_TestModeOn && t == s_TestChannel)
							{
								Console.Write("here");
							}
							
							AddChannel(m_VcTickBuf, portion);

							if (s_TestModeOn)
							{
								Debug.WriteLine("{0}\t{1}",t,m_VcTickBuf[s_TestPlace]);
							}
						}
					}

					if (ModDriver.Reverb != 0)
					{
						if (m_IsStereo)
						{
							MixReverb_Stereo(portion);
						}
						else
						{
							MixReverb_Mono(portion);
						}
					}

					if ((m_VcMode & SharpMikCommon.DMODE_16BITS) == SharpMikCommon.DMODE_16BITS)
					{
						Mix32To16(buf, m_VcTickBuf, count, (int)bufferPlace);
					}
					else
					{
						Mix32To8(buf, m_VcTickBuf, count, (int)bufferPlace);
					}

					bufferPlace += samples2bytes((uint)portion);

					if (bufferPlace > buf.Length)
					{
						return todo;
					}
					else
					{
						left -= portion;
					}					
				}
			}

			return todo;
		}


		int ExtractSample(int[] srce, int size, ref int place)
		{
			int var;
			var = srce[place++] >> (BITSHIFT + 16 - size);

			return var;
		}

		void CheckSample(ref int var, int bound)
		{
			var = (var >= bound) ? bound - 1 : (var < -bound) ? -bound : var;
		}

		void PutShortSample(sbyte[] deste, ref int destePlace, int var)
		{
			deste[destePlace++] = (sbyte)var;
			deste[destePlace++] = (sbyte)(var >> 8);
		}

		void PutSample(sbyte[] deste, ref int destePlace, int var)
		{
			deste[destePlace++] = (sbyte)var;
		}

		void Mix32To16(sbyte[] dste, int[] srce, int count, int dstePlace)
		{
			unchecked
			{
				int x1;
				int srcePlace = 0;

				while (count-- != 0)
				{
					x1 = srce[srcePlace++] >> (BITSHIFT);
					x1 = (x1 > Int16.MaxValue) ? Int16.MaxValue : (x1 < Int16.MinValue) ? Int16.MinValue : x1;

					if (BitConverter.IsLittleEndian)
					{
						dste[dstePlace++] = (sbyte)x1;
						dste[dstePlace++] = (sbyte)(x1 >> 8);
					}
					else
					{
						dste[dstePlace++] = (sbyte)((x1 >> 8) & 0xFF);
						dste[dstePlace++] = (sbyte)(x1 & 0xFF);
					}
				}
			}
		}


		void Mix32To8(sbyte[] dste, int[] srce, int count, int dstePlace)
		{
			int x1, x2, x3, x4;
			int remain;

			int srcePlace = 0;

			remain = count & 3;
			for (count >>= 2; count != 0; count--)
			{
				x1 = ExtractSample(srce, 8, ref srcePlace);
				x2 = ExtractSample(srce, 8, ref srcePlace);
				x3 = ExtractSample(srce, 8, ref srcePlace);
				x4 = ExtractSample(srce, 8, ref srcePlace);

				CheckSample(ref x1, 128);
				CheckSample(ref x2, 128);
				CheckSample(ref x3, 128);
				CheckSample(ref x4, 128);

				PutSample(dste, ref dstePlace, x1+128);
				PutSample(dste, ref dstePlace, x2+128);
				PutSample(dste, ref dstePlace, x3+128);
				PutSample(dste, ref dstePlace, x4+128);
			}

			while (remain-- != 0)
			{
				x1 = ExtractSample(srce, 8, ref srcePlace);
				CheckSample(ref x1, 128);
				PutSample(dste, ref dstePlace, x1+128);
			}
		}


		uint VC_SilenceBytes(sbyte[] buf, uint todo)
		{
			todo = samples2bytes(bytes2samples(todo));

			sbyte toSet = 0;
			/* clear the buffer to zero (16 bits signed) or 0x80 (8 bits unsigned) */
			if ((m_VcMode & SharpMikCommon.DMODE_16BITS) == SharpMikCommon.DMODE_16BITS)
			{
				toSet = 0;
			}
			else
			{
				int value = 0x80;
				toSet = (sbyte)value;
			}

			for (int i = 0; i < todo; i++)
			{
				buf[i] = toSet;
			}

			return todo;
		}

		public virtual uint VC_WriteBytes(sbyte[] buf, uint todo)
		{
			m_IsStereo = (m_VcMode & SharpMikCommon.DMODE_STEREO) == SharpMikCommon.DMODE_STEREO;

			if (m_VcSoftChannel == 0)
			{
				return VC_SilenceBytes(buf, todo);
			}

			todo = bytes2samples(todo);
			VC_WriteSamples(buf, todo);
			todo = samples2bytes(todo);

			if (m_DspProcessor != null)
			{
				m_DspProcessor.PushData(buf, todo);
			}
			
			return todo;
		}

		public override short[] GetSample(short handle)
		{
			if (handle < m_Samples.Length)
			{
				return m_Samples[handle];
			}

			return null;
		}

		public override short SetSample(short[] sample)
		{
			short handle;
			/* Find empty slot to put sample address in */
			for (handle = 0; handle < SharpMikCommon.MAXSAMPLEHANDLES; handle++)
			{
				if (m_Samples[handle] == null)
					break;
			}

			if (handle == SharpMikCommon.MAXSAMPLEHANDLES)
			{
				throw new Exception("Out of handles");
			}

			m_Samples[handle] = sample;

			return handle;
		}
	}
}
