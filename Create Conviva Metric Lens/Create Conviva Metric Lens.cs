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
using Skyline.DataMiner.DataMinerSolutions.ProcessAutomation.Helpers.Logging;
using Skyline.DataMiner.DataMinerSolutions.ProcessAutomation.Manager;
using Skyline.DataMiner.Library.Automation;
using Skyline.DataMiner.Library.Common;
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
		var scriptName = "Create Conviva Metric Lens";

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

		if (!status.Equals("in_progress"))
		{
			helper.SendErrorMessageToTokenHandler();
			return;
		}

		try
		{
			IDms thisDms = engine.GetDms();
			var convivaElement = thisDms.GetElement(domFilter.ElementName);
			var metricLensTable = convivaElement.GetTable(ConvivaElementInfo.MetricLensTable);
			bool metricLensExist = CreateMetricLens(helper, domFilter.FilterName, convivaElement, metricLensTable);
			if (metricLensExist)
			{
				helper.Log($"MetricLens already exist for filterName {domFilter.FilterName}. Skip creation.", PaLogLevel.Information);
				helper.TransitionState("inprogress_to_active");
				helper.SendFinishMessageToTokenHandler();
				return;
			}

			CheckMetricLens(helper, domFilter.FilterName, metricLensTable);

			bool MetricLensCreated()
			{
				try
				{
					var metricFilterColumn = new ColumnFilter { ComparisonOperator = ComparisonOperator.Equal, Value = domFilter.FilterName, Pid = 703 };
					var metricFiltered = metricLensTable.QueryData(new List<ColumnFilter> { metricFilterColumn });
					if (metricFiltered == null || !metricFiltered.Any())
					{
						return false;
					}

					return true;
				}
				catch (Exception ex)
				{
					helper.Log($"Exception thrown while checking conviva metric lens status: {ex}", PaLogLevel.Error);
					throw;
				}
			}

			if (Retry(MetricLensCreated, new TimeSpan(0, 5, 0)))
			{
				// successfully created filter
				helper.Log($"Successfully executed {scriptName} for: {domFilter.FilterName}", PaLogLevel.Information);
				helper.TransitionState("inprogress_to_active");
				helper.SendFinishMessageToTokenHandler();
			}
			else
			{
				// failed to create metric lens
				helper.Log($"Unable to detect MetricLens creation for {domFilter.FilterName}", PaLogLevel.Error);
				helper.SendErrorMessageToTokenHandler();
			}
		}
		catch (Exception ex)
		{
			helper.Log($"Create Conviva MetricLens|{domFilter.FilterName}|Exception during execution: {ex}", PaLogLevel.Error);
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

	private bool CreateMetricLens(PaProfileLoadDomHelper helper, string filterName, IDmsElement convivaElement, IDmsTable metricLensTable)
	{
		try
		{
			var metricLensColumn = new ColumnFilter { ComparisonOperator = ComparisonOperator.Equal, Value = filterName, Pid = 799 };
			var metricsLensData = metricLensTable.QueryData(new List<ColumnFilter> { metricLensColumn });
			if (metricsLensData.Any())
			{
				// metric lens has already been created
				return true;
			}

			convivaElement.GetStandaloneParameter<int?>(51).SetValue(0);

			// wait 3s to create row
			Thread.Sleep(3000);
			var keys = metricLensTable.GetPrimaryKeys();
			var highestKey = Convert.ToString(keys.Select(x => Convert.ToInt32(x)).Max());
			metricLensTable.SetRow(highestKey, new object[] { highestKey, "Assets", filterName, null, "Quality MetricLens", 1 });

			return false;
		}
		catch (Exception ex)
		{
			helper.Log($"|CreateMetricLens|Error creating Conviva Metric lens: {ex}", PaLogLevel.Error);
			return false;
		}
	}

	private void CheckMetricLens(PaProfileLoadDomHelper helper, string filterName, IDmsTable metricLensTable)
	{
		try
		{
			var metricFilterColumn = new ColumnFilter { ComparisonOperator = ComparisonOperator.Equal, Value = filterName, Pid = 703 };
			var metricFiltered = metricLensTable.QueryData(new List<ColumnFilter> { metricFilterColumn });
			if (metricFiltered == null || !metricFiltered.Any())
			{
				return;
			}

			var firstRow = metricFiltered.First();
			var key = Convert.ToString(firstRow[0]);
			var metricState = (MetricLensState)Convert.ToInt32(firstRow[5]);
			var lensStatus = (MetricLensStatus)Convert.ToInt32(firstRow[3]);
			if (metricState == MetricLensState.Disabled && lensStatus != MetricLensStatus.OK && lensStatus != MetricLensStatus.WarmUp)
			{
				// Enable state
				var state = metricLensTable.GetColumn<int?>(706);
				state.SetValue(key, (int)MetricLensState.Enabled);

				// Poll Now
				var button = metricLensTable.GetColumn<int?>(808);
				button.SetValue(key, 1);
			}
			else if (metricState == MetricLensState.Enabled && (lensStatus == MetricLensStatus.OK || lensStatus == MetricLensStatus.WarmUp))
			{
				// metric lens fully set up
				helper.Log($"MetricLens created for {filterName}", PaLogLevel.Information);
			}
			else
			{
				// no action
			}
		}
		catch (Exception ex)
		{
			helper.Log($"|CheckMetricLens|Error creating Conviva Metric Lens: {ex}", PaLogLevel.Error);
		}
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