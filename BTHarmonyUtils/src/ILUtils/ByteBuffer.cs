using System;

namespace BTHarmonyUtils {
	public class ByteBuffer {
		private readonly byte[] buffer;

		public int Position { get; private set; }

		public ByteBuffer(byte[] buffer) {
			this.buffer = buffer;
		}

		public bool CanRead() {
			return Position < buffer.Length;
		}

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

		public short ReadInt16() {
			CheckCanRead(2);
			short value = (short) (buffer[Position] | (buffer[Position + 1] << 8));
			Position += 2;
			return value;
		}

		public int ReadInt32() {
			CheckCanRead(4);
			int value = buffer[Position]
					| (buffer[Position + 1] << 8)
					| (buffer[Position + 2] << 16)
					| (buffer[Position + 3] << 24);
			Position += 4;
			return value;
		}

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