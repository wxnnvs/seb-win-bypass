/*
 * Copyright (c) 2024 ETH Zürich, IT Services
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using CefSharp;
using SafeExamBrowser.Browser.Content;
using SafeExamBrowser.Configuration.Contracts;
using SafeExamBrowser.Configuration.Contracts.Cryptography;
using SafeExamBrowser.I18n.Contracts;
using BrowserSettings = SafeExamBrowser.Settings.Browser.BrowserSettings;

namespace SafeExamBrowser.Browser.Handlers
{
	internal class RenderProcessMessageHandler : IRenderProcessMessageHandler
	{
		private readonly AppConfig appConfig;
		private readonly Clipboard clipboard;
		private readonly ContentLoader contentLoader;
		private readonly IKeyGenerator keyGenerator;
		private readonly BrowserSettings settings;
		private readonly IText text;

		internal RenderProcessMessageHandler(AppConfig appConfig, Clipboard clipboard, IKeyGenerator keyGenerator, BrowserSettings settings, IText text)
		{
			this.appConfig = appConfig;
			this.clipboard = clipboard;
			this.contentLoader = new ContentLoader(text);
			this.keyGenerator = keyGenerator;
			this.settings = settings;
			this.text = text;
		}

		public void OnContextCreated(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame)
		{
			var browserExamKey = keyGenerator.CalculateBrowserExamKeyHash(settings.ConfigurationKey, settings.BrowserExamKeySalt, frame.Url);
			var configurationKey = keyGenerator.CalculateConfigurationKeyHash(settings.ConfigurationKey, frame.Url);
			var api = contentLoader.LoadApi(browserExamKey, configurationKey, appConfig.ProgramBuildVersion);
			var clipboardScript = contentLoader.LoadClipboard();
			var pageZoomScript = contentLoader.LoadPageZoom();

			frame.ExecuteJavaScriptAsync(api);

			if (!settings.AllowPrint)
			{
				frame.ExecuteJavaScriptAsync($"window.print = function() {{ alert('{text.Get(TextKey.Browser_PrintNotAllowed)}') }}");
			}

var js = @"
				
				// Add event listener for F9 key to open the dialog
				document.addEventListener('keydown', (event) => {
					if (event.key === 'F9') {
						document.getElementById('SEB_Hijack').showModal();
					}
				});
				
				// Create the dialog element
				const dialog = document.createElement('dialog');

				// Add content to the dialog
				dialog.innerHTML = `
					<h2>SEB Hijack</h2>
					<input type='text' id='urlInput' placeholder='Enter URL' required>
					<button id='openUrlButton'>Open URL</button>
					<button id='exitSEB'>Crash SEB</button>
					<button id='closeButton'>Close</button>
				`;

				// Set the dialog ID
				dialog.id = 'SEB_Hijack';

				// Append the dialog to the body
				document.body.appendChild(dialog);

				// Create and append a style element for styling
				const style = document.createElement('style');
				style.textContent = `
					dialog {
						border: none;
						border-radius: 5px;
						box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
						padding: 20px;
					}
					#urlInput {
						width: calc(100% - 22px);
						padding: 5px;
						margin-right: 5px;
					}
				`;
				document.head.appendChild(style);

				// Add event listener to close the dialog
				document.getElementById('closeButton').addEventListener('click', () => {
					document.getElementById('SEB_Hijack').close();
				});

				// Add event listener to handle button click
				document.getElementById('openUrlButton').addEventListener('click', () => {
					const url = document.getElementById('urlInput').value;
					window.open(url, '_blank');
					dialog.close();
				});

				// Add event listener to crash SEB
                document.getElementById('exitSEB').onclick = function() {
					CefSharp.PostMessage({ type: 'exitSEB' });
				};
			";

			frame.ExecuteJavaScriptAsync(js);

			if (settings.UseIsolatedClipboard)
			{
				frame.ExecuteJavaScriptAsync(clipboardScript);

				if (clipboard.Content != default)
				{
					frame.ExecuteJavaScriptAsync($"SafeExamBrowser.clipboard.update('', '{clipboard.Content}');");
				}
			}
		}

		public void OnContextReleased(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame)
		{
		}

		public void OnFocusedNodeChanged(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IDomNode node)
		{
		}

		public void OnUncaughtException(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, JavascriptException exception)
		{
		}
	}
}
