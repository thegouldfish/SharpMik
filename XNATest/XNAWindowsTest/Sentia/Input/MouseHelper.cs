using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;

namespace Sentia.Input
{

    public enum InputMouseButtons
    {
        Left,
        Right,
        Middle
    }

    public class MouseHelper
    {
        MouseState m_NewState;
        MouseState m_OldState;

        bool m_HasAction = false;

        Vector2 m_MousePosition = new Vector2();

        Vector2 m_Remap;
        bool m_Remapped = false;


        bool m_Active = true;

        bool m_TrapInput = false;
        Vector2 m_TrapRange;


        bool m_TimeOut = false;
        float m_TimeOutValue = 0.0f;
        float m_timer = 0.0f;


        public MouseHelper()
        {
            m_NewState = new MouseState();
            m_OldState = new MouseState();

            m_timer = 0.0f;
        }


        public void Remap(Vector2 remap)
        {
            m_Remapped = true;
            m_Remap = remap;
        }


        public void SetMouseTrap(Vector2 Range, bool active)
        {
            if (active)
            {
                m_TrapRange = Range;
                m_TrapInput = true;
            }
            else
            {
                m_TrapInput = false;
            }
        }


        Vector2 GetRemappedMouse()
        {
            Vector2 pos = new Vector2(m_NewState.X, m_NewState.Y);


            if (m_Remapped)
            {
                pos = pos * m_Remap;
            }

            return pos;
        }



        public void Update(float frameTimer)
        {
            m_OldState = m_NewState;

            if (m_TrapInput)
            {
                Vector2 pos = new Vector2(m_NewState.X, m_NewState.Y);

                if (pos.X > m_TrapRange.X)
                    pos.X = (m_TrapRange.X - 0.01f);

                if (pos.X < 0.0f)
                    pos.X = 0.1f;

                if (pos.Y > m_TrapRange.Y)
                    pos.Y = m_TrapRange.Y - 0.01f;

                if (pos.Y < 0.0f)
                    pos.Y = 0.1f;


                Mouse.SetPosition((int)pos.X, (int)pos.Y);

                m_NewState = Mouse.GetState();
            }
            else
            {
                m_NewState = Microsoft.Xna.Framework.Input.Mouse.GetState();
            }
            checkAction();


            if (m_TimeOut)
            {
                if (!m_HasAction)
                {
                    m_timer += frameTimer;

                    if (m_timer > m_TimeOutValue)
                    {
                        m_Active = false;
                    }
                }
                else
                {
                    m_timer = 0.0f;
                    m_Active = true;
                }
            }


            m_MousePosition.X = m_NewState.X;
            m_MousePosition.Y = m_NewState.Y;
        }

        private void checkAction()
        {
            m_HasAction = false;

            if (m_NewState.X < 0 || m_NewState.Y < 0)
            {
                m_HasAction = false;
                return;
            }

            m_HasAction |= (m_OldState.X != m_NewState.X);
            m_HasAction |= (m_OldState.Y != m_NewState.Y);
            m_HasAction |= (m_OldState.LeftButton != m_NewState.LeftButton);
            m_HasAction |= (m_OldState.RightButton != m_NewState.RightButton);
            m_HasAction |= (m_OldState.MiddleButton != m_NewState.MiddleButton);
            m_HasAction |= (m_OldState.ScrollWheelValue != m_NewState.ScrollWheelValue);
        }


        public void SetTimeOut(float _Time)
        {
            m_TimeOut = true;
            m_timer = 0.0f;
            m_TimeOutValue = _Time;
        }

        public bool HasAction()
        {
            return m_HasAction;
        }

        public bool IsActive()
        {
            return m_Active;
        }

        public bool AnyButtonReleased()
        {
            return CheckButton(InputMouseButtons.Left) == InputState.Released ||
                CheckButton(InputMouseButtons.Right) == InputState.Released ||
                CheckButton(InputMouseButtons.Middle) == InputState.Released;
        }

        public int GetX()
        {
            return (int)GetRemappedMouse().X;
        }

        public Vector2 GetPosition()
        {
            return GetRemappedMouse();
        }


        public int GetY()
        {
            return (int)GetRemappedMouse().Y;
        }

        private InputState ButtonChecker(ButtonState newState, ButtonState oldState)
        {
            // we get passed the button states from our old and new pad states for a specific button, this allows us to only write this code once, rather than once for each button we want to check like this!
            InputState returnValue;

            if (newState == ButtonState.Pressed)
            {
                if (oldState == ButtonState.Pressed)
                    returnValue = InputState.Held;
                else
                    returnValue = InputState.Pressed;
            }
            else if (oldState == ButtonState.Pressed)
                returnValue = InputState.Released;
            else
                returnValue = InputState.Up;

            return returnValue;
        }

        public InputState CheckButton(InputMouseButtons button)
        {
            switch (button)
            {
                case InputMouseButtons.Left: return ButtonChecker(m_NewState.LeftButton, m_OldState.LeftButton);
                case InputMouseButtons.Right: return ButtonChecker(m_NewState.RightButton, m_OldState.RightButton);
                case InputMouseButtons.Middle: return ButtonChecker(m_NewState.MiddleButton, m_OldState.MiddleButton);

                default:
                    return InputState.NoMatch;
            }
        }

        public int ScrollWheelChange()
        {
            return m_NewState.ScrollWheelValue - m_OldState.ScrollWheelValue;
        }
    }
}
