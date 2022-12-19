using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;

namespace BTHarmonyUtils.@internal {

	/// <summary>
	/// Because the CodeReplacementPatch constructor is in use by published mods,
	///  this internal interface is used to extend the feature set without affecting the external API.
	/// </summary>
	internal interface ITranspilerPatch {

		/// <summary>
		/// Apply this Patch to the given Instructions
		/// </summary>
		/// <param name="instructions"></param>
		void Apply(List<CodeInstruction> instructions);

		/// <summary>
		/// Apply this ReplacementPatch to the given instructions
		/// Catches all exceptions and writes them to the logger
		/// </summary>
		/// <param name="instructions">instructions to apply the changes to</param>
		/// <param name="logger">logger to write exceptions to</param>
		void ApplySafe(List<CodeInstruction> instructions, ManualLogSource logger);

	}

}
