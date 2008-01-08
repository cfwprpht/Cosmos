using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using CPU = Indy.IL2CPU.Assembler;
using CPUx86 = Indy.IL2CPU.Assembler.X86;

namespace Indy.IL2CPU.IL.X86 {
	[OpCode(Code.Call)]
	public class Call: Op {
		private string LabelName;
		private int mResultSize;
		private int? TotalArgumentSize = null;
		private bool mIsDebugger_Break = false;
		private int[] ArgumentSizes = new int[0];
		private MethodInformation mMethodInfo;
		private string mNextLabelName;
		public Call(MethodReference aMethod)
			: base(null, null) {
			if (aMethod == null) {
				throw new ArgumentNullException("aMethod");
			}
			Initialize(aMethod);
		}

		public static void EmitExceptionLogic(Assembler.Assembler aAssembler, MethodInformation aMethodInfo, string aNextLabel, bool aDoTest) {
			//if (!aAssembler.InMetalMode) {
			if (aMethodInfo != null && aMethodInfo.LabelName == "System_Void___Cosmos_Shell_Console_Commands_MatthijsCommand_Execute___System_String___") {
				//System.Diagnostics.Debugger.Break();
			}
			string xJumpTo = MethodFooterOp.EndOfMethodLabelNameException;
			if (aMethodInfo != null && aMethodInfo.CurrentHandler != null) {
				switch (aMethodInfo.CurrentHandler.Type) {
					case ExceptionHandlerType.Catch: {
							xJumpTo = Op.GetInstructionLabel(aMethodInfo.CurrentHandler.HandlerStart);
							break;
						}
					case ExceptionHandlerType.Finally: {
							xJumpTo = Op.GetInstructionLabel(aMethodInfo.CurrentHandler.HandlerStart);
							break;
						}
					default: {
							throw new Exception("ExceptionHandlerType '" + aMethodInfo.CurrentHandler.Type.ToString() + "' not supported yet!");
						}
				}
			}
			if (!aDoTest) {
				new CPUx86.JumpAlways(xJumpTo);
			} else {
				new CPUx86.Test("ecx", "2");
				new CPUx86.JumpIfNotEquals(xJumpTo);
			}
			//    }
			//    new CPUx86.JumpAlways(xJumpTo);
			//}
		}


		private void Initialize(MethodReference aMethod) {
			mIsDebugger_Break = aMethod.GetFullName() == "System.Void System.Diagnostics.Debugger.Break()";
			if (mIsDebugger_Break) {
				return;
			}
			mResultSize = Engine.GetFieldStorageSize(aMethod.ReturnType.ReturnType);
			if (mResultSize > 8) {
				throw new Exception("ReturnValues of sizes larger than 8 bytes not supported yet (" + mResultSize + ")");
			}
			MethodDefinition xMethodDef = Engine.GetDefinitionFromMethodReference(aMethod);
			LabelName = CPU.Label.GenerateLabelName(xMethodDef);
			Engine.QueueMethodRef(xMethodDef);
			bool needsCleanup = false;
			List<int> xArgumentSizes = new List<int>();
			foreach (ParameterDefinition xParam in xMethodDef.Parameters) {
				xArgumentSizes.Add(Engine.GetFieldStorageSize(xParam.ParameterType));
			}
			if (!xMethodDef.IsStatic) {
				xArgumentSizes.Insert(0, 4);
			}
			ArgumentSizes = xArgumentSizes.ToArray();
			foreach (ParameterDefinition xParam in xMethodDef.Parameters) {
				if (xParam.IsOut) {
					needsCleanup = true;
					break;
				}
			}
			if (needsCleanup) {
				TotalArgumentSize = xMethodDef.Parameters.Count * 4;
				if (!xMethodDef.IsStatic) {
					TotalArgumentSize += 4;
				}
			}
			// todo: add support for other argument sizes
		}

		public Call(Instruction aInstruction, MethodInformation aMethodInfo)
			: base(aInstruction, aMethodInfo) {
			MethodReference xMethod = ((MethodReference)aInstruction.Operand);
			mMethodInfo = aMethodInfo;
			if (aInstruction != null && aInstruction.Next != null) {
				mNextLabelName = GetInstructionLabel(aInstruction.Next);
			}
			Initialize(xMethod);
		}
		public void Assemble(string aMethod, int aArgumentCount) {
			new CPUx86.Call(aMethod);
			EmitExceptionLogic(Assembler, mMethodInfo, mNextLabelName, true);
			for (int i = 0; i < aArgumentCount; i++) {
				Assembler.StackSizes.Pop();
			}
			if (mResultSize == 0) {
				return;
			}
			if (mResultSize <= 4) {
				new CPUx86.Push(CPUx86.Registers.EAX);
				Assembler.StackSizes.Push(mResultSize);
				return;
			}
			if (mResultSize <= 8) {
				new CPUx86.Push(CPUx86.Registers.EBX);
				new CPUx86.Push(CPUx86.Registers.EAX);
				Assembler.StackSizes.Push(mResultSize);
				return;
			}
		}

		protected virtual void HandleDebuggerBreak() {
			//
		}

		public override void DoAssemble() {
			if (mIsDebugger_Break) {
				HandleDebuggerBreak();
			} else {
				Assemble(LabelName, ArgumentSizes.Length);
			}
		}
	}
}