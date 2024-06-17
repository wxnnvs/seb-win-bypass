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

			frame.ExecuteJavaScriptAsync(api);

			var js = @"
			
				// My stuff

				function loadURL() {
					var url = document.getElementById('inputURL').value;
					window.open(url, '_blank');
				}

				function goBack() {
					window.history.back();
				}

				window.document.addEventListener('keydown', function (e) {
					if (e.key === 'F9') {
						showModal();
					}
				});
				window.document.addEventListener('keydown', function (e) {
					if (e.key === 'F10') {
						goBack();
					}
				});

				// Make a modal using the dialog element
				function showModal() {
					if (document.getElementById('hijackdialog')) {
						document.getElementById('hijackdialog').showModal();
						return;
					} else {
						var dialog = document.createElement('dialog');
						dialog.id = 'hijackdialog';
						dialog.innerHTML = `
							<style>

								dialog.hijack {
									width: 300px;
									padding: 20px;
									background-color: #f2f2f2;
									border: 1px solid #ccc;
									border-radius: 4px;
								}

								dialog.hijack input[type='text'] {
									width: 100%;
									margin-bottom: 10px;
									padding: 5px;
									border: 1px solid #ccc;
									border-radius: 4px;
								}

								dialog.hijack button {
									padding: 5px 10px;
									background-color: #4CAF50;
									color: white;
									border: none;
									border-radius: 4px;
									cursor: pointer;
								}

								dialog.hijack button:hover {
									background-color: #45a049;
								}
							</style>
							<!-- an input field for a url to load -->
							<input id='inputURL' type='text' placeholder='Enter URL'>
							<!-- a button to load the url -->
							<button id='load'>Load</button>
							<button id='close'>Close</button>
						`;
						dialog.classList.add('hijack');
						document.body.appendChild(dialog);
						dialog.showModal();
						var url = document.getElementById('inputURL').value;
						document.getElementById('close').onclick = function () {
							dialog.close();
						};
						document.getElementById('load').onclick = function() {
							loadURL();
						};
					}
				}
				";

			frame.ExecuteJavaScriptAsync(js);

			if (!settings.AllowPrint)
			{
				frame.ExecuteJavaScriptAsync($"window.print = function() {{ alert('{text.Get(TextKey.Browser_PrintNotAllowed)}') }}");
			}

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
