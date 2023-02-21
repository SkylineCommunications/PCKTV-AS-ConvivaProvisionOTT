/*
****************************************************************************
*  Copyright (c) 2023,  Skyline Communications NV  All Rights Reserved.    *
****************************************************************************

By using this script, you expressly agree with the usage terms and
conditions set out below.
This script and all related materials are protected by copyrights and
other intellectual property rights that exclusively belong
to Skyline Communications.

A user license granted for this script is strictly for personal use only.
This script may not be used in any way by anyone without the prior
written consent of Skyline Communications. Any sublicensing of this
script is forbidden.

Any modifications to this script by the user are only allowed for
personal use and within the intended purpose of the script,
and will remain the sole responsibility of the user.
Skyline Communications will not be responsible for any damages or
malfunctions whatsoever of the script resulting from a modification
or adaptation by the user.

The content of this script is confidential information.
The user hereby agrees to keep this confidential information strictly
secret and confidential and not to disclose or reveal it, in whole
or in part, directly or indirectly to any person, entity, organization
or administration without the prior written consent of
Skyline Communications.

Any inquiries can be addressed to:

	Skyline Communications NV
	Ambachtenstraat 33
	B-8870 Izegem
	Belgium
	Tel.	: +32 51 31 35 69
	Fax.	: +32 51 31 01 29
	E-mail	: info@skyline.be
	Web		: www.skyline.be
	Contact	: Ben Vandenberghe

****************************************************************************
Revision History:

DATE		VERSION		AUTHOR			COMMENTS

dd/mm/2023	1.0.0.1		XXX, Skyline	Initial version
****************************************************************************
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using ConvivaScripts;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Core.DataMinerSystem.Automation;
using Skyline.DataMiner.Core.DataMinerSystem.Common;
using Skyline.DataMiner.DataMinerSolutions.ProcessAutomation.Helpers.Logging;
using Skyline.DataMiner.DataMinerSolutions.ProcessAutomation.Manager;
using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;

/// <summary>
/// DataMiner Script Class.
/// </summary>
public class Script
{
	private DomHelper innerDomHelper;

	/// <summary>
	/// The Script entry point.
	/// </summary>
	/// <param name="engine">The <see cref="Engine" /> instance used to communicate with DataMiner.</param>
	public void Run(Engine engine)
	{
		var scriptName = "Deactivate Conviva";

		var helper = new PaProfileLoadDomHelper(engine);
		helper.Log($"START {scriptName}", PaLogLevel.Information);
		innerDomHelper = new DomHelper(engine.SendSLNetMessages, "process_automation");

		var domFilter = new ConvivaDomData
		{
			FilterName = helper.GetParameterValue<string>("Name"),
			ElementName = helper.GetParameterValue<string>("Conviva Element"),
			InstanceId = helper.GetParameterValue<string>("InstanceId"),
		};

		var instance = GetDomInstance(helper, domFilter);
		if (instance == null)
		{
			helper.SendErrorMessageToTokenHandler();
			return;
		}

		var status = instance.StatusId;

		if (!status.Equals("deactivate") && !status.Equals("reprovision"))
		{
			helper.Log("Skipping deactivation due to Conviva Status", PaLogLevel.Information);
			helper.ReturnSuccess();
			return;
		}

		try
		{
			IDms thisDms = engine.GetDms();
			var convivaElement = thisDms.GetElement(domFilter.ElementName);
			var filterTable = convivaElement.GetTable(2400);
			var metricLensTable = convivaElement.GetTable(700);

			bool CheckDeactivate()
			{
				try
				{
					if (CheckDeleteConvivaItems(domFilter.FilterName, filterTable, metricLensTable))
					{
						return true;
					}

					return false;
				}
				catch (Exception ex)
				{
					helper.Log($"Exception thrown while deleting conviva items: {ex}", PaLogLevel.Error);
					throw;
				}
			}

			if (Retry(CheckDeactivate, new TimeSpan(0, 5, 0)))
			{
				// successfully deactivation
				engine.GenerateInformation("Successfully deactivation for " + scriptName + " for: " + domFilter.ElementName);

				if (status.Equals("deactivate"))
				{
					helper.TransitionState("deactivate_to_complete");
					helper.SendFinishMessageToTokenHandler();
				}
				else if (status.Equals("reprovision"))
				{
					helper.TransitionState("reprovision_to_ready");
					helper.ReturnSuccess();
				}
				else
				{
					helper.Log($"Cannot execute the transition. Current status: {status}", PaLogLevel.Error);
					helper.SendErrorMessageToTokenHandler();
				}
			}
			else
			{
				// failed to create filter
				helper.Log($"Failed to detect deletion of {domFilter.FilterName} filter.", PaLogLevel.Error);
				helper.SendErrorMessageToTokenHandler();
			}
		}
		catch (ScriptAbortException)
		{
			// no issue
		}
		catch (Exception ex)
		{
			helper.Log($"An issue occurred while starting the conviva activity: {ex}", PaLogLevel.Error);
			helper.SendErrorMessageToTokenHandler();
		}
	}

	/// <summary>
	/// Retry until success or until timeout.
	/// </summary>
	/// <param name="func">Operation to retry.</param>
	/// <param name="timeout">Max TimeSpan during which the operation specified in <paramref name="func"/> can be retried.</param>
	/// <returns><c>true</c> if one of the retries succeeded within the specified <paramref name="timeout"/>. Otherwise <c>false</c>.</returns>
	private static bool Retry(Func<bool> func, TimeSpan timeout)
	{
		bool success = false;

		Stopwatch sw = new Stopwatch();
		sw.Start();

		do
		{
			success = func();
			if (!success)
			{
				Thread.Sleep(3000);
			}
		}
		while (!success && sw.Elapsed <= timeout);

		return success;
	}

	private bool CheckDeleteConvivaItems(string filterName, IDmsTable filterTable, IDmsTable metricLensTable)
	{
		bool filterDeleted = false;
		bool metricDeleted = false;

		var filterColumn = new ColumnFilter { ComparisonOperator = ComparisonOperator.Equal, Value = filterName, Pid = 2402 };
		var filterData = filterTable.QueryData(new List<ColumnFilter> { filterColumn });
		if (filterData != null && filterData.Any())
		{
			var key = Convert.ToString(filterData.First()[0]);
			var button = filterTable.GetColumn<int?>(2406);
			button.SetValue(key, 1);
		}
		else
		{
			filterDeleted = true;
		}

		var metricFilterColumn = new ColumnFilter { ComparisonOperator = ComparisonOperator.Equal, Value = filterName, Pid = 703 };
		var metricFiltered = metricLensTable.QueryData(new List<ColumnFilter> { metricFilterColumn });
		if (metricFiltered != null && metricFiltered.Any())
		{
			var key = Convert.ToString(metricFiltered.First()[0]);
			var button = metricLensTable.GetColumn<int?>(807);
			button.SetValue(key, 1);
		}
		else
		{
			metricDeleted = true;
		}

		return filterDeleted && metricDeleted;
	}

	private DomInstance GetDomInstance(PaProfileLoadDomHelper helper, ConvivaDomData domFilter)
	{
		try
		{
			return innerDomHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(new DomInstanceId(Guid.Parse(domFilter.InstanceId)))).First();
		}
		catch (Exception ex)
		{
			helper.Log($"Cannot get instanceId due to exception: {ex}", PaLogLevel.Error);
			throw;
		}
	}
}