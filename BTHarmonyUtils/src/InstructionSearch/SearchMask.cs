using JetBrains.Annotations;

namespace BTHarmonyUtils.InstructionSearch {

	/// <summary>
	/// Used to match for CodeInstructions
	/// </summary>
	[PublicAPI]
	public class SearchMask {

		internal readonly InstructionMask mask;
		internal readonly bool copyToResult;

		/// <summary>
		/// Provides a SearchMask that matches an InstructionMask
		/// </summary>
		/// <param name="mask">the mask to use when matching Instructions</param>
		/// <param name="copyToResult">Copy the instruction matching this SearchMask to the output</param>
		public SearchMask(InstructionMask mask, bool copyToResult = false) {
			this.mask = mask;
			this.copyToResult = copyToResult;
		}

	}

}
