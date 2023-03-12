using System.Collections.Generic;

namespace BTHarmonyUtils.@internal {

	/// <summary>
	/// Quick and dirty Tuple, because it's missing in .Net 3.5
	/// </summary>
	/// <typeparam name="T1"></typeparam>
	/// <typeparam name="T2"></typeparam>
	public class Tuple<T1, T2> {

		/// <summary>
		/// Item1.
		/// </summary>
		public readonly T1 Item1;

		/// <summary>
		/// Item2.
		/// </summary>
		public readonly T2 Item2;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="a">Item 1</param>
		/// <param name="b">Item 2</param>
		public Tuple(T1 a, T2 b) {
			Item1 = a;
			Item2 = b;
		}

		private bool Equals(Tuple<T1, T2> other) {
			return EqualityComparer<T1>.Default.Equals(Item1, other.Item1) && EqualityComparer<T2>.Default.Equals(Item2, other.Item2);
		}

		/// <summary>
		/// Equals.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj) {
			if (ReferenceEquals(null, obj)) {
				return false;
			}
			if (ReferenceEquals(this, obj)) {
				return true;
			}
			return obj.GetType() == GetType() && Equals((Tuple<T1, T2>) obj);
		}

		/// <summary>
		/// HashCode.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() {
			unchecked {
				return (EqualityComparer<T1>.Default.GetHashCode(Item1) * 397) ^ EqualityComparer<T2>.Default.GetHashCode(Item2);
			}
		}

	}

}
