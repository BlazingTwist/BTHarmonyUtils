using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BTHarmonyUtils.ILUtils;
using HarmonyLib;
using JetBrains.Annotations;

namespace BTHarmonyUtils.InstructionSearch {

	/// <summary>
	/// Used to match Instructions more dynamically
	/// </summary>
	[PublicAPI]
	public class InstructionMask {

		private readonly List<Tuple<OpCode?, object>> allowedInstructions = new List<Tuple<OpCode?, object>>();

		private InstructionMask(IEnumerable<OpCode?> allowedOpCodes, IEnumerable<object> allowedOperands) {
			if (allowedOpCodes != null) {
				foreach (OpCode? allowedOpCode in allowedOpCodes) {
					allowedInstructions.Add(GetMatcherTuple(allowedOpCode, null));
				}
			}
			if (allowedOperands != null) {
				foreach (object allowedOperand in allowedOperands) {
					allowedInstructions.Add(new Tuple<OpCode?, object>(null, allowedOperand));
				}
			}
		}

		private InstructionMask(OpCode? opCode, object operand) {
			allowedInstructions.Add(GetMatcherTuple(opCode, operand));
		}

		private static Tuple<OpCode?, object> GetMatcherTuple(OpCode? opCode, object operand) {
			return opCode == null
					? new Tuple<OpCode?, object>(null, operand)
					: InstructionSimplifier.SimplifyForComparison(opCode.Value, operand);
		}

		/// <summary>
		/// Provides an InstructionMask that matches any instruction.
		/// </summary>
		/// <returns></returns>
		public static InstructionMask MatchAny() {
			return new InstructionMask(null, null);
		}

		/// <summary>
		/// Provides an InstructionMask matching an OpCode.
		/// </summary>
		/// <param name="opCode">opCode that this Mask should match</param>
		/// <returns></returns>
		public static InstructionMask MatchOpCode(OpCode? opCode) {
			return new InstructionMask(new[] { opCode }, null);
		}

		/// <summary>
		/// Provides an InstructionMask matching one or more OpCodes.
		/// </summary>
		/// <param name="allowedOpCodes">a Set of OpCodes that this Mask should match</param>
		/// <returns></returns>
		public static InstructionMask MatchOpCodes(params OpCode?[] allowedOpCodes) {
			return new InstructionMask(allowedOpCodes, null);
		}

		/// <summary>
		/// Provides an InstructionMask matching an operand.
		/// </summary>
		/// <param name="operand">operand that this Mask should match</param>
		/// <returns></returns>
		public static InstructionMask MatchOperand(object operand) {
			return new InstructionMask(null, new[] { operand });
		}

		/// <summary>
		/// Provides an InstructionMask matching one or more operands.
		/// </summary>
		/// <param name="operands">a Set of operands that this Mask should match</param>
		/// <returns></returns>
		public static InstructionMask MatchOperands(params object[] operands) {
			return new InstructionMask(null, operands);
		}

		/// <summary>
		/// Provides an InstructionMask matching the opCode and operand of a CodeInstruction
		/// </summary>
		/// <param name="instruction">CodeInstruction that this Mask should match</param>
		/// <returns></returns>
		public static InstructionMask MatchCodeInstruction(CodeInstruction instruction) {
			return new InstructionMask(instruction?.opcode, instruction?.operand);
		}

		/// <summary>
		/// Provides an InstructionMask matching an opCode and operand
		/// </summary>
		/// <param name="opCode">OpCode this Mask should match</param>
		/// <param name="operand">operand this Mask should match</param>
		/// <returns></returns>
		public static InstructionMask MatchInstruction(OpCode? opCode, object operand) {
			return new InstructionMask(opCode, operand);
		}

		internal bool Matches(CodeInstruction other) {
			Tuple<OpCode?, object> simplifiedOther = InstructionSimplifier.SimplifyForComparison(other);
			return allowedInstructions.Any(allowedInstruction =>
					InstructionUtils.OpCodeMatches(simplifiedOther.Item1, allowedInstruction.Item1)
					&& InstructionUtils.OperandMatches(simplifiedOther.Item2, allowedInstruction.Item2)
			);
		}

	}

}
