using System.Collections.Generic;
using HarmonyLib;

namespace BTHarmonyUtils {
	public class InstructionUtils {
		/// <summary>
		/// Checks a list of instructions for occurrences of a sequence
		/// </summary>
		/// <param name="instructions">instructions to check</param>
		/// <param name="sequence">sequence to look for</param>
		/// <returns>the startIndex for every matching sequence</returns>
		public static List<int> FindInstructionSequence(List<CodeInstruction> instructions, List<CodeInstruction> sequence)
		{
			List<int> result = new List<int>();

			for (int i = 0; i < instructions.Count; i++)
			{
				if (SequenceMatches(instructions, sequence, i))
				{
					result.Add(i);
				}
			}

			return result;
		}

		/// <summary>
		/// Checks if a list of instructions matches with the matcher-sequence
		/// </summary>
		/// <param name="instructions">instructions</param>
		/// <param name="sequence">matcher instructions</param>
		/// <param name="offset">offset the instruction list</param>
		/// <returns></returns>
		public static bool SequenceMatches(List<CodeInstruction> instructions, List<CodeInstruction> sequence, int offset)
		{
			if (sequence.Count == 0)
			{
				return true;
			}
			
			if (instructions.Count < sequence.Count + offset)
			{
				return false; // not enough instructions to match sequence
			}

			for (int i = 0; i < sequence.Count; i++)
			{
				CodeInstruction a = instructions[i + offset];
				CodeInstruction b = sequence[i];
				if (!InstructionMatches(a, b))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Checks if an instruction matches the matcherInstruction
		/// </summary>
		/// <param name="instruction">instruction</param>
		/// <param name="matcherInstruction">matcher instruction</param>
		/// <returns></returns>
		public static bool InstructionMatches(CodeInstruction instruction, CodeInstruction matcherInstruction)
		{
			if (instruction.opcode != matcherInstruction.opcode)
			{
				return false;
			}

			return matcherInstruction.operand == null || instruction.OperandIs(matcherInstruction.operand);
		}
	}
}