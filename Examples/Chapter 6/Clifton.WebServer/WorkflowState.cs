using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clifton.WebServer
{
	/// <summary>
	/// Workflow Continuation State
	/// </summary>
	public enum WorkflowState
	{
		/// <summary>
		/// Terminate execution of the workflow.
		/// </summary>
		Abort,

		/// <summary>
		/// Continue with the execution of the workflow.
		/// </summary>
		Continue,

		/// <summary>
		/// Execution is deferred until Continue is called, usually by another thread.
		/// </summary>
		Defer,

		/// <summary>
		/// The workflow should terminate without error.  The workflow step
		/// is indicating that it has handled the request and there is no further
		/// need for downstream processing.
		/// </summary>
		Done,
	}
}
