using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using JetBrains.Annotations;

namespace BTHarmonyUtils.ILUtils {
	/// <summary>
	/// A class for reading CodeInstructions from Methods
	/// </summary>
	[PublicAPI]
	public class MethodBodyReader {
		private static readonly OpCode[] one_byte_opcodes;
		private static readonly OpCode[] two_bytes_opcodes;

		private readonly Module module;
		private readonly Type[] typeArguments;
		private readonly Type[] methodArguments;
		private readonly ByteBuffer ilBytes;
		private readonly List<LocalVariableInfo> localVariables;

		private List<CodeInstruction> instructions;

		[MethodImpl(MethodImplOptions.Synchronized)]
		static MethodBodyReader() {
			one_byte_opcodes = new OpCode[0xe1];
			two_bytes_opcodes = new OpCode[0x1f];

			FieldInfo[] fields = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static);

			foreach (FieldInfo field in fields) {
				OpCode opcode = (OpCode) field.GetValue(null);
				if (opcode.OpCodeType == OpCodeType.Nternal) {
					continue;
				}

				if (opcode.Size == 1) {
					one_byte_opcodes[opcode.Value] = opcode;
				} else {
					two_bytes_opcodes[opcode.Value & 0xff] = opcode;
				}
			}
		}

		/// <summary>
		/// Create a MethodBodyReader for a given Method
		/// </summary>
		/// <param name="method">The Method to read from</param>
		public MethodBodyReader(MethodBase method) {
			module = method.Module;

			MethodBody body = method.GetMethodBody();
			ilBytes = new ByteBuffer(body?.GetILAsByteArray() ?? new byte[0]);

			Type declaringType = method.DeclaringType;
			if (!(declaringType is null) && declaringType.IsGenericType) {
				try {
					typeArguments = declaringType.GetGenericArguments();
				} catch {
					typeArguments = null;
				}
			}

			if (method.IsGenericMethod) {
				try {
					methodArguments = method.GetGenericArguments();
				} catch {
					methodArguments = null;
				}
			}

			localVariables = body?.LocalVariables.ToList() ?? new List<LocalVariableInfo>();
		}

		/// <summary>
		/// Read all of the CodeInstructions of this method into a List
		/// </summary>
		/// <returns>List of CodeInstructions</returns>
		public List<CodeInstruction> ReadInstructions() {
			if (instructions == null) {
				instructions = new List<CodeInstruction>();
				while (ilBytes.CanRead()) {
					OpCode opcode = ReadOpCode();
					object operand = ReadOperand(opcode);
					instructions.Add(new CodeInstruction(opcode, operand));
				}
			}
			return instructions;
		}

		private OpCode ReadOpCode() {
			byte op = ilBytes.ReadByte();
			return op != 0xfe
					? one_byte_opcodes[op]
					: two_bytes_opcodes[ilBytes.ReadByte()];
		}

		// interpret member info value
		private static object GetMemberInfoValue(MemberInfo info) {
			switch (info.MemberType) {
				case MemberTypes.Constructor:
					return (ConstructorInfo) info;
				case MemberTypes.Event:
					return (EventInfo) info;
				case MemberTypes.Field:
					return (FieldInfo) info;
				case MemberTypes.Method:
					return (MethodInfo) info;
				case MemberTypes.TypeInfo:
				case MemberTypes.NestedType:
					return (Type) info;
				case MemberTypes.Property:
					return (PropertyInfo) info;
				default:
					return null;
			}
		}

		private static bool TargetsLocalVariable(OpCode opcode) {
			return opcode.Name.Contains("loc");
		}

		// interpret instruction operand
		private object ReadOperand(OpCode opcode) {
			switch (opcode.OperandType) {
				case OperandType.InlineNone: {
					return null;
				}

				case OperandType.InlineSwitch: {
					int length = ilBytes.ReadInt32();
					int base_offset = ilBytes.Position + (4 * length);
					int[] branches = new int[length];
					for (int i = 0; i < length; i++)
						branches[i] = ilBytes.ReadInt32() + base_offset;
					return branches;
				}

				case OperandType.ShortInlineBrTarget: {
					sbyte val = (sbyte) ilBytes.ReadByte();
					return val + ilBytes.Position;
				}

				case OperandType.InlineBrTarget: {
					int val = ilBytes.ReadInt32();
					return val + ilBytes.Position;
				}

				case OperandType.ShortInlineI: {
					if (opcode == OpCodes.Ldc_I4_S) {
						return (sbyte) ilBytes.ReadByte();
					}
					return ilBytes.ReadByte();
				}

				case OperandType.InlineI: {
					return ilBytes.ReadInt32();
				}

				case OperandType.ShortInlineR: {
					return ilBytes.ReadSingle();
				}

				case OperandType.InlineR: {
					return ilBytes.ReadDouble();
				}

				case OperandType.InlineI8: {
					return ilBytes.ReadInt64();
				}

				case OperandType.InlineSig: {
					int val = ilBytes.ReadInt32();
					byte[] bytes = module.ResolveSignature(val);
					return InlineSignatureParser.ImportCallSite(module, bytes);
				}

				case OperandType.InlineString: {
					int val = ilBytes.ReadInt32();
					return module.ResolveString(val);
				}

				case OperandType.InlineTok: {
					int val = ilBytes.ReadInt32();
					return GetMemberInfoValue(module.ResolveMember(val, typeArguments, methodArguments));
				}

				case OperandType.InlineType: {
					int val = ilBytes.ReadInt32();
					return module.ResolveType(val, typeArguments, methodArguments);
				}

				case OperandType.InlineMethod: {
					int val = ilBytes.ReadInt32();
					MethodBase resolveMethod = module.ResolveMethod(val, typeArguments, methodArguments);
					if (resolveMethod is ConstructorInfo constructorInfo) {
						return constructorInfo;
					}
					return (MethodInfo) resolveMethod;
				}

				case OperandType.InlineField: {
					int val = ilBytes.ReadInt32();
					return module.ResolveField(val, typeArguments, methodArguments);
				}

				case OperandType.ShortInlineVar: {
					byte index = ilBytes.ReadByte();
					if (TargetsLocalVariable(opcode)) {
						LocalVariableInfo variableInfo = localVariables[index];
						if (!(variableInfo is null)) {
							return variableInfo;
						}
					}
					return index;
				}

				case OperandType.InlineVar: {
					short index = ilBytes.ReadInt16();
					if (TargetsLocalVariable(opcode)) {
						LocalVariableInfo variableInfo = localVariables[index];
						if (!(variableInfo is null)) {
							return variableInfo;
						}
					}
					return index;
				}

				default:
					throw new NotSupportedException();
			}
		}
	}
}