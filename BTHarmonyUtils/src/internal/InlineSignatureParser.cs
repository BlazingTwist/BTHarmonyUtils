﻿using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using HarmonyLib;
using Mono.Cecil;

namespace BTHarmonyUtils.@internal {

	internal static class InlineSignatureParser {

		// Based on https://github.com/pardeike/Harmony/blob/381b72c48a9929c55e3469d2bd50b3176a6d5f89/Harmony/Internal/InlineSignatureParser.cs
		// ... which is based on https://github.com/MonoMod/MonoMod.Common/blob/fb7fed148af165905ee0f2db1bb4c78a0137fb89/Utils/ReflectionHelper.ParseCallSite.cs
		// ... which is based on https://github.com/jbevain/cecil/blob/96026325ee1cb6627a3e4a32b924ab2905f02553/Mono.Cecil/AssemblyReader.cs#L3448

		internal static InlineSignature ImportCallSite(Module moduleFrom, byte[] data) {
			InlineSignature callsite = new InlineSignature();

			// Based on https://github.com/jbevain/cecil/blob/96026325ee1cb6627a3e4a32b924ab2905f02553/Mono.Cecil/AssemblyReader.cs#L3448

			using (MemoryStream stream = new MemoryStream(data, false)) {
				using (BinaryReader reader = new BinaryReader(stream)) {
					ReadMethodSignature(callsite);
					return callsite;


					void ReadMethodSignature(InlineSignature method) {
						byte callConv = reader.ReadByte();

						if ((callConv & 0x20) != 0) {
							method.HasThis = true;
							callConv = (byte) (callConv & ~0x20);
						}

						if ((callConv & 0x40) != 0) {
							method.ExplicitThis = true;
							callConv = (byte) (callConv & ~0x40);
						}

						method.CallingConvention = (CallingConvention) callConv + 1;

						if ((callConv & 0x10) != 0) {
							_ = ReadCompressedUInt32();
							// Generic-ness shouldn't apply to CallSites.
						}

						uint paramCount = ReadCompressedUInt32();

						method.ReturnType = ReadTypeSignature();

						for (int i = 0; i < paramCount; i++)
							method.Parameters.Add(ReadTypeSignature());
					}


					uint ReadCompressedUInt32() {
						byte first = reader.ReadByte();
						if ((first & 0x80) == 0)
							return first;

						if ((first & 0x40) == 0)
							return ((uint) (first & ~0x80) << 8)
									| reader.ReadByte();

						return ((uint) (first & ~0xc0) << 24)
								| (uint) reader.ReadByte() << 16
								| (uint) reader.ReadByte() << 8
								| reader.ReadByte();
					}


					int ReadCompressedInt32() {
						byte b = reader.ReadByte();
						_ = reader.BaseStream.Seek(-1, SeekOrigin.Current);
						int u = (int) ReadCompressedUInt32();
						int v = u >> 1;
						if ((u & 1) == 0)
							return v;

						switch (b & 0xc0) {
							case 0:
							case 0x40:
								return v - 0x40;

							case 0x80:
								return v - 0x2000;

							default:
								return v - 0x10000000;
						}
					}


					Type GetTypeDefOrRef() {
						uint tokenData = ReadCompressedUInt32();

						uint rid = tokenData >> 2;
						uint token;
						switch (tokenData & 3) {
							case 0:
								token = (uint) TokenType.TypeDef | rid;
								break;
							case 1:
								token = (uint) TokenType.TypeRef | rid;
								break;
							case 2:
								token = (uint) TokenType.TypeSpec | rid;
								break;
							default:
								token = 0;
								break;
						}
						return moduleFrom.ResolveType((int) token);
					}


					object ReadTypeSignature() {
						MetadataType eType = (MetadataType) reader.ReadByte();
						switch (eType) {
							case MetadataType.ValueType:
							case MetadataType.Class:
								return GetTypeDefOrRef();

							case MetadataType.Pointer:
								return ((Type) ReadTypeSignature()).MakePointerType();

							case MetadataType.FunctionPointer:
								InlineSignature fPtr = new InlineSignature();
								ReadMethodSignature(fPtr);
								/* Note: Inline function pointer signatures cannot be made pointer / byref / array / ... types themselves.
								 * That's because InlineSignature does not extend Type, unlike Cecil, where function pointers are literal types.
								 * Unfortunately System.Reflection does not allow for creating function pointer types on demand.
								 * Maybe System.Reflection.Emit / Mono.Cecil can be used to emit those types instead?
								 * In the worst case, void* might be used instead of (fnptrtype)*
								 * -ade
								 */
								return fPtr;

							case MetadataType.ByReference:
								return ((Type) ReadTypeSignature()).MakePointerType();

							// System.Reflection lacks PinnedType.
							/*
							case MetadataType.Pinned:
								throw new NotSupportedException();
							*/

							case (MetadataType) 0x1d: // SzArray
								return ((Type) ReadTypeSignature()).MakeArrayType();

							case MetadataType.Array:
								Type aType = (Type) ReadTypeSignature();

								uint rank = ReadCompressedUInt32();

								// The following information cannot be used with System.Reflection,
								// but it still needs to be skipped over.
								uint sizes = ReadCompressedUInt32();
								for (int i = 0; i < sizes; i++)
									_ = ReadCompressedUInt32();

								uint lowBounds = ReadCompressedUInt32();
								for (int i = 0; i < lowBounds; i++)
									_ = ReadCompressedInt32();

								return aType.MakeArrayType((int) rank);

							case MetadataType.OptionalModifier:
								return new InlineSignature.ModifierType {
										IsOptional = true,
										Modifier = GetTypeDefOrRef(),
										Type = ReadTypeSignature(),
								};

							case MetadataType.RequiredModifier:
								return new InlineSignature.ModifierType {
										IsOptional = false,
										Modifier = GetTypeDefOrRef(),
										Type = ReadTypeSignature(),
								};

							// System.Reflection lacks SentinelType.
							/*
							case MetadataType.Sentinel:
								throw new NotSupportedException();
							*/

							case MetadataType.Var:
							case MetadataType.MVar:
							case MetadataType.GenericInstance:
								throw new NotSupportedException($"Unsupported generic callsite element: {eType}");

							case MetadataType.Object:
								return typeof(object);

							case MetadataType.Void:
								return typeof(void);

							case MetadataType.TypedByReference:
								return typeof(TypedReference);

							case MetadataType.IntPtr:
								return typeof(IntPtr);

							case MetadataType.UIntPtr:
								return typeof(UIntPtr);

							case MetadataType.Boolean:
								return typeof(bool);

							case MetadataType.Char:
								return typeof(char);

							case MetadataType.SByte:
								return typeof(sbyte);

							case MetadataType.Byte:
								return typeof(byte);

							case MetadataType.Int16:
								return typeof(short);

							case MetadataType.UInt16:
								return typeof(ushort);

							case MetadataType.Int32:
								return typeof(int);

							case MetadataType.UInt32:
								return typeof(uint);

							case MetadataType.Int64:
								return typeof(long);

							case MetadataType.UInt64:
								return typeof(ulong);

							case MetadataType.Single:
								return typeof(float);

							case MetadataType.Double:
								return typeof(double);

							case MetadataType.String:
								return typeof(string);
							
							default:
								throw new NotSupportedException($"Unsupported callsite element: {eType}");
						}
					}
				}
			}
		}

	}

}
