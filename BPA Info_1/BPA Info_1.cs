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

namespace BPA_Info_1
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using System.Security.Claims;
	using System.Text;
	using System.Text.RegularExpressions;
	using AdaptiveCards;
	using Newtonsoft.Json;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.BPA;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	/// <summary>
	/// Represents a DataMiner Automation script.
	/// </summary>
	public class Script
	{
		/// <summary>
		/// The script entry point.
		/// </summary>
		/// <param name="engine">Link with SLAutomation process.</param>
		public void Run(IEngine engine)
		{
			var bpaResults = GetBpaResults();
			//SendBpaResultsAsTable(engine, bpaResults);
			//SendBpaResultsAsFactSets(engine, bpaResults);
			SendBpaResultsAsCarousel(engine, bpaResults);
		}

		private Dictionary<string, ExecuteBpaResponse> GetBpaResults()
		{
			var helper = new BpaManagerHelper(Engine.SLNetRaw);
			var bpas = helper.BPAs.ReadAll();

			var executeBpasResponse = helper.BPAs.GetLastResults(helper.BPAs.ReadAll(), new DestinationAgent[] { new DestinationAgent(-1) });
			return executeBpasResponse.Responses.ToDictionary(r => bpas.FirstOrDefault(b => b.ID == r.BpaId)?.MetaData?.TestName);
		}

		private void SendBpaResultsAsTable(IEngine engine, Dictionary<string, ExecuteBpaResponse> bpaResults)
		{
			var table = new AdaptiveTable
			{
				Type = "Table",
				FirstRowAsHeaders = true,
				Columns = new List<AdaptiveTableColumnDefinition>
				{
					new AdaptiveTableColumnDefinition
					{
						Width = 250,
					},
					new AdaptiveTableColumnDefinition
					{
						Width = 150,
					},
					new AdaptiveTableColumnDefinition
					{
						Width = 300,
					},
					new AdaptiveTableColumnDefinition
					{
						Width = 200,
					},
				},
				Rows = new List<AdaptiveTableRow>
				{
					new AdaptiveTableRow
					{
						Type = "TableRow",
						Cells = new List<AdaptiveTableCell>
						{
							new AdaptiveTableCell
							{
								Type = "TableCell",
								Items = new List<AdaptiveElement>
								{
									new AdaptiveTextBlock("Name")
									{
										Type = "TextBlock",
										Weight = AdaptiveTextWeight.Bolder,
									},
								},
							},
							new AdaptiveTableCell
							{
								Type = "TableCell",
								Items = new List<AdaptiveElement>
								{
									new AdaptiveTextBlock("Status")
									{
										Type = "TextBlock",
										Weight = AdaptiveTextWeight.Bolder,
									},
								},
							},
							new AdaptiveTableCell
							{
								Type = "TableCell",
								Items = new List<AdaptiveElement>
								{
									new AdaptiveTextBlock("Message")
									{
										Type = "TextBlock",
										Weight = AdaptiveTextWeight.Bolder,
									},
								},
							},
							new AdaptiveTableCell
							{
								Type = "TableCell",
								Items = new List<AdaptiveElement>
								{
									new AdaptiveTextBlock("Last Run")
									{
										Type = "TextBlock",
										Weight = AdaptiveTextWeight.Bolder,
									},
								},
							},
						},
					},
				},
			};

			foreach (var result in bpaResults)
			{
				var row = new AdaptiveTableRow
				{
					Type = "TableRow",
					Cells = new List<AdaptiveTableCell>
					{
						new AdaptiveTableCell
						{
							Type = "TableCell",
							Items = new List<AdaptiveElement>
							{
								new AdaptiveTextBlock(result.Key)
								{
									Type = "TextBlock",
									Wrap = true,
								},
							},
						},
						new AdaptiveTableCell
						{
							Type = "TableCell",
							Items = new List<AdaptiveElement>
							{
								new AdaptiveTextBlock(Regex.Replace(result.Value.Outcome.ToString(), @"([a-z])([A-Z])", "$1 $2"))
								{
									Type = "TextBlock",
									Wrap = true,
								},
							},
							Style = TranslateOutcome(result.Value.Outcome),
						},
						new AdaptiveTableCell
						{
							Type = "TableCell",
							Items = new List<AdaptiveElement>
							{
								new AdaptiveTextBlock(result.Value.Message)
								{
									Type = "TextBlock",
									Wrap = true,
								},
							},
							Style = TranslateOutcome(result.Value.Outcome),
						},
						new AdaptiveTableCell
						{
							Type = "TableCell",
							Items = new List<AdaptiveElement>
							{
								new AdaptiveTextBlock(result.Value.Timestamp.ToString())
								{
									Type = "TextBlock",
									Wrap = true,
								},
							},
						},
					},
				};

				table.Rows.Add(row);
			}

			var adaptiveCardBody = new List<AdaptiveElement> { table };

			engine.AddScriptOutput("AdaptiveCard", JsonConvert.SerializeObject(adaptiveCardBody));
		}

		private void SendBpaResultsAsFactSets(IEngine engine, Dictionary<string, ExecuteBpaResponse> bpaResults)
		{
			var adaptiveCardBody = new List<AdaptiveElement>()
			{
				new AdaptiveTextBlock("This is the overview of the results of the last run.")
				{
					Wrap = true,
				},
			};

			foreach (var result in bpaResults)
			{
				adaptiveCardBody.Add(new AdaptiveFactSet()
				{
					Facts = new List<AdaptiveFact>
					{
						new AdaptiveFact("Name:", result.Key),
						new AdaptiveFact("Status:", Regex.Replace(result.Value.Outcome.ToString(), @"([a-z])([A-Z])", "$1 $2")),
						new AdaptiveFact("Message:", result.Value.Message),
					},
				});
			}

			engine.AddScriptOutput("AdaptiveCard", JsonConvert.SerializeObject(adaptiveCardBody));
		}

		private void SendBpaResultsAsContainer(IEngine engine, Dictionary<string, ExecuteBpaResponse> bpaResults)
		{
			var adaptiveCardBody = new List<AdaptiveElement>();

			foreach (var result in bpaResults.OrderBy(r => r.Key))
			{
				var factSet = new AdaptiveFactSet
				{
					Type = "FactSet",
					Facts = new List<AdaptiveFact>
					{
						new AdaptiveFact("Name:", result.Key),
						new AdaptiveFact("Status:", Regex.Replace(result.Value.Outcome.ToString(), @"([a-z])([A-Z])", "$1 $2")),
						new AdaptiveFact("Message:", result.Value.Message),
					},
				};

				var container = new AdaptiveContainer();
				container.Items.Add(factSet);
				container.Style = TranslateOutcome(result.Value.Outcome);

				adaptiveCardBody.Add(container);
			}

			engine.AddScriptOutput("AdaptiveCard", JsonConvert.SerializeObject(adaptiveCardBody));
		}

		private AdaptiveContainerStyle TranslateOutcome(BpaOutcome outcome)
		{
			switch(outcome)
			{
				case BpaOutcome.Warning:
					return AdaptiveContainerStyle.Warning;
				case BpaOutcome.IssuesDetected:
					return AdaptiveContainerStyle.Attention;
				case BpaOutcome.NoIssues:
					return AdaptiveContainerStyle.Good;
				default:
					return AdaptiveContainerStyle.Default;
			}
		}
	}
}