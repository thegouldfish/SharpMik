using SharpMik.Drivers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SharpMik.Player
{
    public class ModStream : Stream
    {
        public Module Module { get; }
        
        private MikMod Player { get; }
        private PullStreamDriver m_PullStream;

        public ModStream(Stream toPlay)
        {
            Player = new MikMod();
            bool result = false;
            m_PullStream = Player.Init<PullStreamDriver>("",out result);

            Module = Player.Play(toPlay);            
        }


        public override bool CanRead
        {
            get
            {
                return m_PullStream.Stream.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return m_PullStream.Stream.CanSeek;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return m_PullStream.Stream.CanWrite;
            }
        }

        public override long Length
        {
            get
            {
                return m_PullStream.Stream.Length;
            }
        }

        public override long Position
        {
            get
            {
                return m_PullStream.Stream.Position;
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
            return m_PullStream.Stream.Read(buffer, offset, count);            
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
}
