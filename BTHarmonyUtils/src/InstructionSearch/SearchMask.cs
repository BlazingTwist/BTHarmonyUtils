using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;

namespace BTHarmonyUtils.InstructionSearch {

	/// <summary>
	/// Used to match for CodeInstructions
	/// </summary>
	[PublicAPI]
	public class SearchMask {

		/// <summary>
		/// OpCode to look for, `null` matches any.
		/// </summary>
		public readonly OpCode? opCode;

		/// <summary>
		/// Operand to look for, `null` matches any.
		/// </summary>
		public readonly object operand;

		/// <summary>
		/// Allow simplified instruction matching, simplify logic is in <see cref="ILUtils.InstructionSimplifier"/>
		/// </summary>
		public readonly bool allowSimplify;

		/// <summary>
		/// Copy the instruction matching this SearchMask to the output
		/// </summary>
		public readonly bool copyToResult;

		private SearchMask(OpCode? opCode, object operand, bool allowSimplify, bool copyToResult) {
			this.opCode = opCode;
			this.operand = operand;
			this.allowSimplify = allowSimplify;
			this.copyToResult = copyToResult;
		}

		/// <summary>
		/// Provides a SearchMask that matches any instruction
		/// </summary>
		/// <param name="copyToResult">Copy the instruction matching this SearchMask to the output</param>
		/// <returns></returns>
		public static SearchMask MatchAny(bool copyToResult) {
			return new SearchMask(null, null, true, copyToResult);
		}

		/// <summary>
		/// Provides a SearchMask matching an OpCode
		/// </summary>
		/// <param name="opCode">OpCode to look for</param>
		/// <param name="copyToResult">Copy the instruction matching this SearchMask to the output</param>
		/// <param name="allowSimplify">Allow simplified instruction matching, simplify logic is in <see cref="ILUtils.InstructionSimplifier"/></param>
		/// <returns></returns>
		public static SearchMask MatchOpCode(OpCode opCode, bool copyToResult = false, bool allowSimplify = true) {
			return new SearchMask(opCode, null, allowSimplify, copyToResult);
		}

		/// <summary>
		/// Provides a SearchMask matching an operand
		/// </summary>
		/// <param name="operand">Operand to look for</param>
		/// <param name="copyToResult">Copy the instruction matching this SearchMask to the output</param>
		/// <returns></returns>
		public static SearchMask MatchOperand(object operand, bool copyToResult = false) {
			return new SearchMask(null, operand, true, copyToResult);
		}

		/// <summary>
		/// Provides a SearchMask matching the opCode and operand of a CodeInstruction
		/// </summary>
		/// <param name="instruction">CodeInstruction to look for</param>
		/// <param name="copyToResult">Copy the instruction matching this SearchMask to the output</param>
		/// <param name="allowSimplify">Allow simplified instruction matching, simplify logic is in <see cref="ILUtils.InstructionSimplifier"/></param>
		/// <returns></returns>
		public static SearchMask MatchCodeInstruction(CodeInstruction instruction, bool copyToResult = false, bool allowSimplify = true) {
			return new SearchMask(instruction?.opcode, instruction?.operand, allowSimplify, copyToResult);
		}

		/// <summary>
		/// Provides a SearchMask matching an opCode and operand
		/// </summary>
		/// <param name="opCode">OpCode to look for, `null` matches any.</param>
		/// <param name="operand">Operand to look for, `null` matches any.</param>
		/// <param name="copyToResult">Copy the instruction matching this SearchMask to the output</param>
		/// <param name="allowSimplify">Allow simplified instruction matching, simplify logic is in <see cref="ILUtils.InstructionSimplifier"/></param>
		/// <returns></returns>
		public static SearchMask MatchInstruction(OpCode? opCode, object operand, bool copyToResult = false, bool allowSimplify = true) {
			return new SearchMask(opCode, operand, allowSimplify, copyToResult);
		}

	}

}
