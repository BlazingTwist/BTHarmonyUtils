using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using BTHarmonyUtils.ILUtils;
using BTHarmonyUtils.@internal;
using BTHarmonyUtils.MidFixPatch;
using HarmonyLib;
using JetBrains.Annotations;

namespace BTHarmonyUtils {

	/// <summary>
	/// Utility class for patching code
	/// </summary>
	[PublicAPI]
	public static class PatcherUtils {

		private static readonly ManualLogSource logger = Logger.CreateLogSource($"BTHarmonyUtils:{nameof(PatcherUtils)}");

		/// <summary>
		/// This makes patching IEnumerator Methods that use `yield` easier.
		/// Searches a method returning `IEnumerator` for the actual implementation of `MoveNext`
		/// 
		/// Beware when Pre- or Post-fixing: the MoveNext method will be called once for every value that the IEnumerator returns!
		/// If you want your Postfix to execute after the last value is returned, you can check if the `bool __result` value is `false`
		///   (indicates that all elements have been iterated over)
		/// </summary>
		/// <param name="method">The Method returning `IEnumerator`</param>
		/// <returns>The MoveNext implementation for the IEnumerator</returns>
		public static MethodInfo FindIEnumeratorMoveNext(MethodBase method) {
			if (method is null) {
				throw new ArgumentNullException(nameof(method));
			}

			foreach (CodeInstruction instruction in new MethodBodyReader(method).ReadInstructions()) {
				if (instruction.opcode == OpCodes.Newobj && instruction.operand is ConstructorInfo ctor) {
					return AccessTools.Method(ctor.DeclaringType, nameof(IEnumerator.MoveNext));
				}
			}
			return null;
		}

		/// <summary>
		/// Apply all HarmonyUtils specific patches, you still need to call harmony.PatchAll for the other patches
		/// </summary>
		/// <param name="harmony">instance of harmony to use for patching</param>
		public static void PatchAll(Harmony harmony) {
			PatchAll(harmony, new StackTrace().GetFrame(1).GetMethod().ReflectedType?.Assembly);
		}

		/// <summary>
		/// Apply all HarmonyUtils specific patches, you still need to call harmony.PatchAll for the other patches
		/// </summary>
		/// <param name="harmony">instance of harmony to use for patching</param>
		/// <param name="assembly">the assembly containing the patches</param>
		public static void PatchAll(Harmony harmony, Assembly assembly) {
			if (assembly == null) {
				throw new ArgumentNullException(nameof(assembly));
			}
			assembly.GetTypes()
					.Where(type => type.GetCustomAttributes(typeof(HarmonyPatch), false).Any())
					.ToList()
					.ForEach(patcherType => PatchAll(harmony, patcherType));
		}

		/// <summary>
		/// Apply all HarmonyUtils specific patches, you still need to call harmony.PatchAll for the other patches
		/// </summary>
		/// <param name="harmony">instance of harmony to use for patching</param>
		/// <param name="type">the type containing the patches</param>
		public static void PatchAll(Harmony harmony, Type type) {
			if (type == null) {
				throw new ArgumentNullException(nameof(type));
			}

			HarmonyMethod classPatchInfo = GetHarmonyInfo(type);
			Type baseType = type;
			while ((baseType = baseType.BaseType) != null) {
				classPatchInfo = GetHarmonyInfo(baseType).Merge(classPatchInfo);
			}

			foreach (MethodInfo method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
				HarmonyMethod methodPatchInfo = classPatchInfo.Merge(GetHarmonyInfo(method));
				BTHarmonyMidFix midFixAttribute = method.GetCustomAttribute<BTHarmonyMidFix>(false);
				if (midFixAttribute != null) {
					MidFixPatcher.DoPatch(harmony, methodPatchInfo, method, midFixAttribute);
				}
			}
		}

		private static HarmonyMethod GetHarmonyInfo(Type type) {
			List<HarmonyMethod> harmonyInfo = type.GetCustomAttributes(typeof(HarmonyPatch), false)
					.Select(attr => ((HarmonyAttribute) attr).info)
					.ToList();
			return HarmonyMethod.Merge(harmonyInfo);
		}

		private static HarmonyMethod GetHarmonyInfo(MethodInfo method) {
			List<HarmonyMethod> harmonyInfo = method.GetCustomAttributes(typeof(HarmonyPatch), false)
					.Select(attr => ((HarmonyAttribute) attr).info)
					.ToList();
			return HarmonyMethod.Merge(harmonyInfo);
		}

		internal static void LogTargetMethodInvalidDeclaration(Type declaringType, MethodInfo method, string errorString, bool hasTargetMethodAttribute) {
			string attributeName = hasTargetMethodAttribute ? nameof(HarmonyTargetMethod) : nameof(HarmonyTargetMethods);
			string returnTypeString = hasTargetMethodAttribute ? "MethodBase" : "IEnumerable<MethodBase>";
			logger.LogError($"Patch class '{declaringType.Name}' contains {attributeName} provider '{method.Name}', {errorString}"
					+ $"\ntry 'private static {returnTypeString} {method.Name}() {{ /* ... */ }}'");
		}

		internal static void LogTargetMethodNoResult(Type declaringType, MethodInfo method, bool hasTargetMethodAttribute) {
			string attributeName = hasTargetMethodAttribute ? nameof(HarmonyTargetMethod) : nameof(HarmonyTargetMethods);
			logger.LogError($"Patch class '{declaringType.Name}' contains {attributeName} provider '{method.Name}', but the return value was null!");
		}

		internal static IEnumerable<MethodBase> ResolveTargetMethod(Type declaringType) {
			List<MethodBase> targetMethods = new List<MethodBase>();
			foreach (MethodInfo method in declaringType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
				HarmonyTargetMethod targetMethodAttribute = method.GetCustomAttribute<HarmonyTargetMethod>(false);
				HarmonyTargetMethods targetMethodsAttribute = method.GetCustomAttribute<HarmonyTargetMethods>(false);
				if (targetMethodAttribute == null && targetMethodsAttribute == null) {
					continue;
				}

				if (!method.IsStatic) {
					LogTargetMethodInvalidDeclaration(
							declaringType, method,
							"but it is not static!", targetMethodAttribute != null
					);
					continue;
				}

				int numParameters = method.GetParameters().Length;
				if (numParameters > 0) {
					LogTargetMethodInvalidDeclaration(
							declaringType, method,
							$"but it takes {numParameters} parameters", targetMethodAttribute != null
					);
					continue;
				}

				if (targetMethodAttribute != null && targetMethodsAttribute != null) {
					logger.LogError($"Patch class '{declaringType.Name}' Method '{method.Name}'"
							+ $" may not contain the Attributes '{nameof(HarmonyTargetMethod)}' and '{nameof(HarmonyTargetMethods)}' at the same time."
							+ $"\ntry '[{nameof(HarmonyTargetMethods)}] private static IEnumerable<MethodBase> {method.Name}() {{ /* ... */ }}'");
					continue;
				}

				if (targetMethodAttribute != null) {
					if (!typeof(MethodBase).IsAssignableFrom(method.ReturnType)) {
						LogTargetMethodInvalidDeclaration(declaringType, method, $"but does not return '{nameof(MethodBase)}'", true);
						continue;
					}

					if (method.Invoke(null, null) is MethodBase result) {
						targetMethods.Add(result);
					} else {
						LogTargetMethodNoResult(declaringType, method, true);
					}
				} else {
					if (!typeof(IEnumerable<MethodBase>).IsAssignableFrom(method.ReturnType)
							|| method.ReturnType.GenericTypeArguments.Length <= 0
							|| !typeof(MethodBase).IsAssignableFrom(method.ReturnType.GenericTypeArguments[0])) {
						LogTargetMethodInvalidDeclaration(declaringType, method, "but does not return 'IEnumerable<MethodBase>'", false);
						continue;
					}

					if (method.Invoke(null, null) is IEnumerable<object> result) {
						targetMethods.AddRange((result).Select(x => x as MethodBase));
					} else {
						LogTargetMethodNoResult(declaringType, method, false);
					}
				}
			}
			return targetMethods.Count <= 0 ? null : targetMethods;
		}

		/// <summary>
		/// resolve the HarmonyMethod info from HarmonyAnnotations to a method
		/// </summary>
		internal static MethodBase ResolveHarmonyMethod(HarmonyMethod info, string patchName) {
			switch (info.methodType ?? MethodType.Normal) {
				case MethodType.Normal: {
					if (string.IsNullOrEmpty(info.methodName)) {
						LogPatchFailure(patchName, "methodName cannot be empty");
						return null;
					}
					if (info.methodName == ".ctor") {
						logger.LogWarning($"{patchName} - MethodType.Constructor should be used instead of setting methodName to .ctor");
						goto case MethodType.Constructor;
					}
					if (info.methodName == ".cctor") {
						logger.LogWarning($"{patchName} - MethodType.StaticConstructor should be used instead of setting methodName to .cctor");
						goto case MethodType.StaticConstructor;
					}
					if (info.methodName.StartsWith("get_")) {
						logger.LogWarning($"{patchName} - MethodType.Getter should be used instead of adding get_ to property names");
						info.methodName = info.methodName.Substring("get_".Length);
						goto case MethodType.Getter;
					}
					if (info.methodName.StartsWith("set_")) {
						logger.LogWarning($"{patchName} - MethodType.Setter should be used instead of adding set_ to property names");
						info.methodName = info.methodName.Substring("set_".Length);
						goto case MethodType.Setter;
					}
					MethodInfo declaredMethod = AccessTools.DeclaredMethod(info.declaringType, info.methodName, info.argumentTypes);
					if (declaredMethod != null) {
						return declaredMethod;
					}
					MethodInfo baseMethod = AccessTools.Method(info.declaringType, info.methodName, info.argumentTypes);
					if (baseMethod != null) {
						logger.LogWarning($"{patchName} - Could not find method {info.methodName} with {info.argumentTypes.Description()} arguments"
								+ $" in type {info.declaringType.FullDescription()}, but it was found in base class {baseMethod.DeclaringType.FullDescription()}");
						return baseMethod;
					}
					logger.LogError($"{patchName} - Could not find method {info.methodName}"
							+ $" with {info.argumentTypes.Description()} arguments in type {info.declaringType.FullDescription()}");
					return null;
				}
				case MethodType.Getter: {
					PropertyInfo resolveProperty = ResolveProperty(info, patchName);
					if (resolveProperty == null) {
						return null;
					}
					MethodInfo getter = resolveProperty.GetGetMethod(true);
					if (getter == null) {
						LogPatchFailure(patchName, $"Property {info.methodName} does not have a Getter");
					}
					return getter;
				}
				case MethodType.Setter: {
					PropertyInfo resolveProperty = ResolveProperty(info, patchName);
					if (resolveProperty == null) {
						return null;
					}
					MethodInfo setter = resolveProperty.GetSetMethod(true);
					if (setter == null) {
						LogPatchFailure(patchName, $"Property {info.methodName} does not have a Setter");
					}
					return setter;
				}
				case MethodType.Constructor: {
					ConstructorInfo constructorInfo = AccessTools.DeclaredConstructor(info.declaringType, info.argumentTypes);
					if (constructorInfo == null) {
						LogPatchFailure(patchName, "Could not find constructor"
								+ $" with {info.argumentTypes.Description()} parameters in type {info.declaringType.FullDescription()}");
					}
					return constructorInfo;
				}
				case MethodType.StaticConstructor: {
					ConstructorInfo constructorInfo = AccessTools.GetDeclaredConstructors(info.declaringType)
							.FirstOrDefault(constructor => constructor.IsStatic);
					if (constructorInfo == null) {
						LogPatchFailure(patchName, $"Could not find static constructor in type {info.declaringType.FullDescription()}");
					}
					return constructorInfo;
				}
				default:
					LogPatchFailure(patchName, $"MethodType: {info.methodType} is not supported by BTHarmonyUtils.");
					return null;
			}
		}

		private static PropertyInfo ResolveProperty(HarmonyMethod info, string patchName) {
			if (string.IsNullOrEmpty(info.methodName)) {
				LogPatchFailure(patchName, "methodName cannot be empty");
				return null;
			}
			PropertyInfo declaredProperty = AccessTools.DeclaredProperty(info.declaringType, info.methodName);
			if (declaredProperty != null) {
				return declaredProperty;
			}
			PropertyInfo baseProperty = AccessTools.Property(info.declaringType, info.methodName);
			if (baseProperty != null) {
				logger.LogWarning($"{patchName} - Could not find property {info.methodName} in type {info.declaringType.FullDescription()}"
						+ $", but it was found in base class of this type: {baseProperty.DeclaringType.FullDescription()}");
				return baseProperty;
			}
			LogPatchFailure(patchName, $"Could not find property {info.methodName} in type {info.declaringType.FullDescription()}");
			return null;
		}

		private static void LogPatchFailure(string patchName, string reason) {
			logger.LogError("Failed to process patch " + patchName + " - " + reason);
		}

	}

}
