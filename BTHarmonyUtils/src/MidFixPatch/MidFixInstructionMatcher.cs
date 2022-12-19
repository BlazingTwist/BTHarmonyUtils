using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using BTHarmonyUtils.ILUtils;
using BTHarmonyUtils.InstructionSearch;
using BTHarmonyUtils.@internal;
using HarmonyLib;
using JetBrains.Annotations;

namespace BTHarmonyUtils.MidFixPatch {

	/// <summary>
	/// A class that matches for CodeInstructions in methods
	/// </summary>
	[PublicAPI]
	public class MidFixInstructionMatcher {

		private readonly int expectedMatches;
		private readonly List<InstructionMask> prefixSequence = new List<InstructionMask>();
		private readonly List<InstructionMask> postfixSequence = new List<InstructionMask>();

		/// <summary>
		/// A construct for matching CodeInstructions in methods
		/// </summary>
		/// <param name="expectedMatches">Amount of matches that this Patch should encounter, or less than 0 for any amount</param>
		/// <param name="prefixInstructionSequence">Sequence of instructions that should occur before the MidFix</param>
		/// <param name="postfixInstructionSequence">Sequence of instructions that should occur after the MidFix</param>
		/// <exception cref="InvalidDataException">thrown when no prefix and postfix sequence is specified -> cannot match for anything</exception>
		public MidFixInstructionMatcher(
				int expectedMatches = -1,
				IEnumerable<CodeInstruction> prefixInstructionSequence = null,
				IEnumerable<CodeInstruction> postfixInstructionSequence = null
		) {
			this.expectedMatches = expectedMatches;
			if (prefixInstructionSequence != null) {
				prefixSequence.AddRange(prefixInstructionSequence.Select(InstructionMask.MatchCodeInstruction));
			}
			if (postfixInstructionSequence != null) {
				postfixSequence.AddRange(postfixInstructionSequence.Select(InstructionMask.MatchCodeInstruction));
			}
			if (prefixSequence.Count == 0 && postfixSequence.Count == 0) {
				throw new InvalidDataException("No matchers specified, cannot apply patch!");
			}
		}

		/// <summary>
		/// A construct for matching CodeInstructions in methods
		/// </summary>
		/// <param name="expectedMatches">Amount of matches that this Patch should encounter, or less than 0 for any amount</param>
		/// <param name="prefixInstructionSequence">Sequence of instructions that should occur before the MidFix</param>
		/// <param name="postfixInstructionSequence">Sequence of instructions that should occur after the MidFix</param>
		/// <exception cref="InvalidDataException">thrown when no prefix and postfix sequence is specified -> cannot match for anything</exception>
		public MidFixInstructionMatcher(
				int expectedMatches = -1,
				IEnumerable<InstructionMask> prefixInstructionSequence = null,
				IEnumerable<InstructionMask> postfixInstructionSequence = null
		) {
			this.expectedMatches = expectedMatches;
			if (prefixInstructionSequence != null) {
				prefixSequence.AddRange(prefixInstructionSequence);
			}
			if (postfixInstructionSequence != null) {
				postfixSequence.AddRange(postfixInstructionSequence);
			}
			if (prefixSequence.Count == 0 && postfixSequence.Count == 0) {
				throw new InvalidDataException("No matchers specified, cannot apply patch!");
			}
		}

		/// <summary>
		/// Move the labels in the replace-sequence and first label of postfix-sequence to beginning of insert-sequence
		/// </summary>
		/// <returns>a new insert-sequence with adjusted labels</returns>
		private List<CodeInstruction> MoveLabels(
				List<CodeInstruction> instructions,
				int index,
				List<CodeInstruction> insertSequence,
				int insertLength,
				int prefixLength,
				int postfixLength
		) {
			List<Label> postFixLabels = new List<Label>();
			if (postfixLength > 0) {
				CodeInstruction firstPostfixInstruction = instructions[index + prefixLength];
				postFixLabels.AddRange(firstPostfixInstruction.labels);
				firstPostfixInstruction.labels.Clear();
			}

			CodeInstruction newFirstInstruction = new CodeInstruction(insertSequence[0]);
			newFirstInstruction.labels.AddRange(postFixLabels);
			List<CodeInstruction> newInsertSequence = new List<CodeInstruction> { newFirstInstruction };
			newInsertSequence.AddRange(insertSequence.GetRange(1, insertLength - 1));
			return newInsertSequence;
		}

		/// <summary>
		/// Apply this MidFixPatch to the given instructions
		/// </summary>
		/// <param name="instructions">instructions to apply the changes to</param>
		/// <param name="midFixPatch">method to call as a midFix</param>
		/// <param name="originalMethod">method that is being patched</param>
		/// <param name="generator">ilGenerator from harmony</param>
		/// <exception cref="InvalidDataException">thrown when matchers find an unexpected amount of matches or the matches are overlapping</exception>
		public void Apply(List<CodeInstruction> instructions, MethodInfo midFixPatch, MethodInfo originalMethod, ILGenerator generator) {
			List<CodeInstruction> insertSequence = MidFixCodeGenerator.GetCodeInstructions(midFixPatch, originalMethod, generator);
			if (insertSequence == null) {
				return;
			}
			int insertSequenceLength = insertSequence.Count;

			int prefixLength = prefixSequence.Count;
			int postfixLength = postfixSequence.Count;
			List<int> sequenceMatches = null;

			if (prefixLength > 0) {
				sequenceMatches = InstructionUtils.FindInstructionSequence(instructions, prefixSequence);
			}

			if (postfixLength > 0) {
				sequenceMatches = sequenceMatches != null
						? sequenceMatches
								.Where(index => InstructionUtils.SequenceMatches(instructions, postfixSequence, index + prefixLength))
								.ToList()
						: InstructionUtils.FindInstructionSequence(instructions, postfixSequence);
			}

			sequenceMatches = sequenceMatches ?? new List<int>();
			int sequenceMatchesCount = sequenceMatches.Count;

			if (expectedMatches >= 0 && sequenceMatchesCount != expectedMatches) {
				throw new InvalidDataException(
						$"MidFixInstructionMatcher has found {sequenceMatchesCount} matches, but expected to find {expectedMatches}! Mod may be outdated!");
			}

			// iterate over indexes in reverse, this way we don't have to update the indexes after every insertion
			for (int i = sequenceMatchesCount - 1; i >= 0; i--) {
				int index = sequenceMatches[i];
				List<CodeInstruction> insertSequenceWithLabels =
						MoveLabels(instructions, index, insertSequence, insertSequenceLength, prefixLength, postfixLength);
				instructions.InsertRange(index + prefixLength, insertSequenceWithLabels);
			}
		}

		/// <summary>
		/// Apply this MidFixPatch to the given instructions
		/// Catches all exceptions and writes them to the logger
		/// </summary>
		/// <param name="instructions">instructions to apply the changes to</param>
		/// <param name="midFixPatch">method to call as a midFix</param>
		/// <param name="originalMethod">method that is being patched</param>
		/// <param name="generator">ilGenerator from harmony</param>
		/// <param name="logger">logger to write exceptions to</param>
		public void ApplySafe(
				List<CodeInstruction> instructions,
				MethodInfo midFixPatch,
				MethodInfo originalMethod,
				ILGenerator generator,
				ManualLogSource logger
		) {
			try {
				Apply(instructions, midFixPatch, originalMethod, generator);
			} catch (InvalidDataException e) {
				string calleeName = new StackFrame(1).GetMethod().Name;
				logger.LogError($"Patching {calleeName} caused error: {e.Message}\n{e.StackTrace}");
			}
		}

	}

}
