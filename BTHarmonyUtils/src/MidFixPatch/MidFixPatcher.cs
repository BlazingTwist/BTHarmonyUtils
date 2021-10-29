using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using BTHarmonyUtils.TranspilerUtils;
using HarmonyLib;
using JetBrains.Annotations;

namespace BTHarmonyUtils.MidFixPatch {
	/// <summary>
	/// Applies the MidFix patches.
	/// </summary>
	public static class MidFixPatcher {
		private static readonly ManualLogSource logger = Logger.CreateLogSource($"BTHarmonyUtils:{nameof(MidFixPatcher)}");

		private class MidFixPatch {
			public readonly MethodInfo midFixMethod;
			public readonly MidFixInstructionMatcher instructionMatcher;
			public readonly MethodBase patchTargetMethod;
			public readonly int priority;

			public MidFixPatch(MethodInfo midFixMethod, MethodInfo instructionMatcherMethod, MethodBase patchTargetMethod, int priority) {
				this.midFixMethod = midFixMethod;
				instructionMatcher = instructionMatcherMethod.Invoke(null, null) as MidFixInstructionMatcher;
				this.patchTargetMethod = patchTargetMethod;
				this.priority = priority;
			}
		}

		private static readonly List<MidFixPatch> midFixPatches = new List<MidFixPatch>();

		/// <summary>
		/// Applies a MidFix-Patch using the AnnotationInfo
		/// This will probably never be used externally, but if you - for some reason - do, here it is.
		/// </summary>
		public static void DoPatch(Harmony harmony, HarmonyMethod info, MethodInfo patcherMethod, BTHarmonyMidFix midFixAttribute) {
			if (!patcherMethod.IsStatic) {
				logger.LogError($"MidFix patch method '{patcherMethod.FullDescription()}' must be static");
				return;
			}
			
			MethodBase resolvedMethod = PatcherUtils.ResolveHarmonyMethod(info, patcherMethod.Name);
			MethodInfo instructionMatcherMethod = midFixAttribute.ResolveInstructionMatcherMethod(patcherMethod);
			if (instructionMatcherMethod.ReturnType == typeof(MidFixInstructionMatcher)) {
				logger.LogError("InstructionMatcherMethod for patch " + patcherMethod.FullDescription() + " does not return type " + nameof(MidFixInstructionMatcher));
				return;
			}
			if (!instructionMatcherMethod.IsStatic || instructionMatcherMethod.GetParameters().Any()) {
				logger.LogError("InstructionMatcherMethod for patch " + patcherMethod.FullDescription() + " may not take ANY parameters");
				return;
			}
			midFixPatches.Add(new MidFixPatch(patcherMethod, instructionMatcherMethod, resolvedMethod, midFixAttribute.priority));
			midFixPatches.Sort((a, b) => b.priority.CompareTo(a.priority)); // in-place descending sort
			harmony.Patch(original: resolvedMethod, transpiler: new HarmonyMethod(SymbolExtensions.GetMethodInfo(() => MidFixTranspiler(null, null, null))));
		}

		[UsedImplicitly]
		private static IEnumerable<CodeInstruction> MidFixTranspiler(IEnumerable<CodeInstruction> codeInstructions, MethodBase patchTargetMethod, ILGenerator generator) {
			List<MidFixPatch> relevantPatches = midFixPatches.Where(patch => patch.patchTargetMethod == patchTargetMethod).ToList();
			if (relevantPatches.Count == 0) {
				logger.LogError("MidFix transpiler called, but no MidFix patches were registered for method " + patchTargetMethod.FullDescription());
				return codeInstructions;
			}

			List<CodeInstruction> instructions = codeInstructions.ToList();
			foreach (MidFixPatch midFixPatch in relevantPatches) {
				midFixPatch.instructionMatcher.ApplySafe(instructions, midFixPatch.midFixMethod, patchTargetMethod as MethodInfo, generator, logger);
			}
			return instructions;
		}
	}
}