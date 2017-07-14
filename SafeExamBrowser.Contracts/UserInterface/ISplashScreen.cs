﻿/*
 * Copyright (c) 2017 ETH Zürich, Educational Development and Technology (LET)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using SafeExamBrowser.Contracts.I18n;

namespace SafeExamBrowser.Contracts.UserInterface
{
	public interface ISplashScreen
	{
		/// <summary>
		/// Closes the splash screen.
		/// </summary>
		void Close();

		/// <summary>
		/// Set the maximum of the splash screen's progress bar.
		/// </summary>
		void SetMaxProgress(int max);

		/// <summary>
		/// Shows the splash screen to the user.
		/// </summary>
		void Show();

		/// <summary>
		/// Updates the progress bar of the splash screen according to the specified amount.
		/// </summary>
		void UpdateProgress(int amount = 1);

		/// <summary>
		/// Updates the status text of the splash screen.
		/// </summary>
		void UpdateText(Key key);
	}
}