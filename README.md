## Features

BTHarmonyUtils is stuffed with classes that are intended to make your life easier.  
Let's have a look at its features.

|Feature|Complexity|Description|
|:---|:---|:---|
|[InstructionSimplifier](https://github.com/BlazingTwist/BTHarmonyUtils/wiki/Instruction-Simplifier)|<span style="font-size: 0.8em">🔴</span>|Converts Instructions to a simplified InstructionSet to improve patch consistency.<br/>You won't be using this explicitly, but I recommend taking a look at it to understand how your patches are applied.
|[Midfix_Patch](https://github.com/BlazingTwist/BTHarmonyUtils/wiki/Midfix-Patch)|<span style="font-size: 0.8em">🔴</span>|An extension of Prefix/Postfix patching, lets you place your patch anywhere inside a Method.
|[Transpiler_Patch](https://github.com/BlazingTwist/BTHarmonyUtils/wiki/Transpiler-Patch)|<span style="font-size: 0.8em">🔴🔴</span>|If you want to change Methods in ways that cannot be done with Prefix, Midfix or Postfix patches, this is for you.<br/>This tool takes care of correctly rewriting the instruction stack, so you can focus solely on how code needs to be changed.|
|[IEnumerator_Patching](https://github.com/BlazingTwist/BTHarmonyUtils/wiki/IEnumerator-Patching)|<span style="font-size: 0.8em">🔴</span>|Patching a Method that returns an `IEnumerator`? You'll want this.|
|[Instruction_Search](https://github.com/BlazingTwist/BTHarmonyUtils/wiki/Instruction-Search)|<span style="font-size: 0.8em">🔴🔴🔴</span>|Useful for more complex Transpiler operations like<br/>* branching to an existing label (e.g. when patching `if` conditionals)<br/>* or dynamically extracting local variable indices.|
|[MethodBodyReader](https://github.com/BlazingTwist/BTHarmonyUtils/wiki/Method-Body-Reader)|<span style="font-size: 0.8em">🔴🔴🔴</span>|If you, for some reason, want to read the Instructions of a Method without Patching it with a Transpiler, this is for you.<br/>As an example: you could dynamically extract part of an existing Method into a separate Method.|
|[ByteBuffer](https://github.com/BlazingTwist/BTHarmonyUtils/wiki/Byte-Buffer)|<span style="font-size: 0.8em">🔴🔴</span>|Allows you to read built-in value types from an Array of Bytes.<br/>Main use-case is reading compressed data from Save-Files or Memory.|
|[TableBuilders](https://github.com/BlazingTwist/BTHarmonyUtils/wiki/Table-Builders)|<span style="font-size: 0.8em">🔴</span>|Allows you to easy to read tables to log files or other places.|