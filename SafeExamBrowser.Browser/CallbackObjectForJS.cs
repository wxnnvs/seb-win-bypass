using CefSharp;
using SafeExamBrowser.Browser.Wrapper;
using SafeExamBrowser.UserInterface.Contracts.Browser.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SafeExamBrowser.Browser
{
    internal class CallbackObjectForJs
    {
        private IWin32Window owner;
        private ICefSharpControl control;

		public CallbackObjectForJs(IWin32Window owner, ICefSharpControl control)
		{
			this.owner = owner;
			this.control = control;
		}

		// Copied and adapted from BrowserControl.cs#80
		public void ExecuteJavaScript(string code, Action<JavaScriptResult> callback = default)
		{
			try
			{
				if (control.BrowserCore != default && control.BrowserCore.MainFrame != default)
				{
					control.BrowserCore.EvaluateScriptAsync(code).ContinueWith(t =>
					{
						callback?.Invoke(new JavaScriptResult
						{
							Message = t.Result.Message,
							Result = t.Result.Result,
							Success = t.Result.Success
						});
					});
				}
				else
				{
					Task.Run(() => callback?.Invoke(new JavaScriptResult
					{
						Message = "JavaScript can't be executed in main frame!",
						Success = false
					}));
				}
			}
			catch (Exception e)
			{
				Task.Run(() => callback?.Invoke(new JavaScriptResult
				{
					Message = $"Failed to execute JavaScript '{(code.Length > 50 ? code.Take(50) : code)}'! Reason: {e.Message}",
					Success = false
				}));
			}
		}

		// seb hijack
		public void ShowMessage(string msg, string title = "Message")
        {   //Read Note
            MessageBox.Show(owner, msg, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void ShowError(string msg, string title = "Error")
        {
            MessageBox.Show(owner, msg, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

		// We gotta update this to be more SEB specific
		public void ExitSEB()
		{
			if (MessageBox.Show("Crashing SEB can take up to 10 seconds. It can be seen in the log files aswell.\n\nAre you sure you want to continue?", "SEB Crash", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
			{
				Environment.Exit(0);
			}
		}

		public async Task SaveAsPDF()
		{
			MessageBox.Show(owner, "Saving as PDF...", "Save as PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
			var settings = new PdfPrintSettings();
			string filename = DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf";
			string filepath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), filename);
			var success = await control.BrowserCore.PrintToPdfAsync(filepath, settings);
			if (success)
			{
				MessageBox.Show(owner, "PDF should be saved to desktop.", "Save as PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			else
			{
				MessageBox.Show(owner, "Failed to generate PDF", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		public void DevTools()
		{
			MessageBox.Show(owner, "Opening Developer Console...", "Developer Console", MessageBoxButtons.OK, MessageBoxIcon.Information);
			control.BrowserCore.ShowDevTools();
		}

		public void Version(string version)
		{
			if (version == "2")
			{
				// ExecuteJavaScript("responseFunction(true);");
				ShowMessage("You are on the latest update:\nPatch 2", "Success");
			}
			else
			{
				// ExecuteJavaScript("responseFunction(false);");
				ShowError($"You are not on the latest update:\nPlease update to Patch {version}. (You are on Patch 2)", "Please update");
			}
		}
	}
}
