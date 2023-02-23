using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Actions;
using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Conditions;
using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Status;
using Skyline.DataMiner.Net.Apps.Sections.SectionDefinitions;
using Skyline.DataMiner.Net.GenericEnums;
using Skyline.DataMiner.Net.Messages.SLDataGateway;
using Skyline.DataMiner.Net.Sections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    [TestClass()]
    public class ScriptTests
    {
        [TestMethod()]
        public void GetRuleByFilterTest()
        {
            Mock<Engine> fakeEngine = new Mock<Engine>();
            var engine = fakeEngine.Object;
            var scriptClass = new CreateConvivaFilterScript();
            var dom = new Dom();

            var domHelper = new DomHelper(engine.SendSLNetMessages, "process_automation");
            var isDomCreated = dom.CreateDom(engine, domHelper);
            if (!isDomCreated)
            {
                return;
            }            

            var domInstance = dom.CreateDomInstance(domHelper);

            var rulesList = scriptClass.GetRuleByFilter(engine, domInstance);

            Assert.IsNotNull(rulesList);
        }
    }

    public class Dom
    {
        public DomInstance CreateDomInstance(DomHelper domHelper)
        {
            // SectionDefinitions
            var filterSectionDefinitionId = new SectionDefinitionID(Guid.Parse("9f69fdde-e84e-44ef-bb84-f4ec3b7e03d4"));
            var rulesSectionDefinitionId = new SectionDefinitionID(Guid.Parse("de83b440-fe18-4ca5-acea-c35d0c3d9cfa"));

            // Fields
            var convivaElementFieldDescriptorId = new FieldDescriptorID(Guid.Parse("52db26ba-de97-4354-8692-fd9fdcb84dba"));
            var typeFieldDescriptorId = new FieldDescriptorID(Guid.Parse("f9fc8955-1d63-4c80-a5fa-13708d41ea67"));
            var convivaNameFieldDescriptorId = new FieldDescriptorID(Guid.Parse("5768a20b-9398-4472-8378-c29caa0c1cd5"));
            var categoryFieldDescriptorId = new FieldDescriptorID(Guid.Parse("61760703-5095-4780-96e4-543c57758105"));
            var subcategoryFieldDescriptorId = new FieldDescriptorID(Guid.Parse("cd5ae5cc-d1c1-490c-8058-93eea62658f9"));
            var enabledFieldDescriptorId = new FieldDescriptorID(Guid.Parse("504b2817-c11a-4174-ae68-ff0661314e27"));
            var instanceIdFieldDescriptorId = new FieldDescriptorID(Guid.Parse("efa78ff1-9347-49eb-8200-907f6489ed11"));

            var fieldFieldDescriptorId = new FieldDescriptorID(Guid.Parse("4366c511-deb2-4859-b07b-50240586890b"));
            var keyFieldDescriptorId = new FieldDescriptorID(Guid.Parse("476d740b-59f6-459b-92d4-3fac099935d5"));
            var operatorFieldDescriptorId = new FieldDescriptorID(Guid.Parse("5f8b81bd-4ef4-4344-916a-b919a665ce1b"));
            var valueFieldDescriptorId = new FieldDescriptorID(Guid.Parse("c2ec7629-67b8-4090-b9f8-94093609ecd4"));
            var groupFieldDescriptorId = new FieldDescriptorID(Guid.Parse("5e864ce1-a84e-4cc6-a3fe-26a844c3d857"));

            // DomDefinition
            var domDefinitionId = new DomDefinitionId(Guid.Parse("269725f0-400b-45d2-97cf-30cd05e0122b"));

            // Add values to fields
            var filterFieldValues = new List<FieldValue>
            {
                new FieldValue(convivaElementFieldDescriptorId, new ValueWrapper<string>("Conviva Test Platform - Test")),
                new FieldValue(typeFieldDescriptorId, new ValueWrapper<string>("create_filter")),
                new FieldValue(convivaNameFieldDescriptorId, new ValueWrapper<string>("Unit Test Conviva")),
                new FieldValue(categoryFieldDescriptorId, new ValueWrapper<string>("CONTENT")),
                new FieldValue(subcategoryFieldDescriptorId, new ValueWrapper<string>("Asset")),
                new FieldValue(enabledFieldDescriptorId, new ValueWrapper<string>("true")),
                new FieldValue(instanceIdFieldDescriptorId, new ValueWrapper<string>("")),
            };

            var filterSection = new Section { SectionDefinitionID = filterSectionDefinitionId };
            foreach (var fieldValue in filterFieldValues)
            {
                filterSection.AddOrReplaceFieldValue(fieldValue);
            }

            var rulesFieldValues = new List<FieldValue>
            {
                new FieldValue(fieldFieldDescriptorId, new ValueWrapper<string>("Asset Name")),
                new FieldValue(keyFieldDescriptorId, new ValueWrapper<string>("")),
                new FieldValue(operatorFieldDescriptorId, new ValueWrapper<string>("contains")),
                new FieldValue(valueFieldDescriptorId, new ValueWrapper<string>("Live-1234")),
                new FieldValue(groupFieldDescriptorId, new ValueWrapper<string>("1")),
            };

            var rulesSection = new Section { SectionDefinitionID = rulesSectionDefinitionId };
            foreach (var fieldValue in rulesFieldValues)
            {
                rulesSection.AddOrReplaceFieldValue(fieldValue);
            }

            var rulesFieldValues2 = new List<FieldValue>
            {
                new FieldValue(fieldFieldDescriptorId, new ValueWrapper<string>("Asset Name")),
                new FieldValue(keyFieldDescriptorId, new ValueWrapper<string>("")),
                new FieldValue(operatorFieldDescriptorId, new ValueWrapper<string>("contains")),
                new FieldValue(valueFieldDescriptorId, new ValueWrapper<string>("Live-1234")),
                new FieldValue(groupFieldDescriptorId, new ValueWrapper<string>("1")),
            };

            var rulesSection2 = new Section { SectionDefinitionID = rulesSectionDefinitionId };
            foreach (var fieldValue in rulesFieldValues2)
            {
                rulesSection2.AddOrReplaceFieldValue(fieldValue);
            }

            var domInstance = new DomInstance()
            {
                DomDefinitionId = domDefinitionId
            };

            // Add all Sections
            domInstance.Sections.Add(filterSection);
            domInstance.Sections.Add(rulesSection);
            domInstance.Sections.Add(rulesSection2);

            // Save the DomInstance to the DomManager
            domInstance = domHelper.DomInstances.Create(domInstance);

            return domInstance;
        }

        public bool CreateDom(Engine engine, DomHelper domHelper)
        {
            try
            {
                // Conviva
                var peacockProvisionDomDefinition = CreateDomDefinition(domHelper);
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

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                engine.Log($"|Failed to create Conviva DOM due to exception: " + ex);
                return false;
            }
        }

        private DomDefinition CreateDomDefinition(DomHelper domHelper)
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
				ID = new DomDefinitionId(Guid.Parse("269725f0-400b-45d2-97cf-30cd05e0122b")),
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

				var operationFieldDescriptor = CreateEnumFieldDescriptorObject("Operation", "What type of search will Conviva perform with this rule.", "5f8b81bd-4ef4-4344-916a-b919a665ce1b", operationEnum);
				var valueFieldDescriptor = CreateFieldDescriptorObject<string>("Value", "Link to the DOM Instance that contains the information for TAG provisioning.", "c2ec7629-67b8-4090-b9f8-94093609ecd4");
				var keyFieldDescriptor = CreateFieldDescriptorObject<string>("Key", "Rules Key.", "476d740b-59f6-459b-92d4-3fac099935d5");
				var groupFieldDescriptor = CreateFieldDescriptorObject<string>("Group", "Groups rule conditions. Rules with the same Group value will be included as an OR. Each unique Group value will be separated from other rule Groups with an AND. ", "5e864ce1-a84e-4cc6-a3fe-26a844c3d857");
				var fieldFieldDescriptor = CreateFieldDescriptorObject<string>("Field", "Field Value.", "4366c511-deb2-4859-b07b-50240586890b");

				List<FieldDescriptor> fieldDescriptors = new List<FieldDescriptor>
				{
					operationFieldDescriptor,
					valueFieldDescriptor,
					keyFieldDescriptor,
					groupFieldDescriptor,
					fieldFieldDescriptor,
				};

				var domInstanceSection = CreateOrUpdateSection("Rules", domHelper, fieldDescriptors);

				return domInstanceSection;
			}

			public static SectionDefinition CreateFilterSection(DomHelper domHelper)
			{
				var typeEnum = new GenericEnum<string>();
				typeEnum.AddEntry("create_filter", "create_filter");

				var convivaElementFieldDescriptor = CreateFieldDescriptorObject<string>("Conviva Element", "The name(not ELEMID) of the Conviva Element that should handle this process.", "52db26ba-de97-4354-8692-fd9fdcb84dba");
				var typeFieldDescriptor = CreateEnumFieldDescriptorObject("Type", "The type of operation Conviva should perform for this filter.", "f9fc8955-1d63-4c80-a5fa-13708d41ea67", typeEnum, new ValueWrapper<string>("create_filter"));
				var convivaNameFieldDescriptor = CreateFieldDescriptorObject<string>("Conviva Name", "Unique ID to link the provision to an Event or Channel.", "5768a20b-9398-4472-8378-c29caa0c1cd5");
				var categoryFieldDescriptor = CreateFieldDescriptorObject<string>("Category", "Category of the Conviva filter.", "61760703-5095-4780-96e4-543c57758105");
				var subcategoryFieldDescriptor = CreateFieldDescriptorObject<string>("Subcategory", "Subcategory of the Conviva filter.", "cd5ae5cc-d1c1-490c-8058-93eea62658f9");
				var enabledFieldDescriptor = CreateFieldDescriptorObject<bool>("Enabled", "Indicates if the filter will be active or inactive.", "504b2817-c11a-4174-ae68-ff0661314e27", new ValueWrapper<bool>(true));
				var instanceIdFieldDescriptor = CreateFieldDescriptorObject<string>("InstanceId", "Indicates if the filter will be active or inactive.", "efa78ff1-9347-49eb-8200-907f6489ed11");

				List<FieldDescriptor> fieldDescriptors = new List<FieldDescriptor>
				{
					convivaElementFieldDescriptor,
					typeFieldDescriptor,
					convivaNameFieldDescriptor,
					categoryFieldDescriptor,
					subcategoryFieldDescriptor,
					enabledFieldDescriptor,
					instanceIdFieldDescriptor,
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

			private static FieldDescriptor CreateFieldDescriptorObject<T>(string fieldName, string toolTip, string id)
			{
				return new FieldDescriptor
				{
					ID = new FieldDescriptorID(Guid.Parse(id)),
					FieldType = typeof(T),
					Name = fieldName,
					Tooltip = toolTip,
				};
			}

			private static FieldDescriptor CreateFieldDescriptorObject<T>(string fieldName, string toolTip, string id, IValueWrapper defaultValue)
			{
				return new FieldDescriptor
				{
					ID = new FieldDescriptorID(Guid.Parse(id)),
					FieldType = typeof(T),
					Name = fieldName,
					Tooltip = toolTip,
					DefaultValue = defaultValue,
				};
			}

			private static FieldDescriptor CreateEnumFieldDescriptorObject(string fieldName, string toolTip, string id, GenericEnum<string> discreets)
			{
				return new GenericEnumFieldDescriptor
				{
					ID = new FieldDescriptorID(Guid.Parse(id)),
					FieldType = typeof(GenericEnum<string>),
					Name = fieldName,
					Tooltip = toolTip,
					GenericEnumInstance = discreets,
				};
			}

			private static FieldDescriptor CreateEnumFieldDescriptorObject(string fieldName, string toolTip, string id, GenericEnum<string> discreets, IValueWrapper defaultValue)
			{
				return new GenericEnumFieldDescriptor
				{
					ID = new FieldDescriptorID(Guid.Parse(id)),
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
}