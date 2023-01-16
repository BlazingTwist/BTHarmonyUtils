using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using BepInEx.Logging;
using BTHarmonyUtils.ILUtils;
using BTHarmonyUtils.InstructionSearch;
using HarmonyLib;

namespace BTHarmonyUtils.@internal {

	internal enum LabelPullUp {

		ImmediateNext,
		PostfixOnly, // used for backwards compatibility when only the first PostFixSequence label was pulled up
		None,

	}

	internal static class LabelPullUpExtensions {

		public static bool CanPullUp(this LabelPullUp pullUp, TranspilerContext context) {
			switch (pullUp) {
				case LabelPullUp.ImmediateNext:
					return context.postfixIndex < context.instructions.Count;
				case LabelPullUp.PostfixOnly:
					return context.postfixLength > 0;
				case LabelPullUp.None:
					return false;
				default:
					throw new ArgumentOutOfRangeException(nameof(pullUp), pullUp, null);
			}
		}

	}

	internal readonly struct TranspilerContext {

		public readonly List<CodeInstruction> instructions;
		public readonly int replaceIndex;
		public readonly int postfixIndex;
		public readonly int insertLength;
		public readonly int replaceLength;
		public readonly int postfixLength;

		public TranspilerContext(
				List<CodeInstruction> instructions,
				int index,
				int insertLength,
				int prefixLength,
				int replaceLength,
				int postfixLength
		) : this() {
			this.instructions = instructions;
			replaceIndex = index + prefixLength;
			postfixIndex = index + prefixLength + replaceLength;
			this.insertLength = insertLength;
			this.replaceLength = replaceLength;
			this.postfixLength = postfixLength;
		}

	}

	internal class InstructionReplacementTranspiler : ITranspilerPatch {

		private readonly int expectedMatches;
		private readonly List<CodeInstruction> insertSequence = new List<CodeInstruction>();
		private readonly List<InstructionMask> prefixSequence = new List<InstructionMask>();
		private readonly List<InstructionMask> targetSequence = new List<InstructionMask>();
		private readonly List<InstructionMask> postfixSequence = new List<InstructionMask>();
		private readonly LabelPullUp pullUp;

		/// <summary>
		/// Replace using basic CodeInstructions
		/// </summary>
		/// <param name="expectedMatches"></param>
		/// <param name="insertSequence"></param>
		/// <param name="prefixSequence"></param>
		/// <param name="targetSequence"></param>
		/// <param name="postfixSequence"></param>
		/// <param name="pullUp"></param>
		/// <exception cref="InvalidDataException">thrown when no prefix, replace and postfix sequence is specified -> cannot match for anything</exception>
		public InstructionReplacementTranspiler(
				int expectedMatches,
				IEnumerable<CodeInstruction> insertSequence,
				IEnumerable<InstructionMask> prefixSequence,
				IEnumerable<InstructionMask> targetSequence,
				IEnumerable<InstructionMask> postfixSequence,
				LabelPullUp pullUp
		) {
			this.expectedMatches = expectedMatches;

			if (insertSequence != null) {
				this.insertSequence.AddRange(insertSequence);
			}

			if (prefixSequence != null) {
				this.prefixSequence.AddRange(prefixSequence);
			}

			if (targetSequence != null) {
				this.targetSequence.AddRange(targetSequence);
			}

			if (postfixSequence != null) {
				this.postfixSequence.AddRange(postfixSequence);
			}

			if (this.prefixSequence.Count == 0 && this.targetSequence.Count == 0 && this.postfixSequence.Count == 0) {
				throw new InvalidDataException("No matchers specified, cannot apply patch!");
			}

			this.pullUp = pullUp;
		}

		public void Apply(List<CodeInstruction> instructions) {
			int insertLength = insertSequence.Count;
			int prefixLength = prefixSequence.Count;
			int replaceLength = targetSequence.Count;
			int postfixLength = postfixSequence.Count;
			List<int> sequenceMatches = null;

			if (prefixLength > 0) {
				sequenceMatches = InstructionUtils.FindInstructionSequence(instructions, prefixSequence);
			}

			if (replaceLength > 0) {
				sequenceMatches = sequenceMatches != null
						? sequenceMatches
								.Where(index => InstructionUtils.SequenceMatches(instructions, targetSequence, index + prefixLength))
								.ToList()
						: InstructionUtils.FindInstructionSequence(instructions, targetSequence);
			}

			if (postfixLength > 0) {
				sequenceMatches = sequenceMatches != null
						? sequenceMatches
								.Where(index => InstructionUtils.SequenceMatches(instructions, postfixSequence, index + prefixLength + replaceLength))
								.ToList()
						: InstructionUtils.FindInstructionSequence(instructions, postfixSequence);
			}

			sequenceMatches = sequenceMatches ?? new List<int>();
			int sequenceMatchesCount = sequenceMatches.Count;

			if (expectedMatches >= 0 && sequenceMatchesCount != expectedMatches) {
				throw new InvalidDataException(
						$"CodeReplacementPatch has found {sequenceMatchesCount} matches, but expected to find {expectedMatches}! Mod may be outdated!");
			}

			// verify that every index is at least `replaceLength` many elements apart -> same code isn't replaced twice
			if (sequenceMatchesCount > 0) {
				int previousIndex = sequenceMatches[0];
				for (int i = 1; i < sequenceMatchesCount; i++) {
					int index = sequenceMatches[i];
					if (index - previousIndex < replaceLength) {
						throw new InvalidDataException("CodeReplacementPatch has matched overlapping replaceSequences! Mod may be outdated!");
					}
					previousIndex = index;
				}
			}

			// iterate over indexes in reverse, this way we don't have to update the indexes after every removal / insertion
			for (int i = sequenceMatchesCount - 1; i >= 0; i--) {
				int index = sequenceMatches[i];
				TranspilerContext context = new TranspilerContext(instructions, index, insertLength, prefixLength, replaceLength, postfixLength);
				List<CodeInstruction> insertSequenceWithLabels = MoveLabels(context);
				instructions.InsertRange(index + prefixLength + replaceLength, insertSequenceWithLabels);
				instructions.RemoveRange(index + prefixLength, replaceLength);
			}
		}

		public void ApplySafe(List<CodeInstruction> instructions, ManualLogSource logger) {
			try {
				Apply(instructions);
			} catch (InvalidDataException e) {
				string calleeName = new StackFrame(2).GetMethod().Name;
				logger.LogError($"Patching {calleeName} caused error: {e.Message}\n{e.StackTrace}");
			}
		}

		private List<CodeInstruction> MoveLabels(TranspilerContext context) {
			List<Label> newFirstInstructionLabels = GatherMoveLabels(context);
			if (newFirstInstructionLabels.Count <= 0) {
				return insertSequence;
			}

			List<CodeInstruction> newInsertSequence = new List<CodeInstruction>();
			if (context.insertLength > 0) {
				CodeInstruction newFirstInstruction = new CodeInstruction(insertSequence[0]);
				newFirstInstruction.labels.AddRange(newFirstInstructionLabels);
				newInsertSequence.Add(newFirstInstruction);
				newInsertSequence.AddRange(insertSequence.GetRange(1, context.insertLength - 1));
			} else if (context.postfixIndex < context.instructions.Count) {
				CodeInstruction postfixInstruction = context.instructions[context.postfixIndex];
				postfixInstruction.labels.AddRange(newFirstInstructionLabels);
			} else {
				CodeInstruction nopInstruction = new CodeInstruction(OpCodes.Nop);
				nopInstruction.labels.AddRange(newFirstInstructionLabels);
				newInsertSequence.Add(nopInstruction);
			}
			return newInsertSequence;
		}

		private List<Label> GatherMoveLabels(TranspilerContext context) {
			List<Label> newFirstInstructionLabels = new List<Label>();

			// if insertSequence is given, compute pullUp labels, otherwise pullUp has no effect
			if (context.insertLength > 0 && pullUp.CanPullUp(context)) {
				CodeInstruction pullUpInstruction = context.instructions[context.postfixIndex];
				newFirstInstructionLabels.AddRange(pullUpInstruction.labels);
				pullUpInstruction.labels.Clear();
			}

			if (context.replaceLength > 0) {
				// if targetSequence is given, its labels must be moved somewhere
				List<Label> targetSequenceLabels = InstructionUtils.FindAllLabels(context.instructions, context.replaceIndex, context.postfixIndex);
				newFirstInstructionLabels.AddRange(targetSequenceLabels);
			}

			return newFirstInstructionLabels;
		}

	}

}
