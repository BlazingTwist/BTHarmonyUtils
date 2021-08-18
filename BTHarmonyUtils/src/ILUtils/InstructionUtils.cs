﻿using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace BTHarmonyUtils {
	public class InstructionUtils {
		/// <summary>
		/// Returns all Labels in the specified range
		/// </summary>
		/// <param name="instructions">list of instructions</param>
		/// <param name="startIndex">index of first instruction to check, inclusive</param>
		/// <param name="endIndex">index of last instruction to check, exclusive</param>
		/// <returns></returns>
		public static List<Label> FindAllLabels(List<CodeInstruction> instructions, int startIndex, int endIndex) {
			List<Label> result = new List<Label>();
			for (int i = startIndex; i < endIndex; i++) {
				result.AddRange(instructions[i].labels);
			}
			return result;
		}

		/// <summary>
		/// Checks a list of instructions for occurrences of a sequence
		/// </summary>
		/// <param name="instructions">instructions to check</param>
		/// <param name="sequence">sequence to look for</param>
		/// <returns>the startIndex for every matching sequence</returns>
		public static List<int> FindInstructionSequence(List<CodeInstruction> instructions, List<CodeInstruction> sequence) {
			List<int> result = new List<int>();

			for (int i = 0; i < instructions.Count; i++) {
				if (SequenceMatches(instructions, sequence, i)) {
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
		public static bool SequenceMatches(List<CodeInstruction> instructions, List<CodeInstruction> sequence, int offset) {
			if (sequence.Count == 0) {
				return true;
			}

			if (instructions.Count < sequence.Count + offset) {
				return false; // not enough instructions to match sequence
			}

			for (int i = 0; i < sequence.Count; i++) {
				CodeInstruction a = instructions[i + offset];
				CodeInstruction b = sequence[i];
				if (!InstructionMatches(a, b)) {
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Checks if an instruction matches the matcherInstruction
		///
		/// Allows rough matches (instruction is roughly the same as the matcherInstruction)
		///  e.g. (ldarg.0, null) == (ldarg, 0) == (ldarg.s, 0)
		///  e.g. brfalse.s == brfalse
		/// </summary>
		/// <param name="instruction">instruction</param>
		/// <param name="matcherInstruction">matcher instruction</param>
		/// <returns></returns>
		public static bool InstructionMatches(CodeInstruction instruction, CodeInstruction matcherInstruction) {
			Tuple<OpCode, object> tuple = InstructionSimplifier.SimplifyForComparison(instruction);
			Tuple<OpCode, object> matcherTuple = InstructionSimplifier.SimplifyForComparison(matcherInstruction);

			if (tuple.Item1 != matcherTuple.Item1) {
				return false;
			}

			if (matcherTuple.Item2 == null) {
				return true;
			}

			if (tuple.Item2 == null) {
				return false;
			}

			if (tuple.Item2 is LocalBuilder localBuilder) {
				if (matcherTuple.Item2 is int index) {
					return localBuilder.LocalIndex == index;
				}
				if (matcherTuple.Item2 is Type type) {
					return localBuilder.LocalType == type;
				}
			}

			Type operandType = tuple.Item2.GetType();
			Type matcherOperandType = matcherTuple.Item2.GetType();

			if (AccessTools.IsNumber(operandType)) {
				if (AccessTools.IsInteger(matcherOperandType)) {
					return Convert.ToInt64(tuple.Item2) == Convert.ToInt64(matcherTuple.Item2);
				}
				if (AccessTools.IsFloatingPoint(matcherOperandType)) {
					return Math.Abs(Convert.ToDouble(tuple.Item2) - Convert.ToDouble(matcherTuple.Item2)) < double.Epsilon;
				}
			}
			return Equals(tuple.Item2, matcherTuple.Item2);
		}
	}
}