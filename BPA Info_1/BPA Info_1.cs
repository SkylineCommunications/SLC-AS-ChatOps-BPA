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
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Text.RegularExpressions;
	using AdaptiveCards;
	using Newtonsoft.Json;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Automation;
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
			var bpaResults = GetBpaResultsByAgent(engine);

			SendBpaResults(engine, bpaResults);
		}

		private Dictionary<string, Dictionary<string, ExecuteBpaResponse>> GetBpaResultsByAgent(IEngine engine)
		{
			var helper = new BpaManagerHelper(Engine.SLNetRaw);
			var bpas = helper.BPAs.ReadAll();

			var dms = engine.GetDms();
			var agents = dms.GetAgents();

			var bpaResultsByAgent = new Dictionary<string, Dictionary<string, ExecuteBpaResponse>>();

			var executeBpasResponse = helper.BPAs.GetLastResults(helper.BPAs.ReadAll(), agents.Select(a => new DestinationAgent(a.Id)).ToArray());
			foreach (var response in executeBpasResponse.Responses)
			{
				var dmaId = response.SourceDmaId;
				var dmaName = agents.FirstOrDefault(a => a.Id == dmaId)?.Name;
				if (dmaName == null)
				{
					continue;
				}

				if (!bpaResultsByAgent.TryGetValue(dmaName, out var bpaResultsForAgent))
				{
					bpaResultsForAgent = new Dictionary<string, ExecuteBpaResponse>();
					bpaResultsByAgent[dmaName] = bpaResultsForAgent;
				}

				var bpaName = bpas.FirstOrDefault(b => b.ID == response.BpaId)?.MetaData?.TestName;
				bpaResultsForAgent[bpaName] = response;
			}

			return bpaResultsByAgent;
		}

		private void SendBpaResults(IEngine engine, Dictionary<string, Dictionary<string, ExecuteBpaResponse>> bpaResultsbyAgent)
		{
			var adaptiveCardBody = new List<AdaptiveElement>();

			foreach (var bpaResultsForAgent in bpaResultsbyAgent.OrderBy(r => r.Key))
			{
				var agentInfoTextBlock = new AdaptiveTextBlock($"BPA results for {bpaResultsForAgent.Key}")
				{
					Type = "TextBlock",
					Weight = AdaptiveTextWeight.Bolder,
					Size = AdaptiveTextSize.Large,
				};
				adaptiveCardBody.Add(agentInfoTextBlock);

				foreach (var result in bpaResultsForAgent.Value.OrderBy(r => r.Key))
				{
					var bpaInfoFacts = new AdaptiveFactSet
					{
						Type = "FactSet",
						Facts = new List<AdaptiveFact>
						{
							new AdaptiveFact("Name", result.Key),
							new AdaptiveFact("Status", Regex.Replace(result.Value.Outcome.ToString(), @"([a-z])([A-Z])", "$1 $2")),
							new AdaptiveFact("Message:", result.Value.Message),
							new AdaptiveFact("Executed:", result.Value.Timestamp.ToString()),
						},
					};

					var bpaResultsContainer = new AdaptiveContainer();
					bpaResultsContainer.Style = TranslateOutcome(result.Value.Outcome);
					bpaResultsContainer.Items.Add(bpaInfoFacts);

					adaptiveCardBody.Add(bpaResultsContainer);
				}
			}

			var adaptiveCard = JsonConvert.SerializeObject(adaptiveCardBody);
			engine.AddScriptOutput("AdaptiveCard", adaptiveCard);
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
					return AdaptiveContainerStyle.Emphasis;
			}
		}
	}
}