using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using BTHarmonyUtils.InstructionSearch;
using BTHarmonyUtils.@internal;
using HarmonyLib;
using JetBrains.Annotations;

namespace BTHarmonyUtils.TranspilerUtils {
	
	/// <summary>
	/// Class that acts as an intermediary between user and transpiler
	/// </summary>
	[PublicAPI]
	public class CodeReplacementPatch {

		private readonly ITranspilerPatch patchImplementation;

		/// <summary>
		/// Create a Patch that replaces a set of Instructions
		/// </summary>
		/// <param name="expectedMatches">Amount of matches that this Patch should encounter, or less than 0 for any amount</param>
		/// <param name="insertInstructionSequence">Sequence of instructions to insert at the matching locations</param>
		/// <param name="prefixInstructionSequence">Sequence of instructions that should occur before the replace-sequence</param>
		/// <param name="targetInstructionSequence">Sequence of instructions that should be removed / replaced with the insert-sequence</param>
		/// <param name="postfixInstructionSequence">Sequence of instructions that should occur after the replace-sequence</param>
		/// <exception cref="InvalidDataException">thrown when no prefix, replace and postfix sequence is specified -> cannot match for anything</exception>
		[Obsolete("This Constructor provides outdated pullUp behaviour for backwards compatibility.\n"
				+ "Instead, use the constructor with the 'pullNextLabelUp' parameter.\n"
				+ "For more information, see https://github.com/BlazingTwist/BTHarmonyUtils/wiki/Transpiler-Patch")]
		public CodeReplacementPatch(
				int expectedMatches = -1,
				IEnumerable<CodeInstruction> insertInstructionSequence = null,
				IEnumerable<CodeInstruction> prefixInstructionSequence = null,
				IEnumerable<CodeInstruction> targetInstructionSequence = null,
				IEnumerable<CodeInstruction> postfixInstructionSequence = null
		) {
			patchImplementation = new InstructionReplacementTranspiler(
					expectedMatches, insertInstructionSequence,
					prefixInstructionSequence?.Select(InstructionMask.MatchCodeInstruction).ToList(),
					targetInstructionSequence?.Select(InstructionMask.MatchCodeInstruction).ToList(),
					postfixInstructionSequence?.Select(InstructionMask.MatchCodeInstruction).ToList(),
					LabelPullUp.PostfixOnly
			);
		}

		/// <summary>
		/// Create a Patch that replaces a set of Instructions
		/// </summary>
		/// <param name="expectedMatches">Amount of matches that this Patch should encounter, or less than 0 for any amount</param>
		/// <param name="insertInstructions">Sequence of instructions to insert at the matching locations</param>
		/// <param name="prefixInstructions">Sequence of instructions that should occur before the replace-sequence</param>
		/// <param name="targetInstructions">Sequence of instructions that should be removed / replaced with the insert-sequence</param>
		/// <param name="postfixInstructions">Sequence of instructions that should occur after the replace-sequence</param>
		/// <param name="pullNextLabelUp">if enabled, the label immediately after the insertSequence will be moved in front of it, otherwise the label remains at the end of the insertSequence</param>
		/// <exception cref="InvalidDataException">thrown when no prefix, replace and postfix sequence is specified -> cannot match for anything</exception>
		public CodeReplacementPatch(
				int expectedMatches = -1,
				IEnumerable<CodeInstruction> insertInstructions = null,
				IEnumerable<CodeInstruction> prefixInstructions = null,
				IEnumerable<CodeInstruction> targetInstructions = null,
				IEnumerable<CodeInstruction> postfixInstructions = null,
				bool pullNextLabelUp = false
		) {
			patchImplementation = new InstructionReplacementTranspiler(
					expectedMatches, insertInstructions,
					prefixInstructions?.Select(InstructionMask.MatchCodeInstruction).ToList(),
					targetInstructions?.Select(InstructionMask.MatchCodeInstruction).ToList(),
					postfixInstructions?.Select(InstructionMask.MatchCodeInstruction).ToList(),
					pullNextLabelUp ? LabelPullUp.ImmediateNext : LabelPullUp.None
			);
		}

		/// <summary>
		/// Create a Patch that replaces a set of Instructions
		/// </summary>
		/// <param name="expectedMatches">Amount of matches that this Patch should encounter, or less than 0 for any amount</param>
		/// <param name="insertInstructions">Sequence of instructions to insert at the matching locations</param>
		/// <param name="prefixInstructions">Sequence of instructions that should occur before the replace-sequence</param>
		/// <param name="targetInstructions">Sequence of instructions that should be removed / replaced with the insert-sequence</param>
		/// <param name="postfixInstructions">Sequence of instructions that should occur after the replace-sequence</param>
		/// <param name="pullNextLabelUp">if enabled, the label immediately after the insertSequence will be moved in front of it, otherwise the label remains at the end of the insertSequence</param>
		/// <exception cref="InvalidDataException">thrown when no prefix, replace and postfix sequence is specified -> cannot match for anything</exception>
		public CodeReplacementPatch(
				int expectedMatches = -1,
				IEnumerable<CodeInstruction> insertInstructions = null,
				IEnumerable<InstructionMask> prefixInstructions = null,
				IEnumerable<InstructionMask> targetInstructions = null,
				IEnumerable<InstructionMask> postfixInstructions = null,
				bool pullNextLabelUp = false
		) {
			patchImplementation = new InstructionReplacementTranspiler(
					expectedMatches, insertInstructions,
					prefixInstructions, targetInstructions, postfixInstructions,
					pullNextLabelUp ? LabelPullUp.ImmediateNext : LabelPullUp.None
			);
		}

		/// <summary>
		/// Apply this ReplacementPatch to the given instructions
		/// </summary>
		/// <param name="instructions">instructions to apply the changes to</param>
		/// <exception cref="InvalidDataException">thrown when matchers find an unexpected amount of matches or the matches are overlapping</exception>
		public void Apply(List<CodeInstruction> instructions) {
			patchImplementation.Apply(instructions);
		}

		/// <summary>
		/// Apply this ReplacementPatch to the given instructions
		/// Catches all exceptions and writes them to the logger
		/// </summary>
		/// <param name="instructions">instructions to apply the changes to</param>
		/// <param name="logger">logger to write exceptions to</param>
		public void ApplySafe(List<CodeInstruction> instructions, ManualLogSource logger) {
			patchImplementation.ApplySafe(instructions, logger);
		}

	}

}
