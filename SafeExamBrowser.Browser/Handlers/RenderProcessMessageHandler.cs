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
			
				function loadURL(url) {
					/*var a,b,c;c=url,
					b=document.createElement('iframe'),
					b.setAttribute('src',c),b.setAttribute('id','rusic-modal'),
					b.setAttribute('style','position: fixed; width: 100%; height: 100%; top: 0; left: 0; right: 0; bottom: 0; z-index: 99999999999; background-color: #fff;'),
					a=document.getElementsByTagName('body')[0],
					a.appendChild(b)*/
					window.open(url, '_blank');
				}

				function closeURL() {
					var element = document.getElementById('rusic-modal'); 
					element.parentNode.removeChild(element);
				}

				function goBack() {
					window.history.back();
				}
				
				window.document.addEventListener('keydown', function(e) { if (e.key === 'F9') { loadURL('https://google.com'); } });
				window.document.addEventListener('keydown', function(e) { if (e.key === 'F10') { goBack(); } });
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
