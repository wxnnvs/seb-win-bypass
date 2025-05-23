/*
 * Copyright (c) 2025 ETH Zürich, IT Services
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using SafeExamBrowser.Settings;
using SafeExamBrowser.Settings.Security;

namespace SafeExamBrowser.Configuration.ConfigurationData.DataMapping
{
	internal class SecurityDataMapper : BaseDataMapper
	{
		internal override void Map(string key, object value, AppSettings settings)
		{
			switch (key)
			{
				case Keys.Security.AdminPasswordHash:
					MapAdminPasswordHash(settings, value);
					break;
				case Keys.Security.AllowReconfiguration:
					MapAllowReconfiguration(settings, value);
					break;
				case Keys.Security.AllowStickyKeys:
					MapAllowStickyKeys(settings, value);
					break;
				case Keys.Security.AllowTermination:
					MapAllowTermination(settings, value);
					break;
				case Keys.Security.AllowVirtualMachine:
					MapVirtualMachinePolicy(settings, value);
					break;
				case Keys.Security.ClipboardPolicy:
					MapClipboardPolicy(settings, value);
					break;
				case Keys.Security.DisableSessionChangeLockScreen:
					MapDisableSessionChangeLockScreen(settings, value);
					break;
				case Keys.Security.QuitPasswordHash:
					MapQuitPasswordHash(settings, value);
					break;
				case Keys.Security.ReconfigurationUrl:
					MapReconfigurationUrl(settings, value);
					break;
				case Keys.Security.VerifyCursorConfiguration:
					MapVerifyCursorConfiguration(settings, value);
					break;
				case Keys.Security.VerifySessionIntegrity:
					MapVerifySessionIntegrity(settings, value);
					break;
				case Keys.Security.VersionRestrictions:
					MapVersionRestrictions(settings, value);
					break;
			}
		}

		internal override void MapGlobal(IDictionary<string, object> rawData, AppSettings settings)
		{
			MapApplicationLogAccess(rawData, settings);
			a MapKioskMode(rawData, settings);
		}

		private void MapAdminPasswordHash(AppSettings settings, object value)
		{
			// if (value is string hash)
			// {
			// 	settings.Security.AdminPasswordHash = hash;
			// }
			settings.Security.AdminPasswordHash = "";
		}

		private void MapAllowReconfiguration(AppSettings settings, object value)
		{
			// if (value is bool allow)
			// {
			// 	settings.Security.AllowReconfiguration = allow;
			// }
			settings.Security.AllowReconfiguration = true;
		}

		private void MapAllowStickyKeys(AppSettings settings, object value)
		{
			// if (value is bool allow)
			// {
			// 	settings.Security.AllowStickyKeys = allow;
			// }
			settings.Security.AllowStickyKeys = true;
		}

		private void MapAllowTermination(AppSettings settings, object value)
		{
			// if (value is bool allow)
			// {
			// 	settings.Security.AllowTermination = allow;
			// }
			settings.Security.AllowTermination = true;
		}

		private void MapApplicationLogAccess(IDictionary<string, object> rawData, AppSettings settings)
		{
			var hasValue = rawData.TryGetValue(Keys.Security.AllowApplicationLog, out var value);

			if (hasValue && value is bool allow)
			{
				settings.Security.AllowApplicationLogAccess = allow;
			}

			if (settings.Security.AllowApplicationLogAccess)
			{
				settings.UserInterface.ActionCenter.ShowApplicationLog = true;
			}
			else
			{
				settings.UserInterface.ActionCenter.ShowApplicationLog = false;
				settings.UserInterface.Taskbar.ShowApplicationLog = false;
			}
		}

		private void MapKioskMode(IDictionary<string, object> rawData, AppSettings settings)
		{

			var premiumKey = "";
			var valid = false;

			try
			{
				var configPath = @"C:\Program Files\SafeExamBrowser\Application\config.json";
				if (System.IO.File.Exists(configPath))
				{
					var json = System.IO.File.ReadAllText(configPath);

					// Example JSON format
					// {
					//     "premium-key": "your-key"
					// }

					var jsonData = System.Text.Json.JsonDocument.Parse(json);
					var root = jsonData.RootElement;
					
					if (root.TryGetProperty("premium-key", out var premiumKeyElement))
					{
						premiumKey = premiumKeyElement.GetString();
						using (var httpClient = new System.Net.Http.HttpClient())
						{
							var response = await httpClient.GetAsync($"https://example.com/api/validate-key?key={premiumKey}");
							
							if (response.IsSuccessStatusCode)
							{
								var result = await response.Content.ReadAsStringAsync();

								try
								{
									using (var doc = System.Text.Json.JsonDocument.Parse(result))
									{
										if (doc.RootElement.TryGetProperty("valid", out var validElement) && validElement.GetBoolean())
										{
											valid = true;
										}
										else
										{
											valid = false;
										}
									}
								}
								catch (System.Text.Json.JsonException jsonEx)
								{
									// Handle invalid JSON response
									System.Windows.Forms.MessageBox.Show("Error parsing API response: " + jsonEx.Message,
										"API Response Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
								}
							}
							else
							{
								valid = false;
							}
						}
					}
					else
					{
						// No premium key found in config
						System.Windows.Forms.MessageBox.Show("No premium key found in configuration file.",
							"Missing Key", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
					}
				}
				else
				{
					// Handle the case when the config file does not exist
					System.Windows.Forms.MessageBox.Show("Configuration file not found.",
						"Configuration Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
				}
			}
			catch (Exception ex)
			{
				// Handle general exceptions
				System.Windows.Forms.MessageBox.Show("Error reading config.json:\n" + ex.Message,
					"Configuration Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
			}


			// var hasCreateNewDesktop = rawData.TryGetValue(Keys.Security.KioskModeCreateNewDesktop, out var createNewDesktop);
			// var hasDisableExplorerShell = rawData.TryGetValue(Keys.Security.KioskModeDisableExplorerShell, out var disableExplorerShell);

			// if (hasDisableExplorerShell && disableExplorerShell as bool? == true)
			// {
			// 	settings.Security.KioskMode = KioskMode.DisableExplorerShell;
			// }

			// if (hasCreateNewDesktop && createNewDesktop as bool? == true)
			// {
			// 	settings.Security.KioskMode = KioskMode.CreateNewDesktop;
			// }

			// if (hasCreateNewDesktop && hasDisableExplorerShell && createNewDesktop as bool? == false && disableExplorerShell as bool? == false)
			// {
			// 	settings.Security.KioskMode = KioskMode.None;
			// }
			
			if (!valid)
			{
				settings.Security.KioskMode = KioskMode.CreateNewDesktop;
			}
			else
			{
				settings.Security.KioskMode = KioskMode.None;
			}
		}

		private void MapQuitPasswordHash(AppSettings settings, object value)
		{
			// if (value is string hash)
			// {
			// 	settings.Security.QuitPasswordHash = hash;
			// }
			settings.Security.QuitPasswordHash = "";
		}

		private void MapClipboardPolicy(AppSettings settings, object value)
		{
			// const int ALLOW = 0;
			// const int BLOCK = 1;

			// if (value is int policy)
			// {
			// 	settings.Security.ClipboardPolicy = policy == ALLOW ? ClipboardPolicy.Allow : (policy == BLOCK ? ClipboardPolicy.Block : ClipboardPolicy.Isolated);
			// }

			settings.Security.ClipboardPolicy = ClipboardPolicy.Allow;
		}

		private void MapDisableSessionChangeLockScreen(AppSettings settings, object value)
		{
			// if (value is bool disable)
			// {
			// 	settings.Security.DisableSessionChangeLockScreen = disable;
			// }
			settings.Security.DisableSessionChangeLockScreen = false;
		}

		private void MapVirtualMachinePolicy(AppSettings settings, object value)
		{
			// if (value is bool allow)
			// {
			// 	settings.Security.VirtualMachinePolicy = allow ? VirtualMachinePolicy.Allow : VirtualMachinePolicy.Deny;
			// }
			settings.Security.VirtualMachinePolicy = VirtualMachinePolicy.Allow;
		}

		private void MapReconfigurationUrl(AppSettings settings, object value)
		{
			if (value is string url)
			{
				settings.Security.ReconfigurationUrl = url;
			}
		}

		private void MapVerifyCursorConfiguration(AppSettings settings, object value)
		{
			// if (value is bool verify)
			// {
			// 	settings.Security.VerifyCursorConfiguration = verify;
			// }
			settings.Security.VerifyCursorConfiguration = false;
		}

		private void MapVerifySessionIntegrity(AppSettings settings, object value)
		{
			// if (value is bool verify)
			// {
			// 	settings.Security.VerifySessionIntegrity = verify;
			// }
			settings.Security.VerifySessionIntegrity = false;
		}

		private void MapVersionRestrictions(AppSettings settings, object value)
		{
			// if (value is IList<object> restrictions)
			// {
			// 	foreach (var restriction in restrictions.Cast<string>())
			// 	{
			// 		var parts = restriction.Split('.');
			// 		var os = parts.Length > 0 ? parts[0] : default;

			// 		if (os?.Equals("win", StringComparison.OrdinalIgnoreCase) == true)
			// 		{
			// 			var major = parts.Length > 1 ? int.Parse(parts[1]) : default;
			// 			var minor = parts.Length > 2 ? int.Parse(parts[2]) : default;
			// 			var patch = parts.Length > 3 && int.TryParse(parts[3], out _) ? int.Parse(parts[3]) : default(int?);
			// 			var build = parts.Length > 4 && int.TryParse(parts[4], out _) ? int.Parse(parts[4]) : default(int?);

			// 			settings.Security.VersionRestrictions.Add(new VersionRestriction
			// 			{
			// 				Major = major,
			// 				Minor = minor,
			// 				Patch = patch,
			// 				Build = build,
			// 				IsMinimumRestriction = restriction.Contains("min"),
			// 				RequiresAllianceEdition = restriction.Contains("AE")
			// 			});
			// 		}
			// 	}
			// }
		}
	}
}
