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
using Newtonsoft.Json;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Core.DataMinerSystem.Automation;
using Skyline.DataMiner.Core.DataMinerSystem.Common;
using Skyline.DataMiner.DataMinerSolutions.ProcessAutomation.Helpers.Logging;
using Skyline.DataMiner.DataMinerSolutions.ProcessAutomation.Manager;
using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
using Skyline.DataMiner.Net.Sections;

/// <summary>
/// DataMiner Script Class.
/// </summary>
public class CreateConvivaFilterScript
{
	private DomHelper innerDomHelper;

	/// <summary>
	/// The Script entry point.
	/// </summary>
	/// <param name="engine">The <see cref="Engine" /> instance used to communicate with DataMiner.</param>
	public void Run(Engine engine)
	{
		var scriptName = "Create Conviva Filter";

		var helper = new PaProfileLoadDomHelper(engine);
		helper.Log($"START {scriptName}", PaLogLevel.Information);
		innerDomHelper = new DomHelper(engine.SendSLNetMessages, "process_automation");

		var domFilter = new ConvivaDomData
		{
			FilterName = helper.GetParameterValue<string>("Name"),
			ElementName = helper.GetParameterValue<string>("Conviva Element"),
			Type = helper.GetParameterValue<string>("Type"),
			Category = helper.GetParameterValue<string>("Category"),
			Subcategory = helper.GetParameterValue<string>("Subcategory"),
			Enabled = helper.GetParameterValue<string>("Enabled"),
			InstanceId = helper.GetParameterValue<string>("InstanceId"),
		};

		var instance = GetDomInstance(helper, domFilter);
		if (instance == null)
		{
			helper.SendErrorMessageToTokenHandler();
			return;
		}

		var status = instance.StatusId;

		if (!status.Equals("ready"))
		{
			helper.SendErrorMessageToTokenHandler();
			return;
		}

		try
		{
			IDms thisDms = engine.GetDms();
			var convivaElement = thisDms.GetElement(domFilter.ElementName);
			var filterTable = convivaElement.GetTable(ConvivaElementInfo.FilterTable);
			var filterColumn = new ColumnFilter { ComparisonOperator = ComparisonOperator.Equal, Value = domFilter.FilterName, Pid = 2402 };
			var filterData = filterTable.QueryData(new List<ColumnFilter> { filterColumn });

			if (filterData.Any())
			{
				// filter already created
				// skip over retry section later
				helper.Log($"Filter ({domFilter.FilterName}) already exists, skipping creation.", PaLogLevel.Information);
			}
			else
			{
				var rules = GetRuleByFilter(engine, instance);
				ConvivaFilterRequest newFilterRequest = GetCreateConvivaRequest(domFilter, rules);
				var convivaParam = convivaElement.GetStandaloneParameter<string>(ConvivaElementInfo.FilterListener);
				convivaParam.SetValue(JsonConvert.SerializeObject(newFilterRequest));
			}

			bool CheckFilter()
			{
				try
				{
					var retryFilterData = filterTable.QueryData(new List<ColumnFilter> { filterColumn });

					if (retryFilterData.Any())
					{
						return true;
					}

					return false;
				}
				catch (Exception ex)
				{
					helper.Log($"Exception thrown while checking conviva filter status: {ex}", PaLogLevel.Error);
					throw;
				}
			}

			if (Retry(CheckFilter, new TimeSpan(0, 5, 0)))
			{
				// successfully created filter
				helper.TransitionState("ready_to_inprogress");

				helper.Log($"Successfully executed {scriptName} for: {domFilter.ElementName}", PaLogLevel.Information);
				helper.ReturnSuccess();
			}
			else
			{
				// failed to create filter
				helper.Log($"Failed to detect creation of {domFilter.FilterName} filter.", PaLogLevel.Error);
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
	public static bool Retry(Func<bool> func, TimeSpan timeout)
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

	public List<RulesRule> GetRuleByFilter(Engine engine, DomInstance instance)
	{
		Dictionary<string, List<RulesSectionDefinition>> rules = new Dictionary<string, List<RulesSectionDefinition>>();

		// Get Rules Sections
		foreach (var section in instance.Sections)
		{
			Func<SectionDefinitionID, SectionDefinition> sectionDefinitionFunc = SetSectionDefinitionById;
			section.Stitch(sectionDefinitionFunc);

			if (!section.GetSectionDefinition().GetName().Equals("Rules"))
			{
				continue;
			}

			var rule = new RulesSectionDefinition();
			foreach (var field in section.FieldValues)
			{
				switch (field.GetFieldDescriptor().Name)
				{
					case "Operation":
						rule.Operation = field.Value.ToString();
						break;
					case "Key":
						rule.Key = field.Value.ToString();
						break;
					case "Value":
						rule.Value = field.Value.ToString();
						break;
					case "Group":
						rule.Group = field.Value.ToString();
						break;
					case "Field":
						rule.Field = field.Value.ToString();
						break;
					default:
						engine.GenerateInformation($"FieldDescriptor not available. FieldDescriptor name: {field.GetFieldDescriptor().Name}");
						break;
				}
			}

			if (rules.ContainsKey(rule.Group))
			{
				rules[rule.Group].Add(rule);
			}
			else
			{
				rules.Add(rule.Group, new List<RulesSectionDefinition> { rule });
			}
		}

		return CreateFilterRules(rules);
	}

	public List<RulesRule> CreateFilterRules(Dictionary<string, List<RulesSectionDefinition>> rules)
	{
		if (rules.Count == 0)
		{
			return new List<RulesRule>
				{
					new RulesRule
					{
						Op = "or",
						Rules = new List<Rule>(),
					},
				};
		}

		List<RulesRule> andRules = new List<RulesRule>();
		foreach (var rule in rules)
		{
			var andRuleOption = new RulesRule();
			List<Rule> orrulesList = new List<Rule>();
			foreach (var orrule in rule.Value)
			{
				orrulesList.Add(new Rule
				{
					Field = orrule.Field,
					Key = String.IsNullOrWhiteSpace(orrule.Key) ? null : orrule.Key,
					Op = orrule.Operation.ToLowerInvariant(),
					Value = orrule.Value,
				});
			}

			andRuleOption.Op = "or";
			andRuleOption.Rules = orrulesList;

			andRules.Add(andRuleOption);
		}

		return andRules;
	}

	private ConvivaFilterRequest GetCreateConvivaRequest(ConvivaDomData domFilter, List<RulesRule> rules)
	{
		return new ConvivaFilterRequest
		{
			Type = domFilter.Type,
			Request = new Request
			{
				Name = domFilter.FilterName,
				Category = domFilter.Category,
				Subcategory = domFilter.Subcategory,
				Enabled = domFilter.Enabled,
				Advanced = true,
				Rules = new Rules
				{
					Op = "and",
					RulesRules = rules,
				},
			},
		};
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

	private SectionDefinition SetSectionDefinitionById(SectionDefinitionID sectionDefinitionId)
	{
		return innerDomHelper.SectionDefinitions.Read(SectionDefinitionExposers.ID.Equal(sectionDefinitionId)).First();
	}
}