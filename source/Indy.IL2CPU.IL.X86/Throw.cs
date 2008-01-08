using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using CPU = Indy.IL2CPU.Assembler;
using CPUx86 = Indy.IL2CPU.Assembler.X86;

namespace Indy.IL2CPU.IL.X86 {
	[OpCode(Code.Throw, false)]
	public class Throw: Op {
		private MethodInformation mMethodInfo;
		public Throw(Mono.Cecil.Cil.Instruction aInstruction, MethodInformation aMethodInfo)
			: base(aInstruction, aMethodInfo) {
			mMethodInfo = aMethodInfo;
		}
		public override void DoAssemble() {
			//if ((from item in Assembler.DataMembers
			//     where item.Value.Name == CPU.Assembler.CurrentExceptionDataMember
			//     select item).Count() == 0) {
			//    Assembler.DataMembers.Add(new System.Collections.Generic.KeyValuePair<string, Indy.IL2CPU.Assembler.DataMember>("il2cpu-system", new Indy.IL2CPU.Assembler.DataMember(CPU.Assembler.CurrentExceptionDataMember, "dd", "0")));
			//}
			new CPUx86.Pop("eax");
			new CPUx86.Move("[" + CPU.DataMember.GetStaticFieldName(CPU.Assembler.CurrentExceptionRef) + "]", "eax");
			new CPUx86.Move("ecx", "3");
			Call.EmitExceptionLogic(Assembler, mMethodInfo, null, false);
			/*
			*/
			Assembler.StackSizes.Pop();
		}
	}
}