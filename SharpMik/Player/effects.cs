using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpMik.Player
{
	public partial class ModEffects : ModPlayer
	{
		delegate int effectDelegate(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel);

		static effectDelegate effect_func = DoNothing;

		#region effects

		/*========== Protracker effects */

		static void DoArpeggio(ushort tick, ushort flags, MP_CONTROL a, byte style)
		{
			byte note = a.main.note;

			if (a.arpmem != 0)
			{
				switch (style)
				{
					case 0:		/* mod style: N, N+x, N+y */
					switch (tick % 3)
					{
						/* case 0: unchanged */
						case 1:
						note += (byte)(a.arpmem >> 4);
						break;
						case 2:
						note += (byte)(a.arpmem & 0xf);
						break;
					}
					break;
					case 3:		/* okt arpeggio 3: N-x, N, N+y */
					switch (tick % 3)
					{
						case 0:
						note -= (byte)(a.arpmem >> 4);
						break;
						/* case 1: unchanged */
						case 2:
						note += (byte)(a.arpmem & 0xf);
						break;
					}
					break;
					case 4:		/* okt arpeggio 4: N, N+y, N, N-x */
					switch (tick % 4)
					{
						/* case 0, case 2: unchanged */
						case 1:
						note += (byte)(a.arpmem & 0xf);
						break;
						case 3:
						note -= (byte)(a.arpmem >> 4);
						break;
					}
					break;
					case 5:		/* okt arpeggio 5: N-x, N+y, N, and nothing at tick 0 */
					if (tick == 0)
						break;
					switch (tick % 3)
					{
						/* case 0: unchanged */
						case 1:
						note -= (byte)(a.arpmem >> 4);
						break;
						case 2:
						note += (byte)(a.arpmem & 0xf);
						break;
					}
					break;
				}
				a.main.period = (ushort)GetPeriod(flags, (ushort)(note << 1), a.speed);
				a.ownper = 1;
			}
		}

		static int DoPTEffect0(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte dat;

			dat = s_UniTrack.UniGetByte();
			if (tick == 0)
			{
				if (dat == 0 && (flags & SharpMikCommon.UF_ARPMEM) == SharpMikCommon.UF_ARPMEM)
					dat = a.arpmem;
				else
					a.arpmem = dat;
			}
			if (a.main.period != 0)
				DoArpeggio(tick, flags, a, 0);

			return 0;
		}

		static int DoPTEffect1(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte dat;

			dat = s_UniTrack.UniGetByte();
			if (tick == 0 && dat != 0)
				a.slidespeed = (ushort)(dat << 2);
			if (a.main.period != 0)
				if (tick != 0)
					a.tmpperiod -= a.slidespeed;

			return 0;
		}

		static int DoPTEffect2(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte dat;

			dat = s_UniTrack.UniGetByte();
			if (tick == 0 && dat != 0)
				a.slidespeed = (ushort)(dat << 2);
			if (a.main.period != 0)
				if (tick != 0)
					a.tmpperiod += a.slidespeed;

			return 0;
		}

		static void DoToneSlide(ushort tick, MP_CONTROL a)
		{
			if (a.main.fadevol == 0)
				a.main.kick = (byte)((a.main.kick == SharpMikCommon.KICK_NOTE) ? SharpMikCommon.KICK_NOTE : SharpMikCommon.KICK_KEYOFF);
			else
				a.main.kick = (byte)((a.main.kick == SharpMikCommon.KICK_NOTE) ? SharpMikCommon.KICK_ENV : SharpMikCommon.KICK_ABSENT);

			if (tick != 0)
			{
				int dist;

				/* We have to slide a.main.period towards a.wantedperiod, so compute
				   the difference between those two values */
				dist = a.main.period - a.wantedperiod;

				/* if they are equal or if portamentospeed is too big ...*/
				if (dist == 0 || a.portspeed > Math.Abs(dist))
					/* ...make tmpperiod equal tperiod */
					a.tmpperiod = a.main.period = a.wantedperiod;
				else if (dist > 0)
				{
					a.tmpperiod -= a.portspeed;
					a.main.period -= a.portspeed; /* dist>0, slide up */
				}
				else
				{
					a.tmpperiod += a.portspeed;
					a.main.period += a.portspeed; /* dist<0, slide down */
				}
			}
			else
				a.tmpperiod = a.main.period;
			a.ownper = 1;
		}

		static int DoPTEffect3(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte dat;

			dat = s_UniTrack.UniGetByte();
			if ((tick == 0) && (dat != 0))
				a.portspeed = (ushort)(dat << 2);
			if (a.main.period != 0)
				DoToneSlide(tick, a);

			return 0;
		}

		static void DoVibrato(ushort tick, MP_CONTROL a)
		{
			byte q;
			ushort temp = 0;	/* silence warning */

			if (tick == 0)
				return;

			q = (byte)((a.vibpos >> 2) & 0x1f);

			switch (a.wavecontrol & 3)
			{
				case 0: /* sine */
				temp = VibratoTable[q];
				break;
				case 1: /* ramp down */
				q <<= 3;
				if (a.vibpos < 0) q = (byte)(255 - q);
				temp = q;
				break;
				case 2: /* square wave */
				temp = 255;
				break;
				case 3: /* random wave */
				temp = (ushort)getrandom(256);
				break;
			}

			temp *= a.vibdepth;
			temp >>= 7; temp <<= 2;

			if (a.vibpos >= 0)
				a.main.period = (ushort)(a.tmpperiod + temp);
			else
				a.main.period = (ushort)(a.tmpperiod - temp);
			a.ownper = 1;

			if (tick != 0)
				a.vibpos += (sbyte)a.vibspd;
		}

		static int DoPTEffect4(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte dat;

			dat = s_UniTrack.UniGetByte();
			if (tick == 0)
			{
				if ((dat & 0x0f) != 0) a.vibdepth = (byte)(dat & 0xf);
				if ((dat & 0xf0) != 0) a.vibspd = (byte)((dat & 0xf0) >> 2);
			}
			if (a.main.period != 0)
				DoVibrato(tick, a);

			return 0;
		}

		static void DoVolSlide(MP_CONTROL a, byte dat)
		{
			if ((dat & 0xf) != 0)
			{
				a.tmpvolume -= (short)(dat & 0x0f);
				if (a.tmpvolume < 0)
					a.tmpvolume = 0;
			}
			else
			{
				a.tmpvolume += (short)(dat >> 4);
				if (a.tmpvolume > 64)
					a.tmpvolume = 64;
			}
		}

		static int DoPTEffect5(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte dat;

			dat = s_UniTrack.UniGetByte();
			if (a.main.period != 0)
				DoToneSlide(tick, a);

			if (tick != 0)
				DoVolSlide(a, dat);

			return 0;
		}

		/* DoPTEffect6 after DoPTEffectA */

		static int DoPTEffect7(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte dat;
			byte q;
			ushort temp = 0;	/* silence warning */

			dat = s_UniTrack.UniGetByte();
			if (tick == 0)
			{
				if ((dat & 0x0f) != 0) a.trmdepth = (byte)(dat & 0xf);
				if ((dat & 0xf0) != 0) a.trmspd = (byte)((dat & 0xf0) >> 2);
			}
			if (a.main.period != 0)
			{
				q = (byte)((a.trmpos >> 2) & 0x1f);

				switch ((a.wavecontrol >> 4) & 3)
				{
					case 0: /* sine */
					temp = VibratoTable[q];
					break;
					case 1: /* ramp down */
					q <<= 3;
					if (a.trmpos < 0) q = (byte)(255 - q);
					temp = q;
					break;
					case 2: /* square wave */
					temp = 255;
					break;
					case 3: /* random wave */
					temp = (ushort)getrandom(256);
					break;
				}
				temp *= a.trmdepth;
				temp >>= 6;

				if (a.trmpos >= 0)
				{
					a.volume = (short)(a.tmpvolume + temp);
					if (a.volume > 64)
						a.volume = 64;
				}
				else
				{
					a.volume = (short)(a.tmpvolume - temp);
					if (a.volume < 0)
						a.volume = 0;
				}
				a.ownvol = 1;

				if (tick != 0)
					a.trmpos += (sbyte)a.trmspd;
			}

			return 0;
		}

		static int DoPTEffect8(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte dat;

			dat = s_UniTrack.UniGetByte();
			if (mod.panflag)
			{
				a.main.panning = dat;
				mod.panning[channel] = dat;
			}

			return 0;
		}

		static int DoPTEffect9(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte dat;

			dat = s_UniTrack.UniGetByte();
			if (tick == 0)
			{
				if (dat != 0) a.soffset = (ushort)(dat << 8);
				a.main.start = a.hioffset | a.soffset;

				if ((a.main.s != null) && (a.main.start > a.main.s.length))
				{
					int result = a.main.s.flags & (SharpMikCommon.SF_LOOP | SharpMikCommon.SF_BIDI);
					//int test = (Common.SF_LOOP|Common.SF_BIDI);
					if (result != 0)
					{
						a.main.start = a.main.s.loopstart;
					}
					else
					{
						a.main.start = a.main.s.length;
					}
				}
			}

			return 0;
		}

		static int DoPTEffectA(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte dat;

			dat = s_UniTrack.UniGetByte();
			if (tick != 0)
				DoVolSlide(a, dat);

			return 0;
		}

		static int DoPTEffect6(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			if (a.main.period != 0)
				DoVibrato(tick, a);
			DoPTEffectA(tick, flags, a, mod, channel);

			return 0;
		}

		static int DoPTEffectB(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte dat;

			dat = s_UniTrack.UniGetByte();

			if (tick != 0 || mod.patdly2 != 0)
				return 0;

			/* Vincent Voois uses a nasty trick in "Universal Bolero" */
			if (dat == mod.sngpos && mod.patbrk == mod.patpos)
				return 0;

			if (!mod.loop && mod.patbrk == 0 &&
				(dat < mod.sngpos ||
				 (mod.sngpos == (mod.numpos - 1) && mod.patbrk == 0) ||
				 (dat == mod.sngpos && (flags & SharpMikCommon.UF_NOWRAP) == SharpMikCommon.UF_NOWRAP)
				))
			{
				/* if we don't loop, better not to skip the end of the
				   pattern, after all... so:
				mod.patbrk=0; */
				mod.posjmp = 3;
			}
			else
			{
				/* if we were fading, adjust... */
				if (mod.sngpos == (mod.numpos - 1))
					mod.volume = (short)(mod.initvolume > 128 ? 128 : mod.initvolume);
				mod.sngpos = dat;
				mod.posjmp = 2;
				mod.patpos = 0;
			}

			return 0;
		}

		static int DoPTEffectC(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte dat;

			dat = s_UniTrack.UniGetByte();
			if (tick != 0)
				return 0;

			if (dat == byte.MaxValue)
				a.anote = dat = 0; /* note cut */
			else
				if (dat > 64)
					dat = 64;

			a.tmpvolume = dat;

			return 0;
		}

		static int DoPTEffectD(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte dat;

			dat = s_UniTrack.UniGetByte();
			if ((tick != 0) || (mod.patdly2 != 0)) return 0;
			if ((mod.positions[mod.sngpos] != SharpMikCommon.LAST_PATTERN) && (dat > mod.pattrows[mod.positions[mod.sngpos]]))
				dat = (byte)mod.pattrows[mod.positions[mod.sngpos]];
			mod.patbrk = dat;
			if (mod.posjmp == 0)
			{
				/* don't ask me to explain this code - it makes
				   backwards.s3m and children.xm (heretic's version) play
				   correctly, among others. Take that for granted, or write
				   the page of comments yourself... you might need some
				   aspirin - Miod */
				if ((mod.sngpos == mod.numpos - 1) && (dat != 0) && ((mod.loop) ||
							   (mod.positions[mod.sngpos] == (mod.numpat - 1)
								&& !((flags & SharpMikCommon.UF_NOWRAP) == SharpMikCommon.UF_NOWRAP))))
				{
					mod.sngpos = 0;
					mod.posjmp = 2;
				}
				else
					mod.posjmp = 3;
			}

			return 0;
		}

		static void DoEEffects(ushort tick, ushort flags, MP_CONTROL a, MODULE mod,
			short channel, byte dat)
		{
			byte nib = (byte)(dat & 0xf);

			switch (dat >> 4)
			{
				case 0x0: /* hardware filter toggle, not supported */
				break;
				case 0x1: /* fineslide up */
				if (a.main.period != 0)
					if (tick == 0)
						a.tmpperiod -= (byte)(nib << 2);
				break;
				case 0x2: /* fineslide dn */
				if (a.main.period != 0)
					if (tick == 0)
						a.tmpperiod += (byte)(nib << 2);
				break;
				case 0x3: /* glissando ctrl */
				a.glissando = nib;
				break;
				case 0x4: /* set vibrato waveform */
				a.wavecontrol &= 0xf0;
				a.wavecontrol |= nib;
				break;
				case 0x5: /* set finetune */
				if (a.main.period != 0)
				{
					if ((flags & SharpMikCommon.UF_XMPERIODS) == SharpMikCommon.UF_XMPERIODS)
						a.speed = (byte)(nib + 128);
					else
						a.speed = SharpMikCommon.finetune[nib];
					a.tmpperiod = GetPeriod(flags, (ushort)(a.main.note << 1), a.speed);
				}
				break;
				case 0x6: /* set patternloop */
				if (tick != 0)
					break;
				if (nib != 0)
				{ /* set reppos or repcnt ? */
					/* set repcnt, so check if repcnt already is set, which means we
					   are already looping */
					if (a.pat_repcnt != 0)
						a.pat_repcnt--; /* already looping, decrease counter */
					else
					{
#if BLAH
				/* this would make walker.xm, shipped with Xsoundtracker,
				   play correctly, but it's better to remain compatible
				   with FT2 */
				if ((!(flags&UF_NOWRAP))||(a.pat_reppos!=SharpMikCommon.POS_NONE))
#endif
						a.pat_repcnt = nib; /* not yet looping, so set repcnt */
					}

					if (a.pat_repcnt != 0)
					{ /* jump to reppos if repcnt>0 */
						if (a.pat_reppos == SharpMikCommon.POS_NONE)
							a.pat_reppos = (short)(mod.patpos - 1);
						if (a.pat_reppos == -1)
						{
							mod.pat_repcrazy = 1;
							mod.patpos = 0;
						}
						else
							mod.patpos = (ushort)a.pat_reppos;
					}
					else a.pat_reppos = SharpMikCommon.POS_NONE;
				}
				else
					a.pat_reppos = (short)(mod.patpos - 1); /* set reppos - can be (-1) */
				break;
				case 0x7: /* set tremolo waveform */
				a.wavecontrol &= 0x0f;
				a.wavecontrol |= (byte)(nib << 4);
				break;
				case 0x8: /* set panning */
				if (mod.panflag)
				{
					if (nib <= 8) nib <<= 4;
					else nib *= 17;
					a.main.panning = nib;
					mod.panning[channel] = nib;
				}
				break;
				case 0x9: /* retrig note */
				/* do not retrigger on tick 0, until we are emulating FT2 and effect
				   data is zero */
				if (tick == 0 && !((flags & SharpMikCommon.UF_FT2QUIRKS) == SharpMikCommon.UF_FT2QUIRKS && (nib == 0)))
					break;
				/* only retrigger if data nibble > 0, or if tick 0 (FT2 compat) */
				if (nib != 0 || tick == 0)
				{
					if (a.retrig == 0)
					{
						/* when retrig counter reaches 0, reset counter and restart
						   the sample */
						if (a.main.period != 0) a.main.kick = SharpMikCommon.KICK_NOTE;
						a.retrig = (sbyte)nib;
					}
					a.retrig--; /* countdown */
				}
				break;
				case 0xa: /* fine volume slide up */
				if (tick != 0)
					break;
				a.tmpvolume += nib;
				if (a.tmpvolume > 64) a.tmpvolume = 64;
				break;
				case 0xb: /* fine volume slide dn  */
				if (tick != 0)
					break;
				a.tmpvolume -= nib;
				if (a.tmpvolume < 0) a.tmpvolume = 0;
				break;
				case 0xc: /* cut note */
				/* When tick reaches the cut-note value, turn the volume to
				   zero (just like on the amiga) */
				if (tick >= nib)
					a.tmpvolume = 0; /* just turn the volume down */
				break;
				case 0xd: /* note delay */
				/* delay the start of the sample until tick==nib */
				if (tick == 0)
					a.main.notedelay = nib;
				else if (a.main.notedelay != 0)
					a.main.notedelay--;
				break;
				case 0xe: /* pattern delay */
				if (tick == 0)
					if (mod.patdly2 == 0)
						mod.patdly = (byte)(nib + 1); /* only once, when tick=0 */
				break;
				case 0xf: /* invert loop, not supported  */
				break;
			}
		}

		static int DoPTEffectE(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			DoEEffects(tick, flags, a, mod, channel, s_UniTrack.UniGetByte());

			return 0;
		}

		static int DoPTEffectF(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte dat;

			dat = s_UniTrack.UniGetByte();
			if (tick != 0 || mod.patdly2 != 0)
				return 0;

			if (mod.extspd && (dat >= mod.bpmlimit))
				mod.bpm = dat;
			else
				if (dat != 0)
				{
					mod.sngspd = (ushort)((dat >= mod.bpmlimit) ? mod.bpmlimit - 1 : dat);
					mod.vbtick = 0;
				}

			return 0;
		}

		/*========== Scream Tracker effects */

		static int DoS3MEffectA(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte speed;

			speed = s_UniTrack.UniGetByte();

			if (tick != 0 || mod.patdly2 != 0)
				return 0;

			if (speed > 128)
				speed -= 128;
			if (speed != 0)
			{
				mod.sngspd = speed;
				mod.vbtick = 0;
			}

			return 0;
		}

		internal static void DoS3MVolSlide(ushort tick, ushort flags, MP_CONTROL a, byte inf)
		{
			byte lo, hi;

			if (inf != 0)
				a.s3mvolslide = inf;
			else
				inf = a.s3mvolslide;

			lo = (byte)(inf & 0xf);
			hi = (byte)(inf >> 4);

			if (lo == 0)
			{
				if ((tick != 0) || (flags & SharpMikCommon.UF_S3MSLIDES) == SharpMikCommon.UF_S3MSLIDES) a.tmpvolume += hi;
			}
			else
				if (hi == 0)
				{
					if ((tick != 0) || (flags & SharpMikCommon.UF_S3MSLIDES) == SharpMikCommon.UF_S3MSLIDES) a.tmpvolume -= lo;
				}
				else
					if (lo == 0xf)
					{
						if (tick == 0) a.tmpvolume += (short)(hi != 0 ? hi : 0xf);
					}
					else
						if (hi == 0xf)
						{
							if (tick == 0) a.tmpvolume -= (short)(lo != 0 ? lo : 0xf);
						}
						else
							return;

			if (a.tmpvolume < 0)
				a.tmpvolume = 0;
			else if (a.tmpvolume > 64)
				a.tmpvolume = 64;
		}

		static int DoS3MEffectD(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			DoS3MVolSlide(tick, flags, a, s_UniTrack.UniGetByte());

			return 1;
		}

		static void DoS3MSlideDn(ushort tick, MP_CONTROL a, byte inf)
		{
			byte hi, lo;

			if (inf != 0)
				a.slidespeed = inf;
			else
				inf = (byte)a.slidespeed;

			hi = (byte)(inf >> 4);
			lo = (byte)(inf & 0xf);

			if (hi == 0xf)
			{
				if (tick == 0) a.tmpperiod += (ushort)(lo << 2);
			}
			else
				if (hi == 0xe)
				{
					if (tick == 0) a.tmpperiod += lo;
				}
				else
				{
					if (tick != 0) a.tmpperiod += (ushort)(inf << 2);
				}
		}

		static int DoS3MEffectE(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte dat;

			dat = s_UniTrack.UniGetByte();
			if (a.main.period != 0)
				DoS3MSlideDn(tick, a, dat);

			return 0;
		}

		static void DoS3MSlideUp(ushort tick, MP_CONTROL a, byte inf)
		{
			byte hi, lo;

			if (inf != 0) a.slidespeed = inf;
			else inf = (byte)a.slidespeed;

			hi = (byte)(inf >> 4);
			lo = (byte)(inf & 0xf);

			if (hi == 0xf)
			{
				if (tick == 0) a.tmpperiod -= (ushort)(lo << 2);
			}
			else
				if (hi == 0xe)
				{
					if (tick == 0) a.tmpperiod -= lo;
				}
				else
				{
					if (tick != 0) a.tmpperiod -= (ushort)(inf << 2);
				}
		}

		static int DoS3MEffectF(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte dat;

			dat = s_UniTrack.UniGetByte();
			if (a.main.period != 0)
				DoS3MSlideUp(tick, a, dat);

			return 0;
		}

		static int DoS3MEffectI(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte inf, on, off;

			inf = s_UniTrack.UniGetByte();
			if (inf != 0)
				a.s3mtronof = inf;
			else
			{
				inf = a.s3mtronof;
				if (inf == 0)
					return 0;
			}

			if (tick == 0)
				return 0;

			on = (byte)((inf >> 4) + 1);
			off = (byte)((inf & 0xf) + 1);
			a.s3mtremor %= (byte)(on + off);
			a.volume = (short)((a.s3mtremor < on) ? a.tmpvolume : 0);
			a.ownvol = 1;
			a.s3mtremor++;

			return 0;
		}

		static int DoS3MEffectQ(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte inf;

			inf = s_UniTrack.UniGetByte();
			if (a.main.period != 0)
			{
				if (inf != 0)
				{
					a.s3mrtgslide = (byte)(inf >> 4);
					a.s3mrtgspeed = (byte)(inf & 0xf);
				}

				/* only retrigger if low nibble > 0 */
				if (a.s3mrtgspeed > 0)
				{
					if (a.retrig == 0)
					{
						/* when retrig counter reaches 0, reset counter and restart the
						   sample */
						if (a.main.kick != SharpMikCommon.KICK_NOTE) a.main.kick = SharpMikCommon.KICK_KEYOFF;
						a.retrig = (sbyte)a.s3mrtgspeed;

						if ((tick != 0) || (flags & SharpMikCommon.UF_S3MSLIDES) == SharpMikCommon.UF_S3MSLIDES)
						{
							switch (a.s3mrtgslide)
							{
								case 1:
								case 2:
								case 3:
								case 4:
								case 5:
								a.tmpvolume -= (short)(1 << (a.s3mrtgslide - 1));
								break;
								case 6:
								a.tmpvolume = (short)((2 * a.tmpvolume) / 3);
								break;
								case 7:
								a.tmpvolume >>= 1;
								break;
								case 9:
								case 0xa:
								case 0xb:
								case 0xc:
								case 0xd:
								a.tmpvolume += (short)(1 << (a.s3mrtgslide - 9));
								break;
								case 0xe:
								a.tmpvolume = (short)((3 * a.tmpvolume) >> 1);
								break;
								case 0xf:
								a.tmpvolume = (short)(a.tmpvolume << 1);
								break;
							}
							if (a.tmpvolume < 0)
								a.tmpvolume = 0;
							else if (a.tmpvolume > 64)
								a.tmpvolume = 64;
						}
					}
					a.retrig--; /* countdown  */
				}
			}

			return 0;
		}

		static int DoS3MEffectR(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte dat, q;
			ushort temp = 0;	/* silence warning */

			dat = s_UniTrack.UniGetByte();
			if (tick == 0)
			{
				if ((dat & 0x0f) != 0) a.trmdepth = (byte)(dat & 0xf);
				if ((dat & 0xf0) != 0) a.trmspd = (byte)((dat & 0xf0) >> 2);
			}

			q = (byte)((a.trmpos >> 2) & 0x1f);

			switch ((a.wavecontrol >> 4) & 3)
			{
				case 0: /* sine */
				temp = VibratoTable[q];
				break;
				case 1: /* ramp down */
				q <<= 3;
				if (a.trmpos < 0) q = (byte)(255 - q);
				temp = q;
				break;
				case 2: /* square wave */
				temp = 255;
				break;
				case 3: /* random */
				temp = (ushort)getrandom(256);
				break;
			}

			temp *= a.trmdepth;
			temp >>= 7;

			if (a.trmpos >= 0)
			{
				a.volume = (short)(a.tmpvolume + temp);
				if (a.volume > 64)
					a.volume = 64;
			}
			else
			{
				a.volume = (short)(a.tmpvolume - temp);
				if (a.volume < 0)
					a.volume = 0;
			}
			a.ownvol = 1;

			if (tick != 0)
				a.trmpos += (sbyte)a.trmspd;

			return 0;
		}

		static int DoS3MEffectT(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte tempo;

			tempo = s_UniTrack.UniGetByte();

			if (tick != 0 || mod.patdly2 != 0)
				return 0;

			mod.bpm = (ushort)((tempo < 32) ? 32 : tempo);

			return 0;
		}

		static int DoS3MEffectU(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte dat, q;
			ushort temp = 0;	/* silence warning */

			dat = s_UniTrack.UniGetByte();
			if (tick == 0)
			{
				if ((dat & 0x0f) != 0) a.vibdepth = (byte)(dat & 0xf);
				if ((dat & 0xf0) != 0) a.vibspd = (byte)((dat & 0xf0) >> 2);
			}
			else
				if (a.main.period != 0)
				{
					q = (byte)((a.vibpos >> 2) & 0x1f);

					switch (a.wavecontrol & 3)
					{
						case 0: /* sine */
						temp = VibratoTable[q];
						break;
						case 1: /* ramp down */
						q <<= 3;
						if (a.vibpos < 0) q = (byte)(255 - q);
						temp = q;
						break;
						case 2: /* square wave */
						temp = 255;
						break;
						case 3: /* random */
						temp = (ushort)getrandom(256);
						break;
					}

					temp *= a.vibdepth;
					temp >>= 8;

					if (a.vibpos >= 0)
						a.main.period = (ushort)(a.tmpperiod + temp);
					else
						a.main.period = (ushort)(a.tmpperiod - temp);
					a.ownper = 1;

					a.vibpos += (sbyte)a.vibspd;
				}

			return 0;
		}

		/*========== Envelope helpers */

		static int DoKeyOff(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			a.main.keyoff |= SharpMikCommon.KEY_OFF;
			if ((!((a.main.volflg & SharpMikCommon.EF_ON) == SharpMikCommon.EF_ON)) || (a.main.volflg & SharpMikCommon.EF_LOOP) == SharpMikCommon.EF_LOOP)
				a.main.keyoff = SharpMikCommon.KEY_KILL;

			return 0;
		}

		static int DoKeyFade(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte dat;

			dat = s_UniTrack.UniGetByte();
			if ((tick >= dat) || (tick == mod.sngspd - 1))
			{
				a.main.keyoff = SharpMikCommon.KEY_KILL;
				if (!((a.main.volflg & SharpMikCommon.EF_ON) == SharpMikCommon.EF_ON))
					a.main.fadevol = 0;
			}

			return 0;
		}

		/*========== Fast Tracker effects */

		/* DoXMEffect6 after DoXMEffectA */

		static int DoXMEffectA(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte inf, lo, hi;

			inf = s_UniTrack.UniGetByte();
			if (inf != 0)
				a.s3mvolslide = inf;
			else
				inf = a.s3mvolslide;

			if (tick != 0)
			{
				lo = (byte)(inf & 0xf);
				hi = (byte)(inf >> 4);

				if (hi == 0)
				{
					a.tmpvolume -= lo;
					if (a.tmpvolume < 0) a.tmpvolume = 0;
				}
				else
				{
					a.tmpvolume += hi;
					if (a.tmpvolume > 64) a.tmpvolume = 64;
				}
			}

			return 0;
		}

		static int DoXMEffect6(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			if (a.main.period != 0)
				DoVibrato(tick, a);

			return DoXMEffectA(tick, flags, a, mod, channel);
		}

		static int DoXMEffectE1(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte dat;

			dat = s_UniTrack.UniGetByte();
			if (tick == 0)
			{
				if (dat != 0) a.fportupspd = dat;
				if (a.main.period != 0)
					a.tmpperiod -= (ushort)(a.fportupspd << 2);
			}

			return 0;
		}

		static int DoXMEffectE2(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte dat;

			dat = s_UniTrack.UniGetByte();
			if (tick == 0)
			{
				if (dat != 0) a.fportdnspd = dat;
				if (a.main.period != 0)
					a.tmpperiod += (ushort)(a.fportdnspd << 2);
			}

			return 0;
		}

		static int DoXMEffectEA(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte dat;

			dat = s_UniTrack.UniGetByte();
			if (tick == 0)
				if (dat != 0) a.fslideupspd = dat;
			a.tmpvolume += a.fslideupspd;
			if (a.tmpvolume > 64) a.tmpvolume = 64;

			return 0;
		}

		static int DoXMEffectEB(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte dat;

			dat = s_UniTrack.UniGetByte();
			if (tick == 0)
				if (dat != 0) a.fslidednspd = dat;
			a.tmpvolume -= a.fslidednspd;
			if (a.tmpvolume < 0) a.tmpvolume = 0;

			return 0;
		}

		static int DoXMEffectG(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			mod.volume = (short)(s_UniTrack.UniGetByte() << 1);
			if (mod.volume > 128) mod.volume = 128;

			return 0;
		}

		static int DoXMEffectH(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte inf;

			inf = s_UniTrack.UniGetByte();

			if (tick != 0)
			{
				if (inf != 0) mod.globalslide = inf;
				else inf = mod.globalslide;
				if ((inf & 0xf0) == 0xf0) inf &= 0xf0;
				mod.volume = (short)(mod.volume + ((inf >> 4) - (inf & 0xf)) * 2);

				if (mod.volume < 0)
					mod.volume = 0;
				else if (mod.volume > 128)
					mod.volume = 128;
			}

			return 0;
		}

		static int DoXMEffectL(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte dat;

			dat = s_UniTrack.UniGetByte();
			if ((tick == 0) && (a.main.i != null))
			{
				ushort points;
				INSTRUMENT i = a.main.i;
				MP_VOICE aout;

				if ((aout = a.slave) != null)
				{
					if (aout.venv.env != null)
					{
						points = (ushort)i.volenv[i.volpts - 1].pos;
						aout.venv.p = aout.venv.env[(dat > points) ? points : dat].pos;
					}
					if (aout.penv.env != null)
					{
						points = (ushort)i.panenv[i.panpts - 1].pos;
						aout.penv.p = aout.penv.env[(dat > points) ? points : dat].pos;
					}
				}
			}

			return 0;
		}

		static int DoXMEffectP(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte inf, lo, hi;
			short pan;

			inf = s_UniTrack.UniGetByte();
			if (!mod.panflag)
				return 0;

			if (inf != 0)
				a.pansspd = inf;
			else
				inf = a.pansspd;

			if (tick != 0)
			{
				lo = (byte)(inf & 0xf);
				hi = (byte)(inf >> 4);

				/* slide right has absolute priority */
				if (hi != 0)
					lo = 0;

				pan = (short)(((a.main.panning == SharpMikCommon.PAN_SURROUND) ? SharpMikCommon.PAN_CENTER : a.main.panning) + hi - lo);
				a.main.panning = (short)((pan < SharpMikCommon.PAN_LEFT) ? SharpMikCommon.PAN_LEFT : (pan > SharpMikCommon.PAN_RIGHT ? SharpMikCommon.PAN_RIGHT : pan));
			}

			return 0;
		}

		static int DoXMEffectX1(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte dat;

			dat = s_UniTrack.UniGetByte();
			if (dat != 0)
				a.ffportupspd = dat;
			else
				dat = a.ffportupspd;

			if (a.main.period != 0)
				if (tick == 0)
				{
					a.main.period -= dat;
					a.tmpperiod -= dat;
					a.ownper = 1;
				}

			return 0;
		}

		static int DoXMEffectX2(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte dat;

			dat = s_UniTrack.UniGetByte();
			if (dat != 0)
				a.ffportdnspd = dat;
			else
				dat = a.ffportdnspd;

			if (a.main.period != 0)
				if (tick == 0)
				{
					a.main.period += dat;
					a.tmpperiod += dat;
					a.ownper = 1;
				}

			return 0;
		}

		/*========== Impulse Tracker effects */

		static void DoITToneSlide(ushort tick, MP_CONTROL a, byte dat)
		{
			if (dat != 0)
				a.portspeed = dat;

			/* if we don't come from another note, ignore the slide and play the note
			   as is */
			if (a.oldnote == 0 || a.main.period == 0)
				return;

			if ((tick == 0) && (a.newsamp != 0))
			{
				a.main.kick = SharpMikCommon.KICK_NOTE;
				a.main.start = -1;
			}
			else
				a.main.kick = (byte)((a.main.kick == SharpMikCommon.KICK_NOTE) ? SharpMikCommon.KICK_ENV : SharpMikCommon.KICK_ABSENT);

			if (tick != 0)
			{
				int dist;

				/* We have to slide a.main.period towards a.wantedperiod, compute the
				   difference between those two values */
				dist = a.main.period - a.wantedperiod;

				/* if they are equal or if portamentospeed is too big... */
				if ((dist == 0) || ((a.portspeed << 2) > Math.Abs(dist)))
					/* ... make tmpperiod equal tperiod */
					a.tmpperiod = a.main.period = a.wantedperiod;
				else
					if (dist > 0)
					{
						a.tmpperiod -= (ushort)(a.portspeed << 2);
						a.main.period -= (ushort)(a.portspeed << 2); /* dist>0 slide up */
					}
					else
					{
						a.tmpperiod += (ushort)(a.portspeed << 2);
						a.main.period += (ushort)(a.portspeed << 2); /* dist<0 slide down */
					}
			}
			else
				a.tmpperiod = a.main.period;
			a.ownper = 1;
		}

		static int DoITEffectG(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			DoITToneSlide(tick, a, s_UniTrack.UniGetByte());

			return 0;
		}

		static void DoITVibrato(ushort tick, MP_CONTROL a, byte dat)
		{
			byte q;
			ushort temp = 0;

			if (tick == 0)
			{
				if ((dat & 0x0f) != 0) a.vibdepth = (byte)(dat & 0xf);
				if ((dat & 0xf0) != 0) a.vibspd = (byte)((dat & 0xf0) >> 2);
			}
			if (a.main.period == 0)
				return;

			q = (byte)((a.vibpos >> 2) & 0x1f);

			switch (a.wavecontrol & 3)
			{
				case 0: /* sine */
				temp = VibratoTable[q];
				break;
				case 1: /* square wave */
				temp = 255;
				break;
				case 2: /* ramp down */
				q <<= 3;
				if (a.vibpos < 0) q = (byte)(255 - q);
				temp = q;
				break;
				case 3: /* random */
				temp = (ushort)getrandom(256);
				break;
			}

			temp *= a.vibdepth;
			temp >>= 8;
			temp <<= 2;

			if (a.vibpos >= 0)
				a.main.period = (ushort)(a.tmpperiod + temp);
			else
				a.main.period = (ushort)(a.tmpperiod - temp);
			a.ownper = 1;

			a.vibpos += (sbyte)a.vibspd;
		}

		static int DoITEffectH(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			DoITVibrato(tick, a, s_UniTrack.UniGetByte());

			return 0;
		}

		static int DoITEffectI(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte inf, on, off;

			inf = s_UniTrack.UniGetByte();
			if (inf != 0)
				a.s3mtronof = inf;
			else
			{
				inf = a.s3mtronof;
				if (inf == 0)
					return 0;
			}

			on = (byte)(inf >> 4);
			off = (byte)(inf & 0xf);

			a.s3mtremor %= (byte)(on + off);
			a.volume = (short)((a.s3mtremor < on) ? a.tmpvolume : 0);
			a.ownvol = 1;
			a.s3mtremor++;

			return 0;
		}

		static int DoITEffectM(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			a.main.chanvol = (sbyte)s_UniTrack.UniGetByte();
			if (a.main.chanvol > 64)
				a.main.chanvol = 64;
			else if (a.main.chanvol < 0)
				a.main.chanvol = 0;

			return 0;
		}

		static int DoITEffectN(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte inf, lo, hi;

			inf = s_UniTrack.UniGetByte();

			if (inf != 0)
				a.chanvolslide = inf;
			else
				inf = a.chanvolslide;

			lo = (byte)(inf & 0xf);
			hi = (byte)(inf >> 4);

			if (hi == 0)
				a.main.chanvol -= (sbyte)lo;
			else
				if (lo == 0)
				{
					a.main.chanvol += (sbyte)hi;
				}
				else
					if (hi == 0xf)
					{
						if (tick == 0) a.main.chanvol -= (sbyte)lo;
					}
					else
						if (lo == 0xf)
						{
							if (tick == 0) a.main.chanvol += (sbyte)hi;
						}

			if (a.main.chanvol < 0)
				a.main.chanvol = 0;
			else if (a.main.chanvol > 64)
				a.main.chanvol = 64;

			return 0;
		}

		static int DoITEffectP(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte inf, lo, hi;
			short pan;

			inf = s_UniTrack.UniGetByte();
			if (inf != 0)
				a.pansspd = inf;
			else
				inf = a.pansspd;

			if (!mod.panflag)
				return 0;

			lo = (byte)(inf & 0xf);
			hi = (byte)(inf >> 4);

			pan = (short)((a.main.panning == SharpMikCommon.PAN_SURROUND) ? SharpMikCommon.PAN_CENTER : a.main.panning);

			if (hi == 0)
				pan += (short)(lo << 2);
			else
				if (lo == 0)
				{
					pan -= (short)(hi << 2);
				}
				else
					if (hi == 0xf)
					{
						if (tick == 0) pan += (short)(lo << 2);
					}
					else
						if (lo == 0xf)
						{
							if (tick == 0) pan -= (short)(hi << 2);
						}
			a.main.panning = (short)((pan < SharpMikCommon.PAN_LEFT) ? SharpMikCommon.PAN_LEFT : (pan > SharpMikCommon.PAN_RIGHT ? SharpMikCommon.PAN_RIGHT : pan));

			return 0;
		}

		static int DoITEffectT(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte tempo;
			short temp;

			tempo = s_UniTrack.UniGetByte();

			if (mod.patdly2 != 0)
				return 0;

			temp = (byte)mod.bpm;
			if ((tempo & 0x10) == 0x10)
				temp += (byte)(tempo & 0x0f);
			else
				temp -= tempo;

			mod.bpm = (ushort)((temp > 255) ? 255 : (temp < 1 ? 1 : temp));

			return 0;
		}

		static int DoITEffectU(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte dat, q;
			ushort temp = 0;	/* silence warning */

			dat = s_UniTrack.UniGetByte();
			if (tick == 0)
			{
				if ((dat & 0x0f) != 0) a.vibdepth = (byte)(dat & 0xf);
				if ((dat & 0xf0) != 0) a.vibspd = (byte)((dat & 0xf0) >> 2);
			}
			if (a.main.period != 0)
			{
				q = (byte)((a.vibpos >> 2) & 0x1f);

				switch (a.wavecontrol & 3)
				{
					case 0: /* sine */
					temp = VibratoTable[q];
					break;
					case 1: /* square wave */
					temp = 255;
					break;
					case 2: /* ramp down */
					q <<= 3;
					if (a.vibpos < 0) q = (byte)(255 - q);
					temp = q;
					break;
					case 3: /* random */
					temp = (ushort)getrandom(256);
					break;
				}

				temp *= a.vibdepth;
				temp >>= 8;

				if (a.vibpos >= 0)
					a.main.period = (ushort)(a.tmpperiod + temp);
				else
					a.main.period = (ushort)(a.tmpperiod - temp);
				a.ownper = 1;

				a.vibpos += (sbyte)(a.vibspd);
			}

			return 0;
		}

		static int DoITEffectW(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte inf, lo, hi;

			inf = s_UniTrack.UniGetByte();

			if (inf != 0)
				mod.globalslide = inf;
			else
				inf = mod.globalslide;

			lo = (byte)(inf & 0xf);
			hi = (byte)(inf >> 4);

			if (lo == 0)
			{
				if (tick != 0) mod.volume += hi;
			}
			else
				if (hi == 0)
				{
					if (tick != 0) mod.volume -= lo;
				}
				else
					if (lo == 0xf)
					{
						if (tick == 0) mod.volume += hi;
					}
					else
						if (hi == 0xf)
						{
							if (tick == 0) mod.volume -= lo;
						}

			if (mod.volume < 0)
				mod.volume = 0;
			else if (mod.volume > 128)
				mod.volume = 128;

			return 0;
		}

		static int DoITEffectY(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte dat, q;
			int temp = 0;	/* silence warning */


			dat = s_UniTrack.UniGetByte();
			if (tick == 0)
			{
				if ((dat & 0x0f) != 0) a.panbdepth = (byte)(dat & 0xf);
				if ((dat & 0xf0) != 0) a.panbspd = (sbyte)((dat & 0xf0) >> 4);
			}
			if (mod.panflag)
			{
				q = a.panbpos;

				switch (a.panbwave)
				{
					case 0: /* sine */
					temp = PanbrelloTable[q];
					break;
					case 1: /* square wave */
					temp = (q < 0x80) ? 64 : 0;
					break;
					case 2: /* ramp down */
					q <<= 3;
					temp = q;
					break;
					case 3: /* random */
					temp = getrandom(256);
					break;
				}

				temp *= a.panbdepth;
				temp = (temp / 8) + mod.panning[channel];

				a.main.panning = (short)((temp < SharpMikCommon.PAN_LEFT) ? SharpMikCommon.PAN_LEFT : (temp > SharpMikCommon.PAN_RIGHT ? SharpMikCommon.PAN_RIGHT : temp));
				a.panbpos += (byte)a.panbspd;

			}

			return 0;
		}

		/* Impulse/Scream Tracker Sxx effects.
		   All Sxx effects share the same memory space. */
		static int DoITEffectS0(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte dat, inf, c;

			dat = s_UniTrack.UniGetByte();
			inf = (byte)(dat & 0xf);
			c = (byte)(dat >> 4);

			if (dat == 0)
			{
				c = a.sseffect;
				inf = a.ssdata;
			}
			else
			{
				a.sseffect = c;
				a.ssdata = inf;
			}

			switch (c)
			{
				case (byte)SharpMikCommon.ExtentedEffects.SS_GLISSANDO: /* S1x set glissando voice */
				DoEEffects(tick, flags, a, mod, channel, (byte)(0x30 | inf));
				break;

				case (byte)SharpMikCommon.ExtentedEffects.SS_FINETUNE: /* S2x set finetune */
				DoEEffects(tick, flags, a, mod, channel, (byte)(0x50 | inf));
				break;

				case (byte)SharpMikCommon.ExtentedEffects.SS_VIBWAVE: /* S3x set vibrato waveform */
				DoEEffects(tick, flags, a, mod, channel, (byte)(0x40 | inf));
				break;

				case (byte)SharpMikCommon.ExtentedEffects.SS_TREMWAVE: /* S4x set tremolo waveform */
				DoEEffects(tick, flags, a, mod, channel, (byte)(0x70 | inf));
				break;

				case (byte)SharpMikCommon.ExtentedEffects.SS_PANWAVE: /* S5x panbrello */
				a.panbwave = inf;
				break;

				case (byte)SharpMikCommon.ExtentedEffects.SS_FRAMEDELAY: /* S6x delay x number of frames (patdly) */
				DoEEffects(tick, flags, a, mod, channel, (byte)(0xe0 | inf));
				break;

				case (byte)SharpMikCommon.ExtentedEffects.SS_S7EFFECTS: /* S7x instrument / NNA commands */
				DoNNAEffects(mod, a, inf);
				break;

				case (byte)SharpMikCommon.ExtentedEffects.SS_PANNING: /* S8x set panning position */
				DoEEffects(tick, flags, a, mod, channel, (byte)(0x80 | inf));
				break;

				case (byte)SharpMikCommon.ExtentedEffects.SS_SURROUND: /* S9x set surround sound */
				{
					if (mod.panflag)
					{
						a.main.panning = (short)SharpMikCommon.PAN_SURROUND;
						mod.panning[channel] = (ushort)SharpMikCommon.PAN_SURROUND;
					}
					break;
				}
				case (byte)SharpMikCommon.ExtentedEffects.SS_HIOFFSET: /* SAy set high order sample offset yxx00h */
				{
					if (tick == 0)
					{
						a.hioffset = (uint)(inf << 16);
						a.main.start = a.hioffset | a.soffset;

						if ((a.main.s != null) && (a.main.start > a.main.s.length))
						{
							a.main.start = (a.main.s.flags & (SharpMikCommon.SF_LOOP | SharpMikCommon.SF_BIDI)) != 0 ?
								a.main.s.loopstart : a.main.s.length;
						}

					}
					break;
				}

				case (byte)SharpMikCommon.ExtentedEffects.SS_PATLOOP: /* SBx pattern loop */
				DoEEffects(tick, flags, a, mod, channel, (byte)(0x60 | inf));
				break;

				case (byte)SharpMikCommon.ExtentedEffects.SS_NOTECUT: /* SCx notecut */
				if (inf == 0) inf = 1;
				DoEEffects(tick, flags, a, mod, channel, (byte)(0xC0 | inf));
				break;

				case (byte)SharpMikCommon.ExtentedEffects.SS_NOTEDELAY: /* SDx notedelay */
				DoEEffects(tick, flags, a, mod, channel, (byte)(0xD0 | inf));
				break;

				case (byte)SharpMikCommon.ExtentedEffects.SS_PATDELAY: /* SEx patterndelay */
				DoEEffects(tick, flags, a, mod, channel, (byte)(0xE0 | inf));
				break;
			}

			return 0;
		}


		/*========== Impulse Tracker Volume/Pan Column effects */

		/*
		 * All volume/pan column effects share the same memory space.
		 */

		static int DoVolEffects(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte c, inf;

			c = s_UniTrack.UniGetByte();
			inf = s_UniTrack.UniGetByte();

			if ((c == 0) && (inf == 0))
			{
				c = a.voleffect;
				inf = a.voldata;
			}
			else
			{
				a.voleffect = c;
				a.voldata = inf;
			}

			if (c != 0)
				switch (c)
				{
					case (byte)SharpMikCommon.ITColumnEffect.VOL_VOLUME:
					if (tick != 0) break;
					if (inf > 64) inf = 64;
					a.tmpvolume = inf;
					break;
					case (byte)SharpMikCommon.ITColumnEffect.VOL_PANNING:
					if (mod.panflag)
						a.main.panning = inf;
					break;
					case (byte)SharpMikCommon.ITColumnEffect.VOL_VOLSLIDE:
					DoS3MVolSlide(tick, flags, a, inf);
					return 1;
					case (byte)SharpMikCommon.ITColumnEffect.VOL_PITCHSLIDEDN:
					if (a.main.period != 0)
						DoS3MSlideDn(tick, a, inf);
					break;
					case (byte)SharpMikCommon.ITColumnEffect.VOL_PITCHSLIDEUP:
					if (a.main.period != 0)
						DoS3MSlideUp(tick, a, inf);
					break;
					case (byte)SharpMikCommon.ITColumnEffect.VOL_PORTAMENTO:
					DoITToneSlide(tick, a, inf);
					break;
					case (byte)SharpMikCommon.ITColumnEffect.VOL_VIBRATO:
					DoITVibrato(tick, a, inf);
					break;
				}

			return 0;
		}

		/*========== UltraTracker effects */

		static int DoULTEffect9(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			ushort offset = s_UniTrack.UniGetWord();

			if (offset != 0)
				a.ultoffset = offset;

			a.main.start = a.ultoffset << 2;
			if ((a.main.s != null) && (a.main.start > a.main.s.length))
			{
				a.main.start = (a.main.s.flags & (SharpMikCommon.SF_LOOP | SharpMikCommon.SF_BIDI)) != 0 ?
									a.main.s.loopstart : a.main.s.length;
			}

			return 0;
		}

		/*========== OctaMED effects */

		static int DoMEDSpeed(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			ushort speed = s_UniTrack.UniGetWord();

			mod.bpm = speed;

			return 0;
		}

		static int DoMEDEffectF1(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			DoEEffects(tick, flags, a, mod, channel, (byte)(0x90 | (mod.sngspd / 2)));

			return 0;
		}

		static int DoMEDEffectF2(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			DoEEffects(tick, flags, a, mod, channel, (byte)(0xd0 | (mod.sngspd / 2)));

			return 0;
		}

		static int DoMEDEffectF3(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			DoEEffects(tick, flags, a, mod, channel, (byte)(0x90 | (mod.sngspd / 3)));

			return 0;
		}

		/*========== Oktalyzer effects */

		static int DoOktArp(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			byte dat, dat2;

			dat2 = s_UniTrack.UniGetByte();	/* arpeggio style */
			dat = s_UniTrack.UniGetByte();
			if (tick == 0)
			{
				if (dat == 0 && (flags & SharpMikCommon.UF_ARPMEM) == SharpMikCommon.UF_ARPMEM)
					dat = a.arpmem;
				else
					a.arpmem = dat;
			}
			if (a.main.period != 0)
				DoArpeggio(tick, flags, a, dat2);

			return 0;
		}

		static int DoNothing(ushort tick, ushort flags, MP_CONTROL a, MODULE mod, short channel)
		{
			s_UniTrack.UniSkipOpcode();

			return 0;
		}


		internal static void DoNNAEffects(MODULE mod, MP_CONTROL a, byte dat)
		{
			int t;
			MP_VOICE aout;

			dat &= 0xf;
			aout = (a.slave != null) ? a.slave : null;

			switch (dat)
			{
				case 0x0: /* past note cut */
				for (t = 0; t < ModDriver.md_sngchn; t++)
					if (mod.voice[t].master == a)
						mod.voice[t].main.fadevol = 0;
				break;
				case 0x1: /* past note off */
				for (t = 0; t < ModDriver.md_sngchn; t++)
					if (mod.voice[t].master == a)
					{
						mod.voice[t].main.keyoff |= SharpMikCommon.KEY_OFF;
						if (!((mod.voice[t].venv.flg & SharpMikCommon.EF_ON) == SharpMikCommon.EF_ON) ||
						   (mod.voice[t].venv.flg & SharpMikCommon.EF_LOOP) == SharpMikCommon.EF_LOOP)
							mod.voice[t].main.keyoff = SharpMikCommon.KEY_KILL;
					}
				break;
				case 0x2: /* past note fade */
				for (t = 0; t < ModDriver.md_sngchn; t++)
					if (mod.voice[t].master == a)
						mod.voice[t].main.keyoff |= SharpMikCommon.KEY_FADE;
				break;
				case 0x3: /* set NNA note cut */
				a.main.nna = (byte)((a.main.nna & ~SharpMikCommon.NNA_MASK) | SharpMikCommon.NNA_CUT);
				break;
				case 0x4: /* set NNA note continue */
				a.main.nna = (byte)((a.main.nna & ~SharpMikCommon.NNA_MASK) | SharpMikCommon.NNA_CONTINUE);
				break;
				case 0x5: /* set NNA note off */
				a.main.nna = (byte)((a.main.nna & ~SharpMikCommon.NNA_MASK) | SharpMikCommon.NNA_OFF);
				break;
				case 0x6: /* set NNA note fade */
				a.main.nna = (byte)((a.main.nna & ~SharpMikCommon.NNA_MASK) | SharpMikCommon.NNA_FADE);
				break;
				case 0x7: /* disable volume envelope */
				if (aout != null)
					aout.main.volflg = (byte)(aout.main.volflg & ~SharpMikCommon.EF_ON);
				break;
				case 0x8: /* enable volume envelope  */
				if (aout != null)
					aout.main.volflg |= SharpMikCommon.EF_ON;
				break;
				case 0x9: /* disable panning envelope */
				if (aout != null)
					aout.main.panflg = (byte)(aout.main.panflg & ~SharpMikCommon.EF_ON);
				break;
				case 0xa: /* enable panning envelope */
				if (aout != null)
					aout.main.panflg |= SharpMikCommon.EF_ON;
				break;
				case 0xb: /* disable pitch envelope */
				if (aout != null)
					aout.main.pitflg = (byte)(aout.main.panflg & ~SharpMikCommon.EF_ON);
				break;
				case 0xc: /* enable pitch envelope */
				if (aout != null)
					aout.main.pitflg |= SharpMikCommon.EF_ON;
				break;
			}
		}



		static effectDelegate[] effects =
		{
			DoNothing,		/* 0 */
			DoNothing,		/* UNI_NOTE */
			DoNothing,		/* UNI_INSTRUMENT */
			DoPTEffect0,	/* UNI_PTEFFECT0 */
			DoPTEffect1,	/* UNI_PTEFFECT1 */
			DoPTEffect2,	/* UNI_PTEFFECT2 */
			DoPTEffect3,	/* UNI_PTEFFECT3 */
			DoPTEffect4,	/* UNI_PTEFFECT4 */
			DoPTEffect5,	/* UNI_PTEFFECT5 */
			DoPTEffect6,	/* UNI_PTEFFECT6 */
			DoPTEffect7,	/* UNI_PTEFFECT7 */
			DoPTEffect8,	/* UNI_PTEFFECT8 */
			DoPTEffect9,	/* UNI_PTEFFECT9 */
			DoPTEffectA,	/* UNI_PTEFFECTA */
			DoPTEffectB,	/* UNI_PTEFFECTB */
			DoPTEffectC,	/* UNI_PTEFFECTC */
			DoPTEffectD,	/* UNI_PTEFFECTD */
			DoPTEffectE,	/* UNI_PTEFFECTE */
			DoPTEffectF,	/* UNI_PTEFFECTF */
			DoS3MEffectA,	/* UNI_S3MEFFECTA */
			DoS3MEffectD,	/* UNI_S3MEFFECTD */
			DoS3MEffectE,	/* UNI_S3MEFFECTE */
			DoS3MEffectF,	/* UNI_S3MEFFECTF */
			DoS3MEffectI,	/* UNI_S3MEFFECTI */
			DoS3MEffectQ,	/* UNI_S3MEFFECTQ */
			DoS3MEffectR,	/* UNI_S3MEFFECTR */
			DoS3MEffectT,	/* UNI_S3MEFFECTT */
			DoS3MEffectU,	/* UNI_S3MEFFECTU */
			DoKeyOff,		/* UNI_KEYOFF */
			DoKeyFade,		/* UNI_KEYFADE */
			DoVolEffects,	/* UNI_VOLEFFECTS */
			DoPTEffect4,	/* UNI_XMEFFECT4 */
			DoXMEffect6,	/* UNI_XMEFFECT6 */
			DoXMEffectA,	/* UNI_XMEFFECTA */
			DoXMEffectE1,	/* UNI_XMEFFECTE1 */
			DoXMEffectE2,	/* UNI_XMEFFECTE2 */
			DoXMEffectEA,	/* UNI_XMEFFECTEA */
			DoXMEffectEB,	/* UNI_XMEFFECTEB */
			DoXMEffectG,	/* UNI_XMEFFECTG */
			DoXMEffectH,	/* UNI_XMEFFECTH */
			DoXMEffectL,	/* UNI_XMEFFECTL */
			DoXMEffectP,	/* UNI_XMEFFECTP */
			DoXMEffectX1,	/* UNI_XMEFFECTX1 */
			DoXMEffectX2,	/* UNI_XMEFFECTX2 */
			DoITEffectG,	/* UNI_ITEFFECTG */
			DoITEffectH,	/* UNI_ITEFFECTH */
			DoITEffectI,	/* UNI_ITEFFECTI */
			DoITEffectM,	/* UNI_ITEFFECTM */
			DoITEffectN,	/* UNI_ITEFFECTN */
			DoITEffectP,	/* UNI_ITEFFECTP */
			DoITEffectT,	/* UNI_ITEFFECTT */
			DoITEffectU,	/* UNI_ITEFFECTU */
			DoITEffectW,	/* UNI_ITEFFECTW */
			DoITEffectY,	/* UNI_ITEFFECTY */
			DoNothing,		/* UNI_ITEFFECTZ */
			DoITEffectS0,	/* UNI_ITEFFECTS0 */
			DoULTEffect9,	/* UNI_ULTEFFECT9 */
			DoMEDSpeed,		/* UNI_MEDSPEED */
			DoMEDEffectF1,	/* UNI_MEDEFFECTF1 */
			DoMEDEffectF2,	/* UNI_MEDEFFECTF2 */
			DoMEDEffectF3,	/* UNI_MEDEFFECTF3 */
			DoOktArp,		/* UNI_OKTARP */
		};

		internal static int pt_playeffects(MODULE mod, short channel, MP_CONTROL a)
		{
			ushort tick = mod.vbtick;
			ushort flags = mod.flags;
			byte c;
			int explicitslides = 0;


			effectDelegate f;

			while ((c = s_UniTrack.UniGetByte()) != 0)
			{
				f = effects[c];

				if (f != DoNothing)
					a.sliding = 0;
				explicitslides |= f(tick, flags, a, mod, channel);

			}
			return explicitslides;
		}


		#endregion
	}
}
