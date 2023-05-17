using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MoonSharp.VsCodeDebugger;

namespace MoonSharp.Commands.Implementations
{
	class DebugCommand : ICommand
	{
		private MoonSharpVsCodeDebugServer m_Debugger;

		public string Name
		{
			get { return "debug"; }
		}

		public void DisplayShortHelp()
		{
			Console.WriteLine("debug - Starts the interactive debugger");
		}

		public void DisplayLongHelp()
		{
			Console.WriteLine("debug - Starts the interactive debugger. Requires a web browser with Flash installed.");
		}

		public void Execute(ShellContext context, string arguments)
		{
			if (m_Debugger == null)
			{
				m_Debugger = new MoonSharpVsCodeDebugServer();
				m_Debugger.AttachToScript(context.Script, "MoonSharp REPL interpreter");
				m_Debugger.Start();
			}
		}
	}
}
