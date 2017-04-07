using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using SharpMik.Player;
using SharpMikXNA.Drivers;
using Sentia.Input;
using SharpMik;


#if WINDOWS_PHONE
using System.Windows;
using System.IO;
#endif

namespace XNAWindowsTest
{
	/// <summary>
	/// This is the main type for your game
	/// </summary>
	public class Game1 : Microsoft.Xna.Framework.Game
	{
		GraphicsDeviceManager graphics;
		SpriteBatch spriteBatch;

		MikMod m_Player;

		SpriteFont m_Font;


		Texture2D m_Background;
		Texture2D m_ProgressBar;
		Texture2D m_ProgressBarCover;

		Texture2D[] m_Buttons;		
		Vector2[] m_ButtonLocations;

		Texture2D[] m_PadIcons;
		Rectangle[] m_PadLocations;

		String m_Message2 = "";
		float m_ProgressPercentage = 0.0f;

		bool m_PadActive = false;

		PadHelper m_Pad;
		MouseHelper m_Mouse;

		int m_ModCount = 6;

		int m_CurrentMod = 1;


		Module m_Mod;
		bool m_IsPlaying = false;

		public Game1()
		{
			graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";
			m_Player = new MikMod();
		}

		protected override void Initialize()
		{

			base.Initialize();
		}


		protected override void LoadContent()
		{
			spriteBatch = new SpriteBatch(GraphicsDevice);

			// When setting up the SharpMik Player you need to pass a driver to it
			//  in this instance we are using the XNA driver from the lib.
			//  additional data can also be passed to the driver if the driver needs / supports it.
			m_Player.Init<XNADriver>();

			// Hook up the event so we know when the player changes
			m_Player.PlayerStateChangeEvent += new ModPlayer.PlayerStateChangedEvent(m_Player_PlayerStateChangeEvent);			

			// Setup some basic stuff for the sample
			m_Font = Content.Load<SpriteFont>("fonts/SpriteFont1");
			m_Pad = new PadHelper(InputPlayer.One);
			m_Mouse = new MouseHelper();


			// Load all the textures.
			m_Background = Content.Load<Texture2D>("Art/background");
			m_ProgressBar = Content.Load<Texture2D>("Art/Progess");
			m_ProgressBarCover = Content.Load<Texture2D>("Art/ProgressCover");

			m_Buttons = new Texture2D[4];

			m_Buttons[0] = Content.Load<Texture2D>("Art/Back");
			m_Buttons[1] = Content.Load<Texture2D>("Art/Play");
			m_Buttons[2] = Content.Load<Texture2D>("Art/Stop");
			m_Buttons[3] = Content.Load<Texture2D>("Art/Next");

			m_ButtonLocations = new Vector2[4];
			m_ButtonLocations[0] = new Vector2(50, 300);
			m_ButtonLocations[1] = new Vector2(250, 300);
			m_ButtonLocations[2] = new Vector2(450, 300);
			m_ButtonLocations[3] = new Vector2(650, 300);
			

			m_PadIcons = new Texture2D[4];
			m_PadIcons[0] = Content.Load<Texture2D>("Art/xboxControllerLeftShoulder");
			m_PadIcons[1] = Content.Load<Texture2D>("Art/xboxControllerButtonA");
			m_PadIcons[2] = Content.Load<Texture2D>("Art/xboxControllerButtonB");
			m_PadIcons[3] = Content.Load<Texture2D>("Art/xboxControllerRightShoulder");
			
			m_PadLocations = new Rectangle[4];
			m_PadLocations[0] = new Rectangle(150, 300,64,32);
			m_PadLocations[1] = new Rectangle(350, 300, 32, 32);
			m_PadLocations[2] = new Rectangle(450, 300, 32, 32);
			m_PadLocations[3] = new Rectangle(625, 300, 64, 32);

			IsMouseVisible = true;
		}

		void m_Player_PlayerStateChangeEvent(ModPlayer.PlayerState state)
		{
			if (state == ModPlayer.PlayerState.kStopped)
			{
				if (m_IsPlaying)
				{
					Next();
				}
			}
		}

		protected override void UnloadContent()
		{

		}




		void Play()
		{
			m_IsPlaying = false;

			if (m_Mod != null)
			{
				m_Player.UnLoadModule(m_Mod);
				m_Mod = null;
			}

#if WINDOWS_PHONE
			// On Windows Phone we need to get a resource stream to get access to the music we included.
			var ResrouceStream = Application.GetResourceStream(new Uri("content/mods/music" + m_CurrentMod + ".mod", UriKind.Relative));

			if (ResrouceStream != null)
			{
				using (Stream stream = ResrouceStream.Stream)
				{
					// The Player supports taking in a stream so it can just be passed in.
					mod = m_Player.Play(stream);
				}
			}
#else
			// On Windows and Xbox we have access to the regular file stream so we can just pass a string and let the lib handle the stream.
			m_Mod = m_Player.Play("content/mods/music" + m_CurrentMod + ".mod");
#endif
			if (m_Mod != null)
			{
				m_Message2 = m_Mod.songname;
				m_IsPlaying = true;
			}
			else
			{
				if(m_Player.HasError)
				{
					m_Message2 = m_Player.ErrorMessage;					
				}				
			}
		}


		void Stop()
		{
			m_Player.Stop();
			m_IsPlaying = false;
		}


		void Next()
		{
			m_CurrentMod++;

			if (m_CurrentMod > m_ModCount)
			{
				m_CurrentMod = 1;
			}

			if (m_IsPlaying)
			{
				Play();
			}
		}

		void Prev()
		{
			m_CurrentMod--;

			if (m_CurrentMod < 1)
			{
				m_CurrentMod = m_ModCount;
			}

			if (m_IsPlaying)
			{
				Play();
			}
		}

		protected override void Update(GameTime gameTime)
		{
			float frameTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
			
			m_Mouse.Update(frameTime);
			m_Pad.Update(frameTime);

#if WINDOWS_PHONE
			m_PadActive = false;			
#elif WINDOWS
			m_PadActive = m_Pad.IsActive();
#elif XBOX
			m_PadActive = true;
#endif

			if (m_PadActive)
			{
				if (m_Pad.CheckButton(InputPadButtons.LBumper) == InputState.Pressed)
				{
					Prev();
				}

				if (m_Pad.CheckButton(InputPadButtons.RBumper) == InputState.Pressed)
				{
					Next();
				}


				if (m_Pad.CheckButton(InputPadButtons.A) == InputState.Pressed)
				{
					Play();
				}

				if (m_Pad.CheckButton(InputPadButtons.B) == InputState.Pressed)
				{
					Stop();
				}
			}

			// Allows the game to exit, WP7 can pick this up as well.
			if (m_Pad.CheckButton(InputPadButtons.Back) == InputState.Pressed)
				this.Exit();


			if (m_Mouse.CheckButton(InputMouseButtons.Left) == InputState.Pressed)
			{
				int selectedButton = -1;
				for (int i = 0; i < 4;i++ )
				{
					if (m_Mouse.GetX() > m_ButtonLocations[i].X && m_Mouse.GetX() < m_ButtonLocations[i].X + m_Buttons[i].Width
						&& m_Mouse.GetY() > m_ButtonLocations[i].Y && m_Mouse.GetY() < m_ButtonLocations[i].Y + m_Buttons[i].Height)
					{
						selectedButton = i;
						break;
					}
				}

				if (selectedButton != -1)
				{
					switch (selectedButton)
					{
						case 0:
						{
							Prev();
							break;
						}

						case 1:
						{
							Play();
							break;
						}

						case 2:
						{
							Stop();
							break;
						}

						case 3:
						{
							Next();
							break;
						}
					}
				}
			}

			m_ProgressPercentage = m_Player.GetProgress();

			base.Update(gameTime);
		}


		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.CornflowerBlue);

			spriteBatch.Begin();

			spriteBatch.Draw(m_Background, new Vector2(0, 0), Color.White);

			spriteBatch.DrawString(m_Font, m_Message2, new Vector2(200, 140), Color.Black);

			spriteBatch.Draw(m_ProgressBar, new Rectangle(42, 209,(int)(m_ProgressBar.Width * m_ProgressPercentage), m_ProgressBar.Height), Color.White);
			spriteBatch.Draw(m_ProgressBarCover, new Vector2(42, 209), Color.White);


			for (int i = 0; i < 4; i++)
			{
				spriteBatch.Draw(m_Buttons[i], m_ButtonLocations[i], Color.White);

				if (m_PadActive)
				{
					spriteBatch.Draw(m_PadIcons[i], m_PadLocations[i], Color.White);
				}				
			}


			spriteBatch.End();


			base.Draw(gameTime);
		}
	}
}
