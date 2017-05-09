using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SharpMik;
using SharpMik.Player;
using SharpMik.Drivers;
using System.Threading;

using System.IO;

namespace SharpMilk
{
	public partial class Form1 : Form
	{
		Module m_Mod = null;
		bool m_Playing = false;


		MikMod m_Player;

		public Form1()
		{
			InitializeComponent();

			m_Player = new MikMod();
			m_Player.PlayerStateChangeEvent += new ModPlayer.PlayerStateChangedEvent(m_Player_PlayerStateChangeEvent);

			trackBar1.Maximum = 99;
		}

		void m_Player_PlayerStateChangeEvent(ModPlayer.PlayerState state)
		{
			if (state == ModPlayer.PlayerState.kStopped)
			{
				Next();
			}
			else
			{
				int place = (int)(100.0f * m_Player.GetProgress());

				MethodInvoker action = delegate
				{
					trackBar1.Value = place;
				};
				trackBar1.BeginInvoke(action);
			}
			
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			PlayPauseMod.Enabled = false;
			StopMod.Enabled = false;

			ModDriver.Mode = (ushort)(ModDriver.Mode | SharpMikCommon.DMODE_NOISEREDUCTION);
			try
			{
				m_Player.Init<NaudioDriver>("");

				DateTime start = DateTime.Now;
			}
			catch (System.Exception ex)
			{
				Console.WriteLine(ex);
			}

		}



		private void toolStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{

		}

		private void OpenMod_Click(object sender, EventArgs e)
		{
			OpenFileDialog dialog = new OpenFileDialog();
            var extentions = SharpMikCommon.ModFileExtentions;

            String filters = "All (*.*)|*.*|";
            foreach (var item in extentions)
            {
                filters +=  "(*"+item + ")|*" + item + "|";
            }

            if (filters.Length > 0)
            {
                dialog.Filter = filters.Substring(0, filters.Length - 1);
            }

			DialogResult result = dialog.ShowDialog();

			if (result == DialogResult.OK)
			{
				m_Mod = m_Player.LoadModule(dialog.FileName);


				if (m_Mod != null)
				{
					toolStripLabel1.Text = m_Mod.songname;
				}
				PlayPauseMod.Enabled = true;
				StopMod.Enabled = false;

				m_Playing = false;
				m_WasPlaying = false;
				PlayPauseMod.Image = global::SharpMikTester.Properties.Resources.PlayHS;
			}	
		}

		bool m_WasPlaying = false;
		private void PlayPauseMod_Click(object sender, EventArgs e)
		{
			if (m_Playing)
			{
				m_Playing = false;
				m_WasPlaying = true;
				PlayPauseMod.Image = global::SharpMikTester.Properties.Resources.PlayHS;
				ModPlayer.Player_TogglePause();
			}
			else
			{
				m_Playing = true;
				PlayPauseMod.Image = global::SharpMikTester.Properties.Resources.PauseHS;

				if (m_WasPlaying)
				{
					ModPlayer.Player_TogglePause();
				}
				else
				{
					m_Player.Play(m_Mod);

					StopMod.Enabled = true;

					OpenMod.Enabled = false;
				}
			}
		}

		private void StopMod_Click(object sender, EventArgs e)
		{
			m_Playing = false;
			ModPlayer.Player_Stop();

			OpenMod.Enabled = true;
			StopMod.Enabled = false;
		}

		private void CloseApp_Click(object sender, EventArgs e)
		{
			ModPlayer.Player_Stop();

			ModDriver.MikMod_Exit();
			this.Close();
		}

		private void toolStripComboBox1_Click(object sender, EventArgs e)
		{

		}


		List<String> m_FileList = new List<String>();
		int place = 0;

		private void toolStripButton1_Click(object sender, EventArgs e)
		{
			FolderBrowserDialog dialog = new FolderBrowserDialog();

			DialogResult result = dialog.ShowDialog();


			if (result == DialogResult.OK)
			{
				var modFiles = Directory.EnumerateFiles(dialog.SelectedPath, "*.*", SearchOption.AllDirectories);
				listBox1.Items.Clear();

				foreach (String name in modFiles)
				{
					if (SharpMikCommon.MatchesExtentions(name))
					{
						m_FileList.Add(name);
						String shortName = Path.GetFileNameWithoutExtension(name);
						listBox1.Items.Add(shortName+ " ("+name+")");
					}
				}				

				place = 0;

				if (!Play())
				{
					Next();
				}
			}
		}

		private void Next()
		{
			if (m_FileList.Count > 0)
			{
				place++;

				if (place >= m_FileList.Count)
				{
					place = 0;
				}

				//m_Mod = m_Player.LoadModule(m_FileList[place]);


				if (!Play())
				{
					Next();
				}
			}
		}

		private void Prev()
		{
			place--;

			if (place < 0 )
			{
				place = m_FileList.Count - 1;
			}

			if (!Play())
			{
				Prev();
			}
		}


		private void Stop()
		{
			m_Player.Stop();
		}

		private bool Play()
		{

			m_Mod = m_Player.Play(m_FileList[place]);
			toolStripLabel1.Text = m_Mod.songname;
			/*
			try
			{
				m_Mod = m_Player.LoadModule(m_FileList[place]);
			}
			catch
			{
				m_Mod = null;
				return false;
			}
			
			toolStripLabel1.Text = m_Mod.songname;

			m_Player.Play(m_Mod);
			 * */
			return true;
		}

		private void toolStripButton3_Click(object sender, EventArgs e)
		{
			Next();
		}

		private void toolStripButton2_Click(object sender, EventArgs e)
		{

		}
	}
}
