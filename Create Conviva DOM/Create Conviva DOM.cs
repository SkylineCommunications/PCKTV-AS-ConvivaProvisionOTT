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
using System.Globalization;
using System.Linq;
using System.Text;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Actions;
using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Conditions;
using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Status;
using Skyline.DataMiner.Net.Apps.Sections.SectionDefinitions;
using Skyline.DataMiner.Net.GenericEnums;
using Skyline.DataMiner.Net.Messages.SLDataGateway;
using Skyline.DataMiner.Net.Sections;

/// <summary>
/// DataMiner Script Class.
/// </summary>
public class Script
{
	private DomHelper domHelper;

	/// <summary>
	/// The Script entry point.
	/// </summary>
	/// <param name="engine">The <see cref="Engine" /> instance used to communicate with DataMiner.</param>
	public void Run(Engine engine)
	{
		var scriptName = "Setup Conviva DOM";
		engine.GenerateInformation("START " + scriptName);
		domHelper = new DomHelper(engine.SendSLNetMessages, "process_automation");
		try
		{
			// Conviva
			var peacockProvisionDomDefinition = CreateDomDefinition();
			if (peacockProvisionDomDefinition != null)
			{
				var domDefinition = domHelper.DomDefinitions.Read(DomDefinitionExposers.Name.Equal(peacockProvisionDomDefinition.Name));
				if (domDefinition.Any())
				{
					peacockProvisionDomDefinition.ID = domDefinition.FirstOrDefault()?.ID;
					domHelper.DomDefinitions.Update(peacockProvisionDomDefinition);
				}
				else
				{
					domHelper.DomDefinitions.Create(peacockProvisionDomDefinition);
				}
			}
		}
		catch (Exception ex)
		{
			engine.Log(scriptName + $"|Failed to execute the TAG Monitor due to exception: " + ex);
		}
	}

	private DomDefinition CreateDomDefinition()
	{
		// Create SectionDefinitions
		var filterSectionDefinition = SectionDefinitions.CreateFilterSection(domHelper);
		var rulesSectionDefinition = SectionDefinitions.CreateRulesSection(domHelper);

		var sections = new List<SectionDefinition> { filterSectionDefinition, rulesSectionDefinition };

		// Create DomBehaviorDefinition
		var behavior = domHelper.DomBehaviorDefinitions.Read(DomBehaviorDefinitionExposers.Name.Equal("Conviva Behavior"));
		if (!behavior.Any())
		{
			var domBehaviorDefinition = BehaviorDefinitions.CreateDomBehaviorDefinition(sections);
			domBehaviorDefinition = domHelper.DomBehaviorDefinitions.Create(domBehaviorDefinition);
			behavior = new List<DomBehaviorDefinition> { domBehaviorDefinition };
		}

		var filterSectionInfo = new SectionDefinitionInfo
		{
			AllowMultipleInstances = false,
			SectionDefinitionID = filterSectionDefinition.GetID(),
		};

		var rulesSectionInfo = new SectionDefinitionInfo
		{
			AllowMultipleInstances = true,
			SectionDefinitionID = rulesSectionDefinition.GetID(),
		};

		var sectionDefinitionInfos = new List<SectionDefinitionInfo> { filterSectionInfo, rulesSectionInfo };

		return new DomDefinition
		{
			Name = "Conviva",
			SectionDefinitionLinks = new List<SectionDefinitionLink> { new SectionDefinitionLink(filterSectionDefinition.GetID()), new SectionDefinitionLink(rulesSectionDefinition.GetID()) },
			DomBehaviorDefinitionId = behavior.FirstOrDefault()?.ID,
			VisualStructure = new DomDefinitionVisualStructure { SectionDefinitionInfos = sectionDefinitionInfos },
		};
	}

	public class SectionDefinitions
	{
		public static SectionDefinition CreateRulesSection(DomHelper domHelper)
		{
			var operationEnum = new GenericEnum<string>();
			operationEnum.AddEntry("Equals", "equals");
			operationEnum.AddEntry("Not Equals", "notequals");
			operationEnum.AddEntry("Contains", "contains");
			operationEnum.AddEntry("Not Contains", "notcontains");

			var operationFieldDescriptor = CreateEnumFieldDescriptorObject("Operation", "What type of search will Conviva perform with this rule.", operationEnum);
			var valueFieldDescriptor = CreateFieldDescriptorObject<string>("Value", "Link to the DOM Instance that contains the information for TAG provisioning.");
			var keyFieldDescriptor = CreateFieldDescriptorObject<string>("Key", "Rules Key.");
			var touchstreamFieldDescriptor = CreateFieldDescriptorObject<string>("Group", "Groups rule conditions. Rules with the same Group value will be included as an OR. Each unique Group value will be separated from other rule Groups with an AND. ");

			List<FieldDescriptor> fieldDescriptors = new List<FieldDescriptor>
				{
					operationFieldDescriptor,
					valueFieldDescriptor,
					keyFieldDescriptor,
					touchstreamFieldDescriptor,
				};

			var domInstanceSection = CreateOrUpdateSection("Rules", domHelper, fieldDescriptors);

			return domInstanceSection;
		}

		public static SectionDefinition CreateFilterSection(DomHelper domHelper)
		{
			var typeEnum = new GenericEnum<string>();
			typeEnum.AddEntry("create_filter", "create_filter");

			var convivaElementFieldDescriptor = CreateFieldDescriptorObject<string>("Conviva Element", "The name(not ELEMID) of the Conviva Element that should handle this process.");
			var typeFieldDescriptor = CreateEnumFieldDescriptorObject("Type", "The type of operation Conviva should perform for this filter.", typeEnum, new ValueWrapper<string>("create_filter"));
			var convivaNameFieldDescriptor = CreateFieldDescriptorObject<string>("Conviva Name", "Unique ID to link the provision to an Event or Channel.");
			var categoryFieldDescriptor = CreateFieldDescriptorObject<string>("Category", "Category of the Conviva filter.");
			var subcategoryFieldDescriptor = CreateFieldDescriptorObject<string>("Subcategory", "Subcategory of the Conviva filter.");
			var enabledFieldDescriptor = CreateFieldDescriptorObject<bool>("Enabled", "Indicates if the filter will be active or inactive.", new ValueWrapper<bool>(true));

			List<FieldDescriptor> fieldDescriptors = new List<FieldDescriptor>
				{
					convivaElementFieldDescriptor,
					typeFieldDescriptor,
					convivaNameFieldDescriptor,
					categoryFieldDescriptor,
					subcategoryFieldDescriptor,
					enabledFieldDescriptor,
				};

			var provisionInfoSection = CreateOrUpdateSection("Filter", domHelper, fieldDescriptors);

			return provisionInfoSection;
		}

		private static SectionDefinition CreateOrUpdateSection(string name, DomHelper domHelper, List<FieldDescriptor> fieldDescriptors)
		{
			var domInstancesSectionDefinition = new CustomSectionDefinition
			{
				Name = name,
			};

			var domInstanceSection = domHelper.SectionDefinitions.Read(SectionDefinitionExposers.Name.Equal(domInstancesSectionDefinition.Name));
			SectionDefinition sectionDefinition;
			if (!domInstanceSection.Any())
			{
				foreach (var field in fieldDescriptors)
				{
					domInstancesSectionDefinition.AddOrReplaceFieldDescriptor(field);
				}

				sectionDefinition = domHelper.SectionDefinitions.Create(domInstancesSectionDefinition) as CustomSectionDefinition;
			}
			else
			{
				// Update Section Definition (Add missing fieldDescriptors)
				sectionDefinition = UpdateSectionDefinition(domHelper, fieldDescriptors, domInstanceSection);
			}

			return sectionDefinition;
		}

		private static SectionDefinition UpdateSectionDefinition(DomHelper domHelper, List<FieldDescriptor> fieldDescriptorList, List<SectionDefinition> sectionDefinition)
		{
			var existingSectionDefinition = sectionDefinition.First() as CustomSectionDefinition;
			var previousFieldNames = existingSectionDefinition.GetAllFieldDescriptors().Select(x => x.Name).ToList();
			List<FieldDescriptor> fieldDescriptorsToAdd = new List<FieldDescriptor>();

			// Check if there's a fieldDefinition to add
			foreach (var newfieldDescriptor in fieldDescriptorList)
			{
				if (!previousFieldNames.Contains(newfieldDescriptor.Name))
				{
					fieldDescriptorsToAdd.Add(newfieldDescriptor);
				}
			}

			if (fieldDescriptorsToAdd.Count > 0)
			{
				foreach (var field in fieldDescriptorsToAdd)
				{
					existingSectionDefinition.AddOrReplaceFieldDescriptor(field);
				}

				existingSectionDefinition = domHelper.SectionDefinitions.Update(existingSectionDefinition) as CustomSectionDefinition;
			}

			return existingSectionDefinition;
		}

		private static FieldDescriptor CreateFieldDescriptorObject<T>(string fieldName, string toolTip)
		{
			return new FieldDescriptor
			{
				FieldType = typeof(T),
				Name = fieldName,
				Tooltip = toolTip,
			};
		}

		private static FieldDescriptor CreateFieldDescriptorObject<T>(string fieldName, string toolTip, IValueWrapper defaultValue)
		{
			return new FieldDescriptor
			{
				FieldType = typeof(T),
				Name = fieldName,
				Tooltip = toolTip,
				DefaultValue = defaultValue,
			};
		}

		private static FieldDescriptor CreateEnumFieldDescriptorObject(string fieldName, string toolTip, GenericEnum<string> discreets)
		{
			return new GenericEnumFieldDescriptor
			{
				FieldType = typeof(GenericEnum<string>),
				Name = fieldName,
				Tooltip = toolTip,
				GenericEnumInstance = discreets,
			};
		}

		private static FieldDescriptor CreateEnumFieldDescriptorObject(string fieldName, string toolTip, GenericEnum<string> discreets, IValueWrapper defaultValue)
		{
			return new GenericEnumFieldDescriptor
			{
				FieldType = typeof(GenericEnum<string>),
				Name = fieldName,
				Tooltip = toolTip,
				GenericEnumInstance = discreets,
				DefaultValue = defaultValue,
			};
		}

		private static FieldDescriptor CreateEnumFieldDescriptorObject(string fieldName, string toolTip, GenericEnum<int> discreets, IValueWrapper defaultValue)
		{
			return new GenericEnumFieldDescriptor
			{
				FieldType = typeof(GenericEnum<int>),
				Name = fieldName,
				Tooltip = toolTip,
				GenericEnumInstance = discreets,
				DefaultValue = defaultValue,
			};
		}

		private static DomInstanceFieldDescriptor CreateDomInstanceFieldDescriptorObject<T>(string fieldName, string toolTip)
		{
			return new DomInstanceFieldDescriptor
			{
				FieldType = typeof(T),
				Name = fieldName,
				Tooltip = toolTip,
			};
		}
	}

	public class BehaviorDefinitions
	{
		public static DomBehaviorDefinition CreateDomBehaviorDefinition(List<SectionDefinition> sections)
		{
			var statuses = new List<DomStatus>
					{
						new DomStatus("draft", "Draft"),
						new DomStatus("ready", "Ready"),
						new DomStatus("in_progress", "In Progress"),
						new DomStatus("active", "Active"),
						new DomStatus("deactivate", "Deactivate"),
						new DomStatus("reprovision", "Reprovision"),
						new DomStatus("complete", "Complete"),
					};

			var transitions = new List<DomStatusTransition>
					{
						new DomStatusTransition("draft_to_ready", "draft", "ready"),
						new DomStatusTransition("ready_to_inprogress", "ready", "in_progress"),
						new DomStatusTransition("inprogress_to_active", "in_progress", "active"),
						new DomStatusTransition("active_to_deactivate", "active", "deactivate"),
						new DomStatusTransition("active_to_reprovision", "active", "reprovision"),
						new DomStatusTransition("deactivate_to_complete", "deactivate", "complete"),
						new DomStatusTransition("reprovision_to_inprogress", "reprovision", "in_progress"),
						new DomStatusTransition("complete_to_ready", "complete", "ready"),
					};

			var scriptAction = new ExecuteScriptDomActionDefinition("start_process")
			{
				Async = false,
				IsInteractive = false,
				Script = "start_subprocess",
				ScriptOptions = new List<string> { "Conviva" },
				Condition = new StatusCondition(new List<string> { "draft", "ready" }),
			};

			return new DomBehaviorDefinition
			{
				Name = "Conviva Behavior",
				InitialStatusId = "draft",
				Statuses = statuses,
				StatusTransitions = transitions,
				StatusSectionDefinitionLinks = GetStatusLinks(sections),
				ActionDefinitions = new List<IDomActionDefinition> { scriptAction },
			};
		}

		private static List<DomStatusSectionDefinitionLink> GetStatusLinks(List<SectionDefinition> sections)
		{
			Dictionary<string, List<FieldDescriptorID>> fieldsList = GetFieldDescriptorDictionary(sections);

			var draftStatusLinks = StatusSectionDefinitions.GetSectionDefinitionLinks(sections, fieldsList, "draft", false);
			var readyStatusLinks = StatusSectionDefinitions.GetSectionDefinitionLinks(sections, fieldsList, "ready", true);
			var inprogressStatusLinks = StatusSectionDefinitions.GetSectionDefinitionLinks(sections, fieldsList, "in_progress", true);
			var activeStatusLinks = StatusSectionDefinitions.GetSectionDefinitionLinks(sections, fieldsList, "active", true);
			var deactivateStatusLinks = StatusSectionDefinitions.GetSectionDefinitionLinks(sections, fieldsList, "deactivate", true);
			var reprovisionStatusLinks = StatusSectionDefinitions.GetSectionDefinitionLinks(sections, fieldsList, "reprovision", true);
			var completeStatusLinks = StatusSectionDefinitions.GetSectionDefinitionLinks(sections, fieldsList, "complete", true);

			return draftStatusLinks.Concat(readyStatusLinks).Concat(inprogressStatusLinks).Concat(activeStatusLinks).Concat(deactivateStatusLinks).Concat(reprovisionStatusLinks).Concat(completeStatusLinks).ToList();
		}

		private static Dictionary<string, List<FieldDescriptorID>> GetFieldDescriptorDictionary(List<SectionDefinition> sections)
		{
			Dictionary<string, List<FieldDescriptorID>> fieldsList = new Dictionary<string, List<FieldDescriptorID>>();
			foreach (var section in sections)
			{
				var fields = section.GetAllFieldDescriptors();
				foreach (var field in fields)
				{
					var sectionName = section.GetName();
					if (!fieldsList.ContainsKey(sectionName))
					{
						fieldsList[sectionName] = new List<FieldDescriptorID>();
					}

					fieldsList[sectionName].Add(field.ID);
				}
			}

			return fieldsList;
		}

		public class StatusSectionDefinitions
		{
			public static List<DomStatusSectionDefinitionLink> GetSectionDefinitionLinks(List<SectionDefinition> sections, Dictionary<string, List<FieldDescriptorID>> fieldsList, string status, bool readOnly)
			{
				var sectionLinks = new List<DomStatusSectionDefinitionLink>();
				foreach (var section in sections)
				{
					var statusLinkId = new DomStatusSectionDefinitionLinkId(status, section.GetID());

					var statusLink = new DomStatusSectionDefinitionLink(statusLinkId);

					foreach (var fieldId in fieldsList[section.GetName()])
					{
						statusLink.FieldDescriptorLinks.Add(new DomStatusFieldDescriptorLink(fieldId)
						{
							Visible = true,
							ReadOnly = readOnly,
							RequiredForStatus = true,
						});
					}

					sectionLinks.Add(statusLink);
				}

				return sectionLinks;
			}
		}
	}
}