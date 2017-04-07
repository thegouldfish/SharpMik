using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SharpMik.Drivers
{

    public class PullAudioStream : Stream
    {
        private PullStreamDriver m_StreamDriver;


        public PullAudioStream(PullStreamDriver driver)
        {
            m_StreamDriver = driver;
        }


        public override bool CanRead
        {
            get
            {
                return m_StreamDriver.IsPlaying;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override long Length
        {
            get
            {
                return -1;
            }
        }

        public override long Position
        {
            get
            {
                return 0;
            }

            set
            {
                
            }
        }

        public override void Flush()
        {
            
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return (int)m_StreamDriver.GetData(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return 0;
        }

        public override void SetLength(long value)
        {
            
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            
        }
    }


    public class PullStreamDriver : VirtualSoftwareDriver
    {
        sbyte[] m_TempBuffer;


        public bool IsPlaying { get; set; }

        public Stream Stream { get; set; }



        public PullStreamDriver()
        {
            m_Next = null;
            m_Name = "Pull Audio Stream";
            m_Version = "1";
            m_HardVoiceLimit = 0;
            m_SoftVoiceLimit = 255;
            m_AutoUpdating = true;
            IsPlaying = false;
            Stream = new PullAudioStream(this);

        }


        public override void CommandLine(string command)
        {
            
        }

        public override bool IsPresent()
        {
            return true;
        }


        public override bool PlayStart()
        {
            IsPlaying = true;
            return base.PlayStart();            
        }


        public override void PlayStop()
        {
            base.PlayStop();
            IsPlaying = false;
        }


        public override void Pause()
        {
            base.Pause();
            IsPlaying = !IsPlaying;
        }

        public uint GetData(byte[] buffer, int offset, int count)
        {
            if (m_TempBuffer == null)
            {
                m_TempBuffer = new sbyte[count];
            }
            else if (m_TempBuffer.Length < count)
            {
                Array.Resize(ref m_TempBuffer, count);
            }

            uint done = WriteBytes(m_TempBuffer, (uint)count);
            Array.Copy(m_TempBuffer, buffer, done);
            
            return done;
        }
    }
}
