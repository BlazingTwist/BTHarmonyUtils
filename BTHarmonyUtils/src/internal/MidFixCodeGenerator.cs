using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using BepInEx.Logging;
using HarmonyLib;

namespace BTHarmonyUtils.@internal {

	/// <summary>
	/// Class for generating IL-Code for MidFixes
	/// </summary>
	public static class MidFixCodeGenerator {

		private static readonly ManualLogSource logger = Logger.CreateLogSource($"BTHarmonyUtils:{nameof(MidFixCodeGenerator)}");

		/// <summary>
		/// Returns the codeInstructions for calling a midFix pattern
		/// </summary>
		/// <param name="midFixPatch">method to call</param>
		/// /// <param name="originalMethod">method that is being patched</param>
		/// /// <param name="generator">ilGenerator from harmony</param>
		/// <returns>codeInstructions</returns>
		public static List<CodeInstruction> GetCodeInstructions(MethodInfo midFixPatch, MethodInfo originalMethod, ILGenerator generator) {
			List<CodeInstruction> result = new List<CodeInstruction>();

			Type originalClassType = originalMethod.DeclaringType;
			Type originalReturnType = originalMethod.ReturnType;

			Dictionary<string, ParameterInfo> originalParamDict = originalMethod.GetParameters().ToDictionary(param => param.Name, param => param);

			LocalBuilder resultLocal = null;

			foreach (ParameterInfo parameter in midFixPatch.GetParameters()) {
				string parameterName = parameter.Name;
				Type parameterType = parameter.ParameterType;
				Type parameterElementType = parameterType.IsByRef ? parameterType.GetElementType() : parameterType;
				Debug.Assert(parameterElementType != null, nameof(parameterElementType) + " != null (internal error)");

				if (parameterName == "__instance") {
					if (!parameterElementType.IsAssignableFrom(originalClassType)) {
						LogErrorTypeMismatch(parameterName, midFixPatch, parameterElementType, originalClassType);
						return null;
					}
					if (parameterType.IsByRef) {
						logger.LogError($"MidFix patch '{midFixPatch.FullDescription()}' parameter '__instance' may not be by ref!");
						return null;
					}
					if (originalMethod.IsStatic) {
						logger.LogError($"MidFix patch '{midFixPatch.FullDescription()}' cannot receive '__instance' parameter,"
								+ $" because patched method '{originalMethod.FullDescription()}' is static");
						return null;
					}
					result.Add(new CodeInstruction(OpCodes.Ldarg_0));
				} else if (parameterName == "__result") {
					if (originalReturnType == typeof(void)) {
						logger.LogError($"MidFix patch '{midFixPatch.FullDescription()}' cannot receive '__result' parameter,"
								+ $" because patched method '{originalMethod.FullDescription()}' has no return value (void)");
						return null;
					}
					if (originalReturnType.IsByRef) {
						logger.LogError($"MidFix patch '{midFixPatch.FullDescription()}'"
								+ $" may not alter the result of a method returning a by-ref result ({originalMethod.FullDescription()})");
						return null;
					}
					if (!parameterType.IsByRef) {
						logger.LogError($"MidFix patch '{midFixPatch.FullDescription()}'"
								+ " must declare the '__result' parameter with the 'ref' or 'out' keyword");
						return null;
					}
					if (!parameterElementType.IsAssignableFrom(originalReturnType)) {
						LogErrorTypeMismatch(parameterName, midFixPatch, parameterElementType, originalReturnType);
						return null;
					}
					resultLocal = generator.DeclareLocal(originalReturnType);
					result.Add(new CodeInstruction(OpCodes.Ldloca_S, resultLocal));
				} else if (parameterName == "__originalMethod") {
					result.Add(new CodeInstruction(OpCodes.Ldtoken, originalMethod));
				} else if (parameterName.StartsWith("___")) {
					string fieldName = parameterName.Substring("___".Length);
					FieldInfo fieldInfo;
					if (fieldName.All(char.IsDigit)) {
						fieldInfo = AccessTools.DeclaredField(originalClassType, int.Parse(fieldName));
						if (fieldInfo == null) {
							logger.LogError($"MidFix patch '{midFixPatch.FullDescription()}'"
									+ $" failed, did not find field at index '{fieldName}' in class '{originalClassType.FullDescription()}'");
							return null;
						}
					} else {
						fieldInfo = AccessTools.Field(originalClassType, fieldName);
						if (fieldInfo == null) {
							logger.LogError($"MidFix patch '{midFixPatch.FullDescription()}'"
									+ $" failed, did not find field with name '{fieldName}' in class '{originalClassType.FullDescription()}'");
							return null;
						}
					}
					if (!parameterElementType.IsAssignableFrom(fieldInfo.FieldType)) {
						LogErrorTypeMismatch(parameterName, midFixPatch, parameterElementType, fieldInfo.FieldType);
						return null;
					}
					if (fieldInfo.IsStatic) {
						result.Add(new CodeInstruction(parameterType.IsByRef ? OpCodes.Ldsflda : OpCodes.Ldsfld, fieldInfo));
					} else {
						result.Add(new CodeInstruction(OpCodes.Ldarg_0));
						result.Add(new CodeInstruction(parameterType.IsByRef ? OpCodes.Ldflda : OpCodes.Ldfld, fieldInfo));
					}
				} else if (Regex.IsMatch(parameterName, "__local[0-9]+")) {
					int localIndex = int.Parse(parameterName.Substring("__".Length));
					IList<LocalVariableInfo> localVariableInfos = originalMethod.GetMethodBody().LocalVariables;
					LocalVariableInfo targetLocalVariable = localVariableInfos.FirstOrDefault(info => info.LocalIndex == localIndex);
					if (targetLocalVariable == null) {
						logger.LogError($"MidFix patch '{midFixPatch.FullDescription()}'"
								+ $" failed, did not find local variable at index '{localIndex}' in method '{originalMethod.FullDescription()}'");
						return null;
					}
					if (!parameterElementType.IsAssignableFrom(targetLocalVariable.LocalType)) {
						LogErrorTypeMismatch(parameterName, midFixPatch, parameterElementType, targetLocalVariable.LocalType);
						return null;
					}
					result.Add(new CodeInstruction(parameterType.IsByRef ? OpCodes.Ldloca_S : OpCodes.Ldloc_S, localIndex));
				} else {
					// targeting method parameter, either via '__Index' or 'paramName'
					ParameterInfo targetParameter;
					if (Regex.IsMatch(parameterName, "__[0-9]+")) {
						int paramIndex = int.Parse(parameterName.Substring("__".Length));
						targetParameter = originalParamDict.Values.FirstOrDefault(paramInfo => paramInfo.Position == paramIndex);
						if (targetParameter == null) {
							logger.LogError($"MidFix patch '{midFixPatch.FullDescription()}'"
									+ $" failed, did not find parameter at index '{paramIndex}' in method '{originalMethod.FullDescription()}'");
							return null;
						}
					} else {
						if (!originalParamDict.TryGetValue(parameterName, out targetParameter)) {
							logger.LogError($"MidFix patch '{midFixPatch.FullDescription()}'"
									+ $" failed, did not find parameter with name '{parameterName}' in method '{originalMethod.FullDescription()}'");
							return null;
						}
					}

					Type targetParameterType = targetParameter.ParameterType;
					Type targetParameterElementType = targetParameterType.IsByRef ? targetParameterType.GetElementType() : targetParameterType;
					Debug.Assert(targetParameterElementType != null, nameof(targetParameterElementType) + " != null (internal error)");

					if (!parameterElementType.IsAssignableFrom(targetParameterElementType)) {
						LogErrorTypeMismatch(parameterName, midFixPatch, parameterElementType, targetParameterElementType);
						return null;
					}

					bool originalParamIsOut = targetParameterType.IsByRef || targetParameter.IsOut;
					bool patchParamIsOut = parameterType.IsByRef || parameter.IsOut;

					if (targetParameterElementType.IsValueType != parameterElementType.IsValueType) {
						logger.LogError($"MidFix patch '{midFixPatch.FullDescription()}'"
								+ $" parameter '{parameterName}' is {(parameterElementType.IsValueType ? "valueType" : "objectType")}"
								+ $", but original '{originalMethod.FullDescription()}' is not. Boxing/Unboxing is not supported by BTHarmonyUtils.");
						return null;
					}

					int actualParamIndex = targetParameter.Position + (originalMethod.IsStatic ? 0 : 1);
					if (originalParamIsOut == patchParamIsOut) {
						result.Add(new CodeInstruction(OpCodes.Ldarg, actualParamIndex));
					} else if (!originalParamIsOut) {
						// !originalParamIsOut && patchParamIsOut
						result.Add(new CodeInstruction(OpCodes.Ldarga, actualParamIndex));
					} else {
						// originalParamIsOut && !patchParamIsOut
						result.Add(new CodeInstruction(OpCodes.Ldarg, actualParamIndex));
						result.Add(targetParameterElementType.IsValueType
								? new CodeInstruction(OpCodes.Ldobj, targetParameterElementType)
								: new CodeInstruction(GetIndOpcode(parameterType)));
					}
				}
			}

			result.Add(new CodeInstruction(OpCodes.Call, midFixPatch));

			Type returnType = midFixPatch.ReturnType;
			if (returnType == typeof(void)) {
				if (resultLocal != null) {
					logger.LogWarning($"MidFix patch '{midFixPatch.FullDescription()}' __result will be ignored, because return type is void");
				}
			} else if (returnType == typeof(bool)) {
				Label midFixContinueLabel = generator.DefineLabel();
				result.Add(new CodeInstruction(OpCodes.Brtrue, midFixContinueLabel));
				if (resultLocal != null) {
					result.Add(new CodeInstruction(OpCodes.Ldloc_S, resultLocal));
				}
				result.Add(new CodeInstruction(OpCodes.Ret));
				CodeInstruction midFixContinueInstruction = new CodeInstruction(OpCodes.Nop);
				midFixContinueInstruction.labels.Add(midFixContinueLabel);
				result.Add(midFixContinueInstruction);
			} else {
				logger.LogWarning($"MidFix patch '{midFixPatch.FullDescription()}' does not return a recognized type, will be treated as returning void");
				result.Add(new CodeInstruction(OpCodes.Pop));
				if (resultLocal != null) {
					logger.LogWarning($"MidFix patch '{midFixPatch.FullDescription()}' __result will be ignored, because return type is void");
				}
			}

			return result;
		}

		private static void LogErrorTypeMismatch(string parameterName, MethodBase patchMethod, Type patchType, Type originalType) {
			logger.LogError($"MidFix parameter '{parameterName}' for patch: {patchMethod.FullDescription()} is of type '{patchType.FullDescription()}'"
					+ $" and cannot be assigned from original type '{originalType.FullDescription()}' - patch will be skipped.");
		}

		/// <summary>
		/// straight copy from Harmony GetIndOpcode
		/// </summary>
		private static OpCode GetIndOpcode(Type type) {
			if (type.IsEnum) {
				return OpCodes.Ldind_I4;
			}
			if (type == typeof(float)) {
				return OpCodes.Ldind_R4;
			}
			if (type == typeof(double)) {
				return OpCodes.Ldind_R8;
			}
			if (type == typeof(byte)) {
				return OpCodes.Ldind_U1;
			}
			if (type == typeof(ushort)) {
				return OpCodes.Ldind_U2;
			}
			if (type == typeof(uint)) {
				return OpCodes.Ldind_U4;
			}
			if (type == typeof(ulong)) {
				return OpCodes.Ldind_I8;
			}
			if (type == typeof(sbyte)) {
				return OpCodes.Ldind_I1;
			}
			if (type == typeof(short)) {
				return OpCodes.Ldind_I2;
			}
			if (type == typeof(int)) {
				return OpCodes.Ldind_I4;
			}
			// ReSharper disable once ConvertIfStatementToReturnStatement
			if (type == typeof(long)) {
				return OpCodes.Ldind_I8;
			}
			return OpCodes.Ldind_Ref;
		}

	}

}
