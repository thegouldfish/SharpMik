using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Sentia.Input
{
    public enum InputDPad
    {
        Up,
        Down,
        Left,
        Right,
        Count
    };

    public enum InputTriggers
    {
        Right,
        Left
    };

    public enum InputPadButtons
    {
        A,
        B,
        Y,
        X,
        Start,
        Back,
        RBumper,
        LBumper,
        RStick,
        LStick
    };

    public enum InputPlayer
    {
        One,
        Two,
        Three,
        Four
    };

    public enum InputState
    {
        Up,
        Released,

        Held,
        Pressed,        
        NoMatch
    };

    public class PadHelper
    {
        PlayerIndex m_Player;
        GamePadState m_OldState;
        GamePadState m_NewState;

        bool m_invertLeftY;
        bool m_invertRightY;
        bool m_hasAction;


        bool m_JoinDpadAndAnalog = false;

        float m_DeadZone = 0.2f;

        ButtonState[] m_NewAnalogDPad;
        ButtonState[] m_PreviousAnalogDPad;

        public PadHelper(InputPlayer player)
        {
            // we first need to figure out which pad we're dealing with
            // use the Visual Studio Express 'insert snippet' command to make adding switch statements even easier.
            switch (player)
            {
                case InputPlayer.One:
                    m_Player = PlayerIndex.One;
                    break;
                case InputPlayer.Two:
                    m_Player = PlayerIndex.Two;
                    break;
                case InputPlayer.Three:
                    m_Player = PlayerIndex.Three;
                    break;
                case InputPlayer.Four:
                    m_Player = PlayerIndex.Four;
                    break;
                default:
                    m_Player = PlayerIndex.One;
                    break;
            }

            // update our pad states now we know which pad we're dealing with
            m_OldState = GamePad.GetState(m_Player);
            m_NewState = GamePad.GetState(m_Player);

            m_NewAnalogDPad = new ButtonState[(int)InputDPad.Count];
            m_PreviousAnalogDPad = new ButtonState[(int)InputDPad.Count];

            for (int i = 0; i < (int)InputDPad.Count; i++)
            {
                m_NewAnalogDPad[i] = ButtonState.Released;
                m_PreviousAnalogDPad[i] = ButtonState.Released;
            }
        }


        public void JoinDPadAndAnalog(bool join)
        {
            m_JoinDpadAndAnalog = join;
        }

        public PlayerIndex GetPlayerIndex()
        {
            return m_Player;
        }

        public bool HasAction()
        {
            return m_hasAction;
        }

        public void SetLeftStickInvert(bool invert)
        {
            m_invertLeftY = invert;
        }

        public void SetRightStickInvert(bool invert)
        {
            m_invertRightY = invert;
        }

        public void Update(float gameTime)
        {
            m_OldState = m_NewState;
            m_NewState = GamePad.GetState(m_Player);

            if (m_JoinDpadAndAnalog)
            {
                for (int i = 0; i < 4; i++)
                {
                    m_PreviousAnalogDPad[i] = m_NewAnalogDPad[i];
                }
                JoinUp();
            }

            ActionCheck();
        }


        public void JoinUp()
        {

            float y = m_NewState.ThumbSticks.Left.Y;

            if (y > 0.0f && y < m_DeadZone)
            {
                y = 0.0f;
            }

            float x = m_NewState.ThumbSticks.Left.X;

            for (int i = 0; i < (int)InputDPad.Count; i++)
            {
                m_NewAnalogDPad[i] = ButtonState.Released;
            }

            if (x > m_DeadZone)
            {
                m_NewAnalogDPad[(int)InputDPad.Right] = ButtonState.Pressed;
            }
            else if (x < -m_DeadZone)
            {
                m_NewAnalogDPad[(int)InputDPad.Left] = ButtonState.Pressed;
            }


            if (y > m_DeadZone)
            {
                m_NewAnalogDPad[(int)InputDPad.Up] = ButtonState.Pressed;
            }
            else if (y < -m_DeadZone)
            {
                m_NewAnalogDPad[(int)InputDPad.Down] = ButtonState.Pressed;
            }

            
        }

        public bool AnyButtonReleased()
        {
            return CheckButton(InputPadButtons.A)       == InputState.Released ||
                CheckButton(InputPadButtons.B)          == InputState.Released ||
                CheckButton(InputPadButtons.Y)          == InputState.Released ||
                CheckButton(InputPadButtons.X)          == InputState.Released ||
                CheckButton(InputPadButtons.Back)       == InputState.Released ||
                CheckButton(InputPadButtons.LBumper)    == InputState.Released ||
                CheckButton(InputPadButtons.RBumper)    == InputState.Released ||
                CheckButton(InputPadButtons.LStick)     == InputState.Released ||
                CheckButton(InputPadButtons.RStick)     == InputState.Released ||
                CheckButton(InputPadButtons.Start)      == InputState.Released;
        }


        private void ActionCheck()
        {
            m_hasAction = m_OldState.PacketNumber != m_NewState.PacketNumber;
        }

        private InputState ButtonChecker(ButtonState newState, ButtonState oldState)
        {
            // we get passed the button states from our old and new pad states for a specific button, this allows us to only write this code once, rather than once for each button we want to check like this!
            InputState returnValue;

            if (newState == ButtonState.Pressed && oldState == ButtonState.Pressed)
                returnValue = InputState.Held;
            else if (oldState == ButtonState.Released && newState == ButtonState.Pressed)
                returnValue = InputState.Pressed;
            else if(oldState == ButtonState.Pressed && newState == ButtonState.Released)
                returnValue = InputState.Released;
            else
                returnValue = InputState.Up;

            return returnValue;
        }

        public InputState CheckButton(InputPadButtons button)
        {
            // rather than checking each key every update, we only check keys we want to            

            switch( button )
            {
                case InputPadButtons.A:
                    return ButtonChecker(m_NewState.Buttons.A, m_OldState.Buttons.A);

                case InputPadButtons.B:
                    return ButtonChecker(m_NewState.Buttons.B, m_OldState.Buttons.B);

                case InputPadButtons.X:
                    return ButtonChecker(m_NewState.Buttons.X, m_OldState.Buttons.X);

                case InputPadButtons.Y:
                    return ButtonChecker(m_NewState.Buttons.Y, m_OldState.Buttons.Y);

                case InputPadButtons.Back:
                    return ButtonChecker(m_NewState.Buttons.Back, m_OldState.Buttons.Back);

                case InputPadButtons.Start:
                    return ButtonChecker(m_NewState.Buttons.Start, m_OldState.Buttons.Start);

                case InputPadButtons.LBumper:
                    return ButtonChecker(m_NewState.Buttons.LeftShoulder, m_OldState.Buttons.LeftShoulder);

                case InputPadButtons.RBumper:
                    return ButtonChecker(m_NewState.Buttons.RightShoulder, m_OldState.Buttons.RightShoulder);


                // if in doubt, return showing a no match
                default:
                    return InputState.NoMatch;
            }
        }

        public InputState CheckDPad(InputDPad direction)
        {
            // this works in the same fashion as CheckButton
            int id = (int)direction;
            InputState analog = ButtonChecker(m_NewAnalogDPad[id], m_PreviousAnalogDPad[id]);

            switch(direction)
            {
                case InputDPad.Up:
                    return JoinUpCheck(ButtonChecker(m_NewState.DPad.Up, m_OldState.DPad.Up),analog);

                case InputDPad.Down:
                    return JoinUpCheck(ButtonChecker(m_NewState.DPad.Down, m_OldState.DPad.Down),analog);

                case InputDPad.Left:
                    return JoinUpCheck(ButtonChecker(m_NewState.DPad.Left, m_OldState.DPad.Left),analog);

                case InputDPad.Right:
                    return JoinUpCheck(ButtonChecker(m_NewState.DPad.Right, m_OldState.DPad.Right), analog);

                default:
                    return InputState.NoMatch;
            }
        }

        private InputState JoinUpCheck(InputState dPad, InputState analog)
        {
            if (m_JoinDpadAndAnalog)
            {
                if ((int)dPad > (int)analog)
                {
                    return dPad;
                }
                else
                {
                    return analog;
                }
            }
            else
            {
                return dPad;
            }
        }

        public Vector2 CheckRightStick()
        {
            if (m_invertRightY)
            {
                Vector2 temp = m_NewState.ThumbSticks.Right;

                temp.Y *= -1;

                return temp;
            }
            else
            {
                return m_NewState.ThumbSticks.Right;
            }
        }

        public Vector2 CheckLeftStick()
        {
            if (m_invertLeftY)
            {
                Vector2 temp = m_NewState.ThumbSticks.Left;

                temp.Y *= -1;

                return temp;
            }
            else
            {
                return m_NewState.ThumbSticks.Left;
            }
        }



        public void Vibrate(float vibrationvalue)
        {
            float vibrationAmount = MathHelper.Clamp(vibrationvalue, 0.0f, 1.0f);
            // this sets both motors to the same amount - it's a trivial task to set up vibration for either motor separately.
            GamePad.SetVibration(m_Player, vibrationAmount, vibrationAmount);
        }

        public float LeftTigger()
        {
            return m_NewState.Triggers.Left;
        }

        public float RightTigger()
        {
            return m_NewState.Triggers.Right;
        }

        public bool IsActive()
        {
            
            return m_OldState.IsConnected;
        }
    }
}
