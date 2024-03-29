﻿using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using BTHarmonyUtils.InstructionSearch;
using BTHarmonyUtils.@internal;
using HarmonyLib;
using JetBrains.Annotations;

namespace BTHarmonyUtils.ILUtils {

	/// <summary>
	/// A Utility class centered around CodeInstructions
	/// </summary>
	[PublicAPI]
	public static class InstructionUtils {

		/// <summary>
		/// Returns all Labels in the specified range
		/// </summary>
		/// <param name="instructions">list of instructions</param>
		/// <param name="startIndex">index of first instruction to check, inclusive</param>
		/// <param name="endIndex">index of last instruction to check, exclusive</param>
		/// <returns>all Labels in the specified range</returns>
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
		/// Checks a list of instructions for occurrences of a sequence
		/// </summary>
		/// <param name="instructions">instructions to check</param>
		/// <param name="sequence">sequence to look for</param>
		/// <returns>the startIndex for every matching sequence</returns>
		public static List<int> FindInstructionSequence(List<CodeInstruction> instructions, List<InstructionMask> sequence) {
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
		/// <returns>true if the instructionSequence at the specified offset matches the matcher-sequence</returns>
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
		/// Checks if a list of instructions matches with the matcher-sequence
		/// </summary>
		/// <param name="instructions">instructions</param>
		/// <param name="sequence">matcher instructions</param>
		/// <param name="offset">offset the instruction list</param>
		/// <returns>true if the instructionSequence at the specified offset matches the matcher-sequence</returns>
		public static bool SequenceMatches(List<CodeInstruction> instructions, List<InstructionMask> sequence, int offset) {
			if (sequence.Count == 0) {
				return true;
			}

			if (instructions.Count < sequence.Count + offset) {
				return false; // not enough instructions to match sequence
			}

			for (int i = 0; i < sequence.Count; i++) {
				CodeInstruction instruction = instructions[i + offset];
				InstructionMask mask = sequence[i];
				if (!mask.Matches(instruction)) {
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Helper overload for `InstructionMatches`
		/// </summary>
		/// <param name="instruction">instruction</param>
		/// <param name="matcherInstruction">matcher instruction</param>
		/// <returns>true if the instruction roughly equals the matcher-instruction</returns>
		public static bool InstructionMatches(CodeInstruction instruction, CodeInstruction matcherInstruction) {
			return InstructionMatches(instruction, InstructionSimplifier.SimplifyForComparison(matcherInstruction));
		}

		/// <summary>
		/// Checks if an instruction matches the matcherInstruction
		///
		/// Allows rough matches (instruction is roughly the same as the matcherInstruction)
		///  e.g. (ldarg.0, null) == (ldarg, 0) == (ldarg.s, 0)
		///  e.g. brfalse.s == brfalse
		/// </summary>
		/// <param name="instruction">instruction</param>
		/// <param name="matcherTuple">matcher instruction</param>
		/// <returns>true if the instruction roughly equals the matcher-instruction</returns>
		public static bool InstructionMatches(CodeInstruction instruction, Tuple<OpCode?, object> matcherTuple) {
			Tuple<OpCode?, object> instructionTuple = InstructionSimplifier.SimplifyForComparison(instruction);
			return OpCodeMatches(instructionTuple.Item1, matcherTuple.Item1)
					&& OperandMatches(instructionTuple.Item2, matcherTuple.Item2);
		}

		/// <summary>
		/// Checks if an OpCode matcher matches an OpCode
		/// </summary>
		/// <param name="opCode">the opCode to match</param>
		/// <param name="matcherOpCode">the opCode to use as a matcher</param>
		/// <returns></returns>
		public static bool OpCodeMatches(OpCode? opCode, OpCode? matcherOpCode) {
			return matcherOpCode == null || opCode == matcherOpCode;
		}

		/// <summary>
		/// Checks if an Operand matcher matches an Operand
		/// </summary>
		/// <param name="operand">the operand to match</param>
		/// <param name="matcherOperand">the operand to use as a matcher</param>
		/// <returns></returns>
		public static bool OperandMatches(object operand, object matcherOperand) {
			if (matcherOperand == null) {
				return true;
			}

			if (operand == null) {
				return false;
			}

			if (operand is LocalBuilder localBuilder) {
				if (matcherOperand is int index) {
					return localBuilder.LocalIndex == index;
				}
				if (matcherOperand is Type type) {
					return localBuilder.LocalType == type;
				}
			}

			Type operandType = operand.GetType();
			Type matcherOperandType = matcherOperand.GetType();

			if (AccessTools.IsNumber(operandType)) {
				if (AccessTools.IsInteger(matcherOperandType)) {
					return Convert.ToInt64(operand) == Convert.ToInt64(matcherOperand);
				}
				if (AccessTools.IsFloatingPoint(matcherOperandType)) {
					return Math.Abs(Convert.ToDouble(operand) - Convert.ToDouble(matcherOperand)) < double.Epsilon;
				}
			}
			return Equals(operand, matcherOperand);
		}

	}

}
