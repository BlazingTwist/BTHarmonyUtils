using System;

namespace BTHarmonyUtils.ILUtils {
	/// <summary>
	/// Want to read values from an array of Bytes?
	/// Use this thing.
	/// </summary>
	public class ByteBuffer {
		private readonly byte[] buffer;

		/// <summary>
		/// Current index on the buffer
		/// </summary>
		public int Position { get; private set; }

		/// <summary>
		/// Create a ByteBuffer for a byte[]
		/// </summary>
		/// <param name="buffer">the byte[]</param>
		public ByteBuffer(byte[] buffer) {
			this.buffer = buffer;
		}

		/// <summary>
		/// Returns true if there are still bytes to be read from the buffer
		/// </summary>
		/// <returns>true if there are still bytes to be read from the buffer</returns>
		public bool CanRead() {
			return Position < buffer.Length;
		}

		/// <summary>
		/// Read and return a single byte
		/// </summary>
		/// <returns>the next byte from the buffer</returns>
		public byte ReadByte() {
			CheckCanRead(1);
			return buffer[Position++];
		}

		private byte[] ReadBytes(int length) {
			CheckCanRead(length);
			byte[] value = new byte[length];
			Buffer.BlockCopy(buffer, Position, value, 0, length);
			Position += length;
			return value;
		}

		/// <summary>
		/// Read a 16bit Integer (2 bytes)
		/// </summary>
		/// <returns>16bit Integer from the buffer</returns>
		public short ReadInt16() {
			CheckCanRead(2);
			short value = (short) (buffer[Position] | (buffer[Position + 1] << 8));
			Position += 2;
			return value;
		}

		/// <summary>
		/// Read a 32bit Integer (4 bytes)
		/// </summary>
		/// <returns>32bit Integer from the buffer</returns>
		public int ReadInt32() {
			CheckCanRead(4);
			int value = buffer[Position]
					| (buffer[Position + 1] << 8)
					| (buffer[Position + 2] << 16)
					| (buffer[Position + 3] << 24);
			Position += 4;
			return value;
		}

		/// <summary>
		/// Read a 64bit Integer (8 bytes)
		/// </summary>
		/// <returns>64bit Integer from the buffer</returns>
		public long ReadInt64() {
			CheckCanRead(8);
			uint low = (uint) (
					buffer[Position]
					| (buffer[Position + 1] << 8)
					| (buffer[Position + 2] << 16)
					| (buffer[Position + 3] << 24)
			);

			uint high = (uint) (
					buffer[Position + 4]
					| (buffer[Position + 5] << 8)
					| (buffer[Position + 6] << 16)
					| (buffer[Position + 7] << 24)
			);

			long value = (((long) high) << 32) | low;
			Position += 8;
			return value;
		}

		/// <summary>
		/// Read a Float from the buffer (4 bytes)
		/// </summary>
		/// <returns>float from the buffer</returns>
		public float ReadSingle() {
			if (!BitConverter.IsLittleEndian) {
				byte[] bytes = ReadBytes(4);
				Array.Reverse(bytes);
				return BitConverter.ToSingle(bytes, 0);
			}

			float value = BitConverter.ToSingle(buffer, Position);
			Position += 4;
			return value;
		}

		/// <summary>
		/// Read a Double from the buffer (8 bytes)
		/// </summary>
		/// <returns>double from the buffer</returns>
		public double ReadDouble() {
			if (!BitConverter.IsLittleEndian) {
				byte[] bytes = ReadBytes(8);
				Array.Reverse(bytes);
				return BitConverter.ToDouble(bytes, 0);
			}

			double value = BitConverter.ToDouble(buffer, Position);
			Position += 8;
			return value;
		}

		// ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
		private void CheckCanRead(int count) {
			if (Position + count > buffer.Length) {
				throw new ArgumentOutOfRangeException();
			}
		}
	}
}