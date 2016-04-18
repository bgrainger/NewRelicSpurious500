using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebApplication1
{
	internal class LoggingStream : Stream
	{
		private readonly MemoryStream m_memory;
		private readonly Stream m_base;

		public LoggingStream(Stream baseStream)
		{
			m_base = baseStream;
			m_memory = new MemoryStream();
		}

		public string DumpData()
		{
			return Encoding.UTF8.GetString(m_memory.ToArray());
		}

		public override bool CanWrite => m_base.CanWrite;
		public override long Length => m_base.Length;
		public override bool CanRead => m_base.CanRead;
		public override bool CanSeek => m_base.CanSeek;
		public override void Flush() => m_base.Flush();
		public override int Read(byte[] buffer, int offset, int count) => m_base.Read(buffer, offset, count);

		public override long Position
		{
			get { return m_base.Position; }
			set { throw new NotSupportedException(); }
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		public override void SetLength(long value)
		{
			m_memory.SetLength(value);
			m_base.SetLength(value);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			m_memory.Write(buffer, offset, count);
			m_base.Write(buffer, offset, count);
		}

		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			m_memory.Write(buffer, offset, count);
			return base.WriteAsync(buffer, offset, count, cancellationToken);
		}

		public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			m_memory.Write(buffer, offset, count);
			return base.BeginWrite(buffer, offset, count, callback, state);
		}

		public override void WriteByte(byte value)
		{
			m_memory.WriteByte(value);
			base.WriteByte(value);
		}
	}
}