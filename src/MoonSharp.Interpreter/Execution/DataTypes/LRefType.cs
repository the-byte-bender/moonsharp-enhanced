﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MoonSharp.Interpreter.Execution
{
	public enum LRefType
	{
		Invalid,
		Global,
		Local,
		Upvalue,
		Index,
		Argument
	}
}