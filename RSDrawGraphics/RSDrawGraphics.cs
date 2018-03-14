/*=============================================================================|
|  PROJECT RSDrawGraphics                                                1.0.0 |
|==============================================================================|
|  Copyright (C) 2016 Denis FRAIPONT (SICA2M)                                  |
|  All rights reserved.                                                        |
|==============================================================================|
|  RSDrawGraphics is free software: you can redistribute it and/or modify      |
|  it under the terms of the Lesser GNU General Public License as published by |
|  the Free Software Foundation, either version 3 of the License, or           |
|  (at your option) any later version.                                         |
|                                                                              |
|  It means that you can distribute your commercial software which includes    |
|  RSDrawGraphics without the requirement to distribute the source code        |
|  of your application and without the requirement that your application be    |
|  itself distributed under LGPL.                                              |
|                                                                              |
|  RSDrawGraphics is distributed in the hope that it will be useful,           |
|  but WITHOUT ANY WARRANTY; without even the implied warranty of              |
|  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the               |
|  Lesser GNU General Public License for more details.                         |
|                                                                              |
|  You should have received a copy of the GNU General Public License and a     |
|  copy of Lesser GNU General Public License along with RSDrawGraphics.        |
|  If not, see  http://www.gnu.org/licenses/                                   |
|=============================================================================*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using ABB.Robotics.Controllers;
using ABB.Robotics.Controllers.Discovery;
using ABB.Robotics.Controllers.RapidDomain;
using ABB.Robotics.Math;
using ABB.Robotics.RobotStudio;
using ABB.Robotics.RobotStudio.Controllers;
using ABB.Robotics.RobotStudio.Stations;
using ABB.Robotics.RobotStudio.Environment;

namespace RSDrawGraphics
{
	/// <summary>
	/// Define GfxShapeData Record
	/// </summary>
	public struct GfxShapeData : IRapidData
	{
		public enum ShapeTypes{ Capsule, Box }
		private UserDefined _data;
		private bool _isNotNull;
		public GfxShapeData(RapidDataType rdt, RapidData rd)
		{
			_data = new UserDefined(rdt);
			_data = (UserDefined)rd.Value;
			_isNotNull = true;
		}
		private UserDefined IntData
		{
			get { return _data; }
			set { _data = value; }
		}
		public bool IsNotNull { get { return _isNotNull; }}

		public ShapeTypes ShapeType
		{
			get
			{
				ShapeTypes res = ShapeTypes.Capsule;//Capsule by default
				if (Num.Parse(IntData.Components[0].ToString()).Value == 2)
					res = ShapeTypes.Box;

				return res;
			}
			set
			{
				IntData.Components[0] = new Num((value == ShapeTypes.Box) ? 2 : 1);
			}
		}
		public Num ParentAxis
		{
			get
			{
				Num res = Num.Parse(IntData.Components[1].ToString());
				return res;
			}
			set
			{
				IntData.Components[1] = new Num(value);
			}
		}
		public Pose CurrentAxis
		{
			get
			{
				Pose res = Pose.Parse(IntData.Components[2].ToString());
				return res;
			}
			set
			{
				Pose res = new Pose();
				res.FillFromString2(value.ToString());
				IntData.Components[2] = res;
			}
		}
		public Pose Shift
		{
			get
			{
				Pose res = Pose.Parse(IntData.Components[3].ToString());
				return res;
			}
			set
			{
				Pose res = new Pose();
				res.FillFromString2(value.ToString());
				IntData.Components[3] = res;
			}
		}
		public Pos Size
		{
			get
			{
				Pos res = Pos.Parse(IntData.Components[4].ToString());
				return res;
			}
			set
			{
				Pos res = new Pos();
				res.FillFromString2(value.ToString());
				IntData.Components[4] = res;
			}
		}
		public Num Color
		{
			get
			{
				Num res = Num.Parse(IntData.Components[5].ToString());
				return res;
			}
			set
			{
				IntData.Components[5] = new Num(value);
			}
		}

		public void FillFromString(string newValue)
		{
			IntData.FillFromString2(newValue);
		}
		public void Fill(DataNode root)
		{
			IntData.Fill2(root);
		}
		public void Fill(string value)
		{
			IntData.Fill2(value);
		}
		public override string ToString()
		{
			return IntData.ToString();
		}
		public DataNode ToStructure()
		{
			return IntData.ToStructure();
		}
	}

	/// <summary>
	/// Code-behind class for the RSDrawGraphics Smart Component.
	/// </summary>
	/// <remarks>
	/// The code-behind class should be seen as a service provider used by the 
	/// Smart Component runtime. Only one instance of the code-behind class
	/// is created, regardless of how many instances there are of the associated
	/// Smart Component.
	/// Therefore, the code-behind class should not store any state information.
	/// Instead, use the SmartComponent.StateCache collection.
	/// </remarks>
	public class CodeBehind : SmartComponentCodeBehind
	{
		//Principal Component is OnLoad (don't do anything)
		bool bOnLoad = false;
		//Private Component List for EventHandler
		private List<SmartComponent> myComponents = new List<SmartComponent>();
		//Last time an update occurs (when received a lot of event log in same time).
		DateTime lastUpdate = DateTime.UtcNow;
		//If component is on OnPropertyValueChanged
		Dictionary<string, bool> isOnPropertyValueChanged;
		//If component is on UpdateControllers
		Dictionary<string, bool> isUpdatingControllers;

		/// <summary>
		/// Called from [!:SmartComponent.InitializeCodeBehind]. 
		/// </summary>
		/// <param name="component">Smart Component</param>
		public override void OnInitialize(SmartComponent component)
		{
			///Never Called???
			base.OnInitialize(component);
			component.Properties["Status"].Value = "OnInitialize";
		}

		/// <summary>
		/// Called if the library containing the SmartComponent has been replaced
		/// </summary>
		/// <param name="component">Smart Component</param>
		public override void OnLibraryReplaced(SmartComponent component)
		{
			base.OnLibraryReplaced(component);
			component.Properties["Status"].Value = "OnLibraryReplaced";

			//Save Modified Component for EventHandlers
			//OnPropertyValueChanged is not called here
			UpdateComponentList(component);
		}

		/// <summary>
		/// Called when the library or station containing the SmartComponent has been loaded 
		/// </summary>
		/// <param name="component">Smart Component</param>
		public override void OnLoad(SmartComponent component)
		{
			base.OnLoad(component);
			bOnLoad = true;
			isOnPropertyValueChanged = new Dictionary<string, bool>();
			isUpdatingControllers = new Dictionary<string, bool>();
			component.Properties["Status"].Value = "OnLoad";
			//Here component is not the final component and don't get saved properties.
			//Only called once for all same instance.
			//Connect Controller eventHandler
			//ABB.Robotics.RobotStudio.Controllers.ControllerReferenceChangedEventHandler
			ControllerManager.ControllerAdded -= OnControllerAdded;
			ControllerManager.ControllerAdded += OnControllerAdded;
			ControllerManager.ControllerRemoved -= OnControllerRemoved;
			ControllerManager.ControllerRemoved += OnControllerRemoved;
			//ActiveStation is null here at startup
			component.ContainingProject.Saving -= OnSaving;
			component.ContainingProject.Saving += OnSaving;

			/*The following code doesn't work
			//Connect to Apply Button
			CommandBarControl commandBarControl = UIEnvironment.RibbonTabs["RAPID"].Groups["Controller"].Controls["RapidApplyMenu"];
			if (commandBarControl!=null)
			{
				//((CommandBarPopup)commandBarControl).ClickButton.ExecuteCommand -= OnApply;
				//((CommandBarPopup)commandBarControl).ClickButton.ExecuteCommand += OnApply;
				((CommandBarButton)((CommandBarPopup)commandBarControl).Controls["RapidApplyAll"]).ExecuteCommand -= OnApply;
				((CommandBarButton)((CommandBarPopup)commandBarControl).Controls["RapidApplyAll"]).ExecuteCommand += OnApply;
				((CommandBarButton)((CommandBarPopup)commandBarControl).Controls["RapidModuleApply"]).ExecuteCommand -= OnApply;
				((CommandBarButton)((CommandBarPopup)commandBarControl).Controls["RapidModuleApply"]).ExecuteCommand += OnApply;
				//This Works but not implicit
				//CommandBarButton commandBarButton = new CommandBarButton("RSDrawGraphicsApply", "Apply All and Update RsDrawGraphics");
				//commandBarButton.ExecuteCommand += OnApply;
				//commandBarButton.DefaultEnabled = true;
				//commandBarButton.KeyboardShortcuts = new Keys[] { Keys.S | Keys.Shift | Keys.Control | Keys.Alt };
				//((CommandBarPopup)commandBarControl).Controls.Add(commandBarButton);
			}*/
			// So, survey log message for checked programs at end of apply.
			Logger.LogMessageAdded -= OnLogMessageAdded;
			Logger.LogMessageAdded += OnLogMessageAdded;

			bOnLoad = false;
		}

		/// <summary>
		/// Called when the value of a dynamic property value has changed.
		/// </summary>
		/// <param name="component"> Component that owns the changed property. </param>
		/// <param name="changedProperty"> Changed property. </param>
		/// <param name="oldValue"> Previous value of the changed property. </param>
		public override void OnPropertyValueChanged(SmartComponent component, DynamicProperty changedProperty, Object oldValue)
		{
			base.OnPropertyValueChanged(component, changedProperty, oldValue);
			if (bOnLoad)
			{
				UpdateComponentList(component);
				UpdateControllers(component);
				isOnPropertyValueChanged[component.UniqueId] = false;
				return;
			}
			if (!isOnPropertyValueChanged.ContainsKey(component.UniqueId))
				isOnPropertyValueChanged[component.UniqueId] = false;

			bool bIsOnPropertyValueChanged = isOnPropertyValueChanged[component.UniqueId];
			isOnPropertyValueChanged[component.UniqueId] = true;

			if ((!changedProperty.Name.StartsWith("Generated"))
				  && (changedProperty.Name != "Status") )
			{

				if (changedProperty.Name == "Controller")
				{
					if ((string)changedProperty.Value != "Update")
					{
						string sAllowedValues = "";
						GetController(component, ref sAllowedValues);
						if (sAllowedValues != "")
						{
							Logger.AddMessage("RSDrawGraphics: Connecting Component " + component.Name + " to " + (string)changedProperty.Value, LogMessageSeverity.Information);
							component.Properties["Status"].Value = "Connected";
						}
					}
				}
				if (changedProperty.Name == "Task")
				{
					string sAllowedValues = "";
					Controller controller = null;
					GetTask(component, ref controller, ref sAllowedValues);
				}
				if (changedProperty.Name == "Module")
				{
					string sAllowedValues = "";
					Controller controller = null;
					Task task = null;
					GetModule(component, ref controller, ref task, ref sAllowedValues);
					if (task != null)
						try { task.Dispose(); } catch (System.ObjectDisposedException) { } //Need task._disposed public.
				}
				if (changedProperty.Name == "Data")
				{
					Controller controller = null;
					Task task = null;
					Module module = null;
					GetGfxShapeData(component, ref controller, ref task, ref module);
					if (task != null)
						try { task.Dispose(); } catch (System.ObjectDisposedException) { } //Need task._disposed public.
				}

				if ((int)component.IOSignals["UpdateGraph"].Value == 1)
				{
					RebuildPart(component);
				}
			}
			if (changedProperty.Name == "MakeAxisFrame")
			{
				if (!(bool)changedProperty.Value)
				{
					while (component.Frames.Count>0)
					{
						component.Frames.Remove(component.Frames[0]);
					}

				}
			}
			//Update properties visibility
			string shapeType = (string)component.Properties["ShapeType"].Value;
			component.Properties["Radius"].UIVisible = (shapeType == "Capsule");
			component.Properties["Height"].UIVisible = (shapeType == "Box");
			component.Properties["Width"].UIVisible = (shapeType == "Box");
			if (!bIsOnPropertyValueChanged && (changedProperty.Name !="Status")
				  && (!changedProperty.Name.StartsWith("Generated")) )
			{
				//Update available controller
				UpdateControllers(component);
				//Save Modified Component for EventHandlers
				UpdateComponentList(component);
			}

			isOnPropertyValueChanged[component.UniqueId] = bIsOnPropertyValueChanged;
		}

		/// <summary>
		/// Called when the value of an I/O signal value has changed.
		/// </summary>
		/// <param name="component"> Component that owns the changed signal. </param>
		/// <param name="changedSignal"> Changed signal. </param>
		public override void OnIOSignalValueChanged(SmartComponent component, IOSignal changedSignal)
		{
			if (changedSignal.Name == "UpdateGraph")
			{
				if ((int)changedSignal.Value == 1)
				{
					Controller controller = null;
					Task task = null;
					Module module = null;
					GetGfxShapeData(component, ref controller, ref task, ref module);
					if (controller != null)
					{
						component.Properties["Status"].Value = "Simulated";
					}
					if (task != null)
						try { task.Dispose(); } catch (System.ObjectDisposedException) { } //Need task._disposed public.
					//Update even if no connection
					RebuildPart(component);
				}
			}

			if (changedSignal.Name == "SyncToRAPID")
			{
				if ((int)changedSignal.Value == 1)
				{
					Controller controller = null;
					Task task = null;
					Module module = null;
					SetGfxShapeData(component, ref controller, ref task, ref module);
					if (controller != null)
					{
						component.Properties["Status"].Value = "Synchronized";
						component.IOSignals["SyncToRAPID"].Value = 0;
					}
					if (task != null)
						try { task.Dispose(); } catch (System.ObjectDisposedException) { } //Need task._disposed public.
					//Update even if no connection
					RebuildPart(component);
				}
			}
			UpdateComponentList(component);
		}

		/// <summary>
		/// Called during simulation.
		/// </summary>
		/// <param name="component"> Simulated component. </param>
		/// <param name="simulationTime"> Time (in ms) for the current simulation step. </param>
		/// <param name="previousTime"> Time (in ms) for the previous simulation step. </param>
		/// <remarks>
		/// For this method to be called, the component must be marked with
		/// simulate="true" in the xml file.
		/// </remarks>
		public override void OnSimulationStep(SmartComponent component, double simulationTime, double previousTime)
		{
			//Big resources needed and ShapeData could not be updated so frequently in RAPID Task
			//Controller controller = null;
			//Task task = null;
			//Module module = null;
			//GetGfxShapeData(component, ref controller, ref task, ref module);
			//if (controller != null)
			//{
			//	component.Properties["Status"].Value = "Simulated";
			//	RebuildPart(component);
			//}
			//if (task != null)
			//	try { task.Dispose(); } catch (System.ObjectDisposedException) { } //Need task._disposed public.
			//UpdateComponentList(component);
		}

		/// <summary>
		/// Called to retrieve the actual value of a property attribute with the dummy value '?'.
		/// </summary>
		/// <param name="component">Component that owns the property.</param>
		/// <param name="owningProperty">Property that owns the attribute.</param>
		/// <param name="attributeName">Name of the attribute to query.</param>
		/// <returns>Value of the attribute.</returns>
		public override string QueryPropertyAttributeValue(SmartComponent component, DynamicProperty owningProperty, string attributeName)
		{
			return "?";
		}

		/// <summary>
		/// Called to validate the value of a dynamic property with the CustomValidation attribute.
		/// </summary>
		/// <param name="component">Component that owns the changed property.</param>
		/// <param name="property">Property that owns the value to be validated.</param>
		/// <param name="newValue">Value to validate.</param>
		/// <returns>Result of the validation. </returns>
		public override ValueValidationInfo QueryPropertyValueValid(SmartComponent component, DynamicProperty property, object newValue)
		{
			if (property.Name == "Color")
			{
				Vector3 color = (Vector3)newValue;
				if ((color.x < 0) || (color.y < 0) || (color.z < 0))
					return new ValueValidationInfo(ValueValidationResult.BelowMin, "(0-255)");
				if ((color.x > 255) || (color.y > 255) || (color.z > 255))
					return new ValueValidationInfo(ValueValidationResult.AboveMax, "(0-255)");
			}
			return ValueValidationInfo.Valid;
		}


		//*********************************************************************************************
		/// <summary>
		/// Update internal component list to get them in EventHandler
		/// </summary>
		/// <param name="component">Component to update.</param>
		protected void UpdateComponentList(SmartComponent component)
		{
			bool bFound = false;
			for (int i = 0; i < myComponents.Count; ++i)
			{
				SmartComponent myComponent = myComponents[i];
				//Test if component exists as no OnUnLoad event exists.
				if ( (myComponent.ContainingProject == null)
					  || (myComponent.ContainingProject.GetObjectFromUniqueId(myComponents[i].UniqueId) == null)
						|| (myComponent.ContainingProject.Name == "")
						|| (bFound && (myComponent.UniqueId == component.UniqueId)) )
				{
					Logger.AddMessage("RSDrawGraphics: Remove old Component " + myComponents[i].Name + " from cache.", LogMessageSeverity.Information);
					myComponents.Remove(myComponent);
					--i;
					continue;
				}
				if (myComponents[i].UniqueId == component.UniqueId)
				{
					myComponents[i] = component;
					bFound = true;
				}
			}
			if (!bFound)
				myComponents.Add(component);
		}

		/// <summary>
		/// Get all Controllers to update Allowed controllers.
		/// </summary>
		/// <param name="component">Component that owns the controller property.</param>
		protected void UpdateControllers(SmartComponent component)
		{
			if (!isUpdatingControllers.ContainsKey(component.UniqueId))
				isUpdatingControllers[component.UniqueId] = false;

			//This occurs when RebuildPart(component);
			if (isUpdatingControllers[component.UniqueId])
				return;

			isUpdatingControllers[component.UniqueId] = true;

			string allowedValues = ";" + (string)component.Properties["Controller"].Value;
			if (allowedValues == ";Update") allowedValues = ";";
			string controllerId = allowedValues.Replace(";","");
			bool oldFound = false;

			foreach (ControllerObjectReference controllerObjectReference in ControllerManager.ControllerReferences)
			{
				if (!allowedValues.Contains(controllerObjectReference.SystemId.ToString()))
				{
					allowedValues += (";" + controllerObjectReference.SystemId.ToString() + " [" + controllerObjectReference.RobControllerConnection.ToString() + "]");
				} else
				{
					oldFound = true;
				}
			}
			allowedValues = allowedValues.Replace(";;", ";");
			if ((allowedValues == "") || (allowedValues == ";")) allowedValues = ";Update";
			component.Properties["Controller"].Attributes["AllowedValues"] = allowedValues;
			if (!oldFound && (controllerId != ""))
			{
				//Old controller not found test to connect to it.
				Logger.AddMessage("RSDrawGraphics: Test Connecting Component " + component.Name + " to " + controllerId + ". Because it is not online.", LogMessageSeverity.Information);
				Controller controller = null;
				Task task = null;
				Module module = null;
				GetGfxShapeData(component, ref controller, ref task, ref module);
				if (controller != null)
				{
					Logger.AddMessage("RSDrawGraphics: Connecting Component " + component.Name + " to " + controllerId + ". But it is not online.", LogMessageSeverity.Information);
					component.Properties["Status"].Value = "Connected";
				}
				else
				{
					component.Properties["Status"].Value = "Disconnected";
				}
				if (task != null)
					try { task.Dispose(); } catch (System.ObjectDisposedException) { } //Need task._disposed public.

				RebuildPart(component);
			}

			isUpdatingControllers[component.UniqueId] = false;
		}

		/// <summary>
		/// Get Controller from Controller property and tasks list.
		/// </summary>
		/// <param name="component">Component that owns the changed property.</param>
		/// <param name="sTasksList">Tasks list delimited by ";".</param>
		/// <returns>Found Controller</returns>
		protected Controller GetController(SmartComponent component, ref string sTasksList)
		{
			Controller controller = null;
			string guid = ((string)component.Properties["Controller"].Value).Split(' ')[0];
			if (guid != "")
			{
				Guid systemId = new Guid(guid);

				NetworkScanner scanner = new NetworkScanner();
				if (scanner.TryFind(systemId,TimeSpan.FromSeconds(60),1, out ControllerInfo controllerInfo))
				{
					controller = ControllerFactory.CreateFrom(controllerInfo);//new Controller(systemId);
					if (controller.SystemId == systemId)
					{
						if (controller.Connected)
						{
							string oldTask = (string)component.Properties["Task"].Value;
							bool bSetOldValue = (component.Properties["Task"].Attributes["AllowedValues"].ToString() == "") && (oldTask != "");
							foreach (Task task in controller.Rapid.GetTasks())
							{
								sTasksList += ";" + task.Name;
							}
							if ((";" + sTasksList + ";").Contains(";" + oldTask + ";") && bSetOldValue)
							{
								component.Properties["Task"].Attributes["AllowedValues"] = sTasksList;
								Task task = null;
								Module module = null;
								GetGfxShapeData(component, ref controller, ref task, ref module);
								if (controller != null)
								{
									Logger.AddMessage("RSDrawGraphics: Connecting Component " + component.Name + " to " + controller.SystemId.ToString() + ". Use olds values.", LogMessageSeverity.Information);
									component.Properties["Status"].Value = "Connected";
								}
								if (task != null)
									try { task.Dispose(); } catch (System.ObjectDisposedException) { } //Need task._disposed public.
							}
						}
					}
				}
				else
				{
					//Try to connect controller later with OnControlerAdded
				}
			}
			else
			{
				component.Properties["Status"].Value = "Disconnected";
			}
			component.Properties["Task"].Attributes["AllowedValues"] = sTasksList;
			if (sTasksList == "")
			{
				component.Properties["Module"].Attributes["AllowedValues"] = "";
				component.Properties["Data"].Attributes["AllowedValues"] = "";
			}
			return controller;
		}

		/// <summary>
		///  Occurs before a ABB.Robotics.RobotStudio.Project is saved to file.
		/// </summary>
		/// <param name="sender">Sender</param>
		/// <param name="e">The event argument.</param>
		private void OnSaving(object sender, ABB.Robotics.RobotStudio.SavingProjectEventArgs e)
		{
			//Can't use foreach as collection is updated inside
			for (int i = 0; i < myComponents.Count; ++i)
			{
				SmartComponent myComponent = myComponents[i];
				//Test if component exists as no OnUnLoad event exists.
				if ( (myComponent.ContainingProject == null)
					  || (myComponent.ContainingProject.GetObjectFromUniqueId(myComponent.UniqueId) == null)
						|| (myComponent.ContainingProject.Name == "") )
				{
					Logger.AddMessage("RSDrawGraphics: Remove old Component " + myComponent.Name + " from cache.", LogMessageSeverity.Information);
					myComponents.Remove(myComponent);
					--i;
					continue;
				}

				Controller controller = null;
				Task task = null;
				Module module = null;
				GetGfxShapeData(myComponent, ref controller, ref task, ref module);
				if (controller != null)
					myComponent.Properties["Status"].Value = "Saved";
				if (task != null)
					try { task.Dispose(); } catch (System.ObjectDisposedException) { } //Need task._disposed public.

				RebuildPart(myComponent);
			}
		}

		/// <summary>
		///  Raised when a message is added.
		/// </summary>
		/// <param name="sender">Sender</param>
		/// <param name="e">The event argument.</param>
		private void OnLogMessageAdded(object sender, LogMessageAddedEventArgs e)
		{
			if (e.Message.Text.StartsWith("RSDrawGraphics"))
				return;

			RobotStudio.API.Internal.EventLogMessage eventLogMessage = e.Message as RobotStudio.API.Internal.EventLogMessage;
			if ( (e.Message.Text.Contains("Update RSDrawGraphics"))
				|| (e.Message.Text.StartsWith("Checked:") && e.Message.Text.EndsWith("No errors."))
				|| (e.Message.Text.StartsWith("Vérifié :") && e.Message.Text.EndsWith("aucune erreur."))
				|| (e.Message.Text.StartsWith("Geprüft:") && e.Message.Text.EndsWith("Keine Fehler."))
				|| (e.Message.Text.StartsWith("Comprobado:") && e.Message.Text.EndsWith("Sin errores."))
				|| (e.Message.Text.StartsWith("Verificato:") && e.Message.Text.EndsWith("Nessun errore."))
				|| (e.Message.Text.StartsWith("次の項目を確認しました:") && e.Message.Text.EndsWith("エラーはありません。"))
				|| (e.Message.Text.StartsWith("已检查：") && e.Message.Text.EndsWith("无错误。"))
				|| ((eventLogMessage != null) && (eventLogMessage.Msg.Domain == 1) && (eventLogMessage.Msg.Number == 122)) //Program stopped.
				//|| ((eventLogMessage != null) && (eventLogMessage.Msg.Domain == 1) && (eventLogMessage.Msg.Number == 123)) //Program stopped. Step
				|| ((eventLogMessage != null) && (eventLogMessage.Msg.Domain == 1) && (eventLogMessage.Msg.Number == 124)) //Program stopped. Start
				)
			{
				if ( DateTime.Compare(DateTime.UtcNow, lastUpdate.AddSeconds(1)) > 0 )
				{
					string filter = "";
					if (eventLogMessage != null)
						filter = eventLogMessage.CtrlId.ToString() + " ";

					filter += e.Message.Text;
					UpdateGraphics(filter);
					lastUpdate = DateTime.UtcNow;
				}
			}
		}

		/// <summary>
		/// This event is raised when the user selects the "Apply" CommandBarButton
		/// </summary>
		/// <param name="sender">Sender</param>
		/// <param name="e">The event argument.</param>
		public void OnApply(object sender, ExecuteCommandEventArgs e)
		{
			//If called by a CommandBarButton
			if (e!=null)
			{
				//First Apply modifications to be updated
				CommandBarControl commandBarControl = UIEnvironment.RibbonTabs["RAPID"].Groups["Controller"].Controls["RapidApplyMenu"];
				if (commandBarControl != null)
				{
					//((CommandBarPopup)commandBarControl).ClickButton.ExecuteCommand -= OnApply;
					//((CommandBarPopup)commandBarControl).ClickButton.ExecuteCommand += OnApply;
					((CommandBarButton)((CommandBarPopup)commandBarControl).Controls["RapidApplyAll"]).Execute();
					Application.DoEvents();

					System.Threading.Thread.Sleep(2000);
				}

			}
			UpdateGraphics("");
		}

		/// <summary>
		/// Update Graphics for all component, checking controller GUID, Name or task
		/// </summary>
		/// <param name="ctrlFilter">Text containing controller guid, name or task in the form (name/RAPID/task)</param>
		private void UpdateGraphics(string ctrlFilter)
		{
			Dictionary<string, KeyValuePair<Controller, Task>> toSignalUpdateDone = new Dictionary<string, KeyValuePair<Controller, Task>>();

			//Can't use foreach as collection is updated inside
			for (int i = 0; i < myComponents.Count; ++i)
			{
				SmartComponent myComponent = myComponents[i];
				//Test if component exists as no OnUnLoad event exists.
				if ((myComponent.ContainingProject == null)
						|| (myComponent.ContainingProject.GetObjectFromUniqueId(myComponent.UniqueId) == null)
						|| (myComponent.ContainingProject.Name == ""))
				{
					Logger.AddMessage("RSDrawGraphics: Remove old Component " + myComponent.Name + " from cache.", LogMessageSeverity.Information);
					myComponents.Remove(myComponent);
					--i;
					continue;
				}

				string guid = ((string)myComponent.Properties["Controller"].Value).Split(' ')[0];
				string ctrlName = ((string)myComponent.Properties["Controller"].Value).Split('[')[1].Split(' ')[0];
				string taskName = ((string)myComponent.Properties["Task"].Value);
				bool filterOK = (ctrlFilter == "");
				filterOK |= ctrlFilter.Contains(guid);
				if (ctrlFilter.Contains("/RAPID/"))
					filterOK |= ctrlFilter.Contains(ctrlName + "/RAPID/" + taskName);
				else
					filterOK |= ctrlFilter.Contains(ctrlName);

				if (filterOK)
				{
					Logger.AddMessage("RSDrawGraphics: Updating Component " + myComponent.Name, LogMessageSeverity.Information);
					Controller controller = null;
					Task task = null;
					Module module = null;
					GetGfxShapeData(myComponent, ref controller, ref task, ref module);
					if ((controller != null) && (task != null))
					{
						myComponent.Properties["Status"].Value = "Updated " + DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss");
						toSignalUpdateDone[controller.Name + "/" + task.Name] = new KeyValuePair<Controller, Task>(controller, task);
					}

					RebuildPart(myComponent);
				}
			}

			//Only set GfxShapeDataUpdateDone once per task
			foreach (var item in toSignalUpdateDone)//Dictionary< string, KeyValuePair< Controller, Task> >
			{
				Controller mainController = item.Value.Key;
				Task mainTask = item.Value.Value;

				try
				{
					Module mainModule = mainTask.GetModule("modGfxShapeData");
					if (mainModule != null)
					{
						RapidData rd = mainModule.GetRapidData("GfxShapeDataUpdateDone");
						if (rd != null)
						{
							if (!((Bool)rd.Value).ToBoolean())
							{
								try
								{
									if (mainController.CurrentUser == null)
										mainController.Logon(UserInfo.DefaultUser);

									using (Mastership ms = Mastership.Request(mainController.Rapid))
									{
										rd.Value = new Bool(true);
									}
								}
								catch (InvalidOperationException)
								{
									Logger.AddMessage("RSDrawGraphics: Mastership.Request InvalidOperationException", LogMessageSeverity.Information);
								}
								catch (ABB.Robotics.TimeoutException)
								{
									Logger.AddMessage("RSDrawGraphics: Mastership.Request TimeoutException", LogMessageSeverity.Information);
								}
							}
						}
					}
				}
				catch (ABB.Robotics.GenericControllerException)
				{ }
			}

		}

		/// <summary>
		/// Raised when a controller reference is added.
		/// </summary>
		/// <param name="sender">Sender</param>
		/// <param name="e">The event argument.</param>
		public void OnControllerAdded(object sender, ControllerReferenceChangedEventArgs e)
		{
			//Can't use foreach as collection is updated inside
			for (int i = 0; i < myComponents.Count; ++i)
			{
				SmartComponent myComponent = myComponents[i];
				//Test if component exists as no OnUnLoad event exists.
				if ( (myComponent.ContainingProject == null)
					  || (myComponent.ContainingProject.GetObjectFromUniqueId(myComponent.UniqueId) == null)
						|| (myComponent.ContainingProject.Name == "") )
				{
					Logger.AddMessage("RSDrawGraphics: Remove old Component " + myComponent.Name + " from cache.", LogMessageSeverity.Information);
					myComponents.Remove(myComponent);
					--i;
					continue;
				}

				string guid = ((string)myComponent.Properties["Controller"].Value).Split(' ')[0];
				if (guid != "")
				{
					Guid systemId = new Guid(guid);
					if (e.Controller.SystemId == systemId)
					{
						Logger.AddMessage("RSDrawGraphics: Connecting Component " + myComponent.Name, LogMessageSeverity.Information);
						Controller controller = null;
						Task task = null;
						Module module = null;
						GetGfxShapeData(myComponent, ref controller, ref task, ref module);
						if (controller != null)
							myComponent.Properties["Status"].Value = "Connected";
						if (task != null && task.Enabled)
							try { task.Dispose(); } catch (ObjectDisposedException) { } //Need task._disposed public.

						RebuildPart(myComponent);
					}
				}
				string allowedValues = myComponent.Properties["Controller"].Attributes["AllowedValues"].ToString();
				if (!allowedValues.Contains(e.Controller.SystemId.ToString()))
				{
					allowedValues += (";" + e.Controller.SystemId.ToString() + " [" + e.Controller.RobControllerConnection.ToString() + "]");
				}
				if (allowedValues == "") allowedValues = ";Update";
				myComponent.Properties["Controller"].Attributes["AllowedValues"] = allowedValues;
			}
		}

		/// <summary>
		/// Raised when a controller reference is removed.
		/// </summary>
		/// <param name="sender">Sender</param>
		/// <param name="e">The event argument.</param>
		public void OnControllerRemoved(object sender, ControllerReferenceChangedEventArgs e)
		{
			//Can't use foreach as collection is updated inside
			for (int i = 0; i < myComponents.Count; ++i)
			{
				SmartComponent myComponent = myComponents[i];
				//Test if component exists as no OnUnLoad event exists.
				if ( (myComponent.ContainingProject == null)
					  || (myComponent.ContainingProject.GetObjectFromUniqueId(myComponent.UniqueId) == null)
						|| (myComponent.ContainingProject.Name == "") )
				{
					Logger.AddMessage("RSDrawGraphics: Remove old Component " + myComponent.Name + " from cache.", LogMessageSeverity.Information);
					myComponents.Remove(myComponent);
					--i;
					continue;
				}

				string guid = ((string)myComponent.Properties["Controller"].Value).Split(' ')[0];
				if (guid != "")
				{
					Guid systemId = new Guid(guid);
					if (e.Controller.SystemId == systemId)
					{
						Logger.AddMessage("RSDrawGraphics: Disconnecting Component " + myComponent.Name, LogMessageSeverity.Information);
						myComponent.Properties["Status"].Value = "Disconnected";
					}
				}
			}
		}

		/// <summary>
		/// Get Task from Task Property and modules list.
		/// </summary>
		/// <param name="component">Component that owns the changed property.</param>
		/// <param name="controller">Controller that owns the task.</param>
		/// <param name="sModulesList">Modules list delimited by ";".</param>
		/// <returns>Found Task</returns>
		protected Task GetTask(SmartComponent component, ref Controller controller, ref string sModulesList)
		{
			Task task = null;
			string sTasksList = "";
			if (controller == null)
				controller = GetController(component, ref sTasksList);

			string sTaskName = (string)component.Properties["Task"].Value;
			if ((sTaskName != "") && (controller != null))
			{
				task = controller.Rapid.GetTask(sTaskName);
				if (task != null)
				{
					string oldModule = (string)component.Properties["Module"].Value;
					bool bSetOldValue = (component.Properties["Module"].Attributes["AllowedValues"].ToString() == "") && (oldModule != "");
					foreach (Module module in task.GetModules())
					{
					sModulesList += ";" + module.Name;
					}
					if ((";" + sModulesList + ";").Contains(";" + oldModule + ";") && bSetOldValue)
					{
						component.Properties["Module"].Attributes["AllowedValues"] = sModulesList;
						Module module = null;
						GetGfxShapeData(component, ref controller, ref task, ref module);
						if (controller != null)
						{
							Logger.AddMessage("RSDrawGraphics: Connecting Component " + component.Name + " to " + controller.SystemId.ToString() + ". Use olds values.", LogMessageSeverity.Information);
							component.Properties["Status"].Value = "Connected";
						}
						if (task != null)
							try { task.Dispose(); } catch (System.ObjectDisposedException) { } //Need task._disposed public.
					}
				}
			}
			component.Properties["Module"].Attributes["AllowedValues"] = sModulesList;
			if (sModulesList == "")
			{
				component.Properties["Data"].Attributes["AllowedValues"] = "";
			}
			return task;
		}

		/// <summary>
		/// Get Module from Module Property and data(GfxShapeData) list.
		/// </summary>
		/// <param name="component">Component that owns the changed property.</param>
		/// <param name="controller">Controller that owns the task.</param>
		/// <param name="task">Task that owns the module.</param>
		/// <param name="sDatasList"></param>
		/// <returns>Found Module</returns>
		protected Module GetModule(SmartComponent component, ref Controller controller, ref Task task, ref string sDatasList)
		{
			Module module = null;
			sDatasList = "";
			string sModulesList = "";
			if (task == null)
				task = GetTask(component, ref controller, ref sModulesList);

			string sModuleName = (string)component.Properties["Module"].Value;
			if ((sModuleName != "") && (task != null))
			{
				module = task.GetModule(sModuleName);
				if (module != null)
				{
					string oldData = (string)component.Properties["Data"].Value;
					bool bSetOldValue = (component.Properties["Data"].Attributes["AllowedValues"].ToString() == "") && (oldData != "");
					RapidSymbolSearchProperties rssp = RapidSymbolSearchProperties.CreateDefault();
					rssp.GlobalSymbols = true;
					rssp.InUse = false;
					rssp.LocalSymbols = true;
					rssp.Recursive = false;
					rssp.SearchMethod = SymbolSearchMethod.Block;
					rssp.Types = SymbolTypes.Data;

					string typeName = "";// "GfxShapeData"; Don't find GfxShapeData Directly.
					RapidSymbol[] rsArray = module.SearchRapidSymbol(rssp, typeName, string.Empty);
					foreach (RapidSymbol rs in rsArray)
					{
						RapidData rd = module.GetRapidData(rs);
						if (rd.RapidType == "GfxShapeData")
						{
							sDatasList += ";" + rs.Name;
						}
					}
					//Sort Value as SearchRapidSymbol don't sort them (nor in declaration order)
					string[] stArray = sDatasList.Split(';');
					Array.Sort(stArray);
					sDatasList = string.Join(";", stArray);

					if ((";" + sDatasList + ";").Contains(";" + oldData + ";") && bSetOldValue)
					{
						component.Properties["Data"].Attributes["AllowedValues"] = sDatasList;
						GetGfxShapeData(component, ref controller, ref task, ref module);
						if (controller != null)
						{
							Logger.AddMessage("RSDrawGraphics: Connecting Component " + component.Name + " to " + controller.SystemId.ToString() + ". Use olds values.", LogMessageSeverity.Information);
							component.Properties["Status"].Value = "Connected";
						}
						if (task != null)
							try { task.Dispose(); } catch (System.ObjectDisposedException) { } //Need task._disposed public.
					}
				}
			}
			component.Properties["Data"].Attributes["AllowedValues"] = sDatasList;
			return module;
		}

		/// <summary>
		/// Get GfxShapeData values from Data Property.
		/// </summary>
		/// <param name="component">Component that owns the changed property.</param>
		/// <param name="controller">Controller that owns the task.</param>
		/// <param name="task">Task that owns the module.</param>
		/// <param name="module">Module that owns the data.</param>
		/// <returns>Found GfxShapeData values.</returns>
		protected GfxShapeData GetGfxShapeData(SmartComponent component, ref Controller controller, ref Task task, ref Module module)
		{
			GfxShapeData sd = default(GfxShapeData);
			string sDatasList = component.Properties["Data"].Attributes["AllowedValues"].ToString();
			if (module == null)
				module = GetModule(component, ref controller, ref task, ref sDatasList);

			string sDataName = (string)component.Properties["Data"].Value;
			if ((sDataName != "") && (module != null))
			{
				if ((";"+sDatasList+";").Contains(";"+sDataName+";"))
				{
					RapidData rd = module.GetRapidData(sDataName);
					if (rd.RapidType == "GfxShapeData")
					{
						RapidDataType rdt = controller.Rapid.GetRapidDataType(task.Name, module.Name, rd.Name);

						sd = new GfxShapeData(rdt, rd);
						if (sd.IsNotNull)
						{
							//Update Properties
							component.Properties["ShapeType"].Value = (sd.ShapeType == GfxShapeData.ShapeTypes.Box) ? "Box" : "Capsule";
							component.Properties["ParentAxis"].Value = sd.ParentAxis.Value;
							component.Properties["CurrentAxis"].Value = PoseToMatrix4(sd.CurrentAxis);
							component.Properties["Shift"].Value = PoseToMatrix4(sd.Shift);
							if (sd.ShapeType == GfxShapeData.ShapeTypes.Capsule)
							{ 
								component.Properties["Radius"].Value = (Double)(sd.Size.X / 1000);
								component.Properties["Length"].Value = (Double)(sd.Size.Y / 1000);
							} else
							{
								component.Properties["Length"].Value = (Double)(sd.Size.X / 1000);
								component.Properties["Height"].Value = (Double)(sd.Size.Y / 1000);
								component.Properties["Width"].Value = (Double)(sd.Size.Z / 1000);
							}
							double blue =  ((UInt32)sd.Color.Value & 0x000000FF) / 0x00000001;
							double green = ((UInt32)sd.Color.Value & 0x0000FF00) / 0x00000100;
							double red =   ((UInt32)sd.Color.Value & 0x00FF0000) / 0x00010000;
							double opac =  ((UInt32)sd.Color.Value & 0xFF000000) / 0x01000000;
							Vector3 vec = new Vector3(red, green, blue);
							component.Properties["Color"].Value = vec;
							component.Properties["Opacity"].Value = opac;
						}
					}
				}
				else
				{
					if ((string)component.Properties["Data"].Value != "")
						component.Properties["Data"].Value = "";
				}
			}
			return sd;
		}

		/// <summary>
		/// Set GfxShapeData values from Data Property.
		/// </summary>
		/// <param name="component">Component that owns the changed property.</param>
		/// <param name="controller">Controller that owns the task.</param>
		/// <param name="task">Task that owns the module.</param>
		/// <param name="module">Module that owns the data.</param>
		/// <returns>Found GfxShapeData values.</returns>
		protected GfxShapeData SetGfxShapeData(SmartComponent component, ref Controller controller, ref Task task, ref Module module)
		{
			GfxShapeData sd = default(GfxShapeData);
			string sDataList = "";
			if (module == null)
				module = GetModule(component, ref controller, ref task, ref sDataList);

			string sDataName = (string)component.Properties["Data"].Value;
			if ((sDataName != "") && (module != null))
			{
				if ((";" + sDataList + ";").Contains(";" + sDataName + ";"))
				{
					RapidData rd = module.GetRapidData(sDataName);
					if (rd.RapidType == "GfxShapeData")
					{
						RapidDataType rdt = controller.Rapid.GetRapidDataType(task.Name, module.Name, rd.Name);

						sd = new GfxShapeData(rdt, rd);
						if (sd.IsNotNull)
						{
							//Update Properties
							sd.ShapeType = ((string)component.Properties["ShapeType"].Value == "Box") ? GfxShapeData.ShapeTypes.Box : GfxShapeData.ShapeTypes.Capsule;
							sd.ParentAxis = new Num((double)((Int32)component.Properties["ParentAxis"].Value));
							sd.CurrentAxis = Matrix4ToPose((Matrix4)component.Properties["CurrentAxis"].Value);
							sd.Shift = Matrix4ToPose((Matrix4)component.Properties["Shift"].Value);

							Pos pos = new Pos();
							if (sd.ShapeType == GfxShapeData.ShapeTypes.Capsule)
							{
								pos.X = (float)((double)component.Properties["Radius"].Value * 1000);
								pos.Y = (float)((double)component.Properties["Length"].Value * 1000);
								pos.Z = 0;
							}
							else
							{
								pos.X = (float)((double)component.Properties["Length"].Value * 1000);
								pos.Y = (float)((double)component.Properties["Height"].Value * 1000);
								pos.Z = (float)((double)component.Properties["Width"].Value * 1000);
							}
							sd.Size = pos;
							Vector3 vec = (Vector3)component.Properties["Color"].Value;
							double color = vec.z * 0x00000001;
							color += vec.y * 0x00000100;
							color += vec.x * 0x00010000;
							color += (Int32)component.Properties["Opacity"].Value * 0x01000000;
							sd.Color = new Num(color);

							//If not connected, connect to it
							if (controller.CurrentUser == null)
								controller.Logon(new UserInfo("Default User", "robotics", "RSDrawGraphics"));

							// Request mastership, needed to write values
							using (Mastership ms = Mastership.Request(controller.Rapid))
							{
								rd.Value = sd;
							}
						}
					}
				}
				else
				{
					if ((string)component.Properties["Data"].Value != "")
						component.Properties["Data"].Value = "";
				}
			}
			else
			{
				//Create text with current Properties
				string str = "  RECORD GfxShapeData\n    num nType;\n    num nParentAxis;\n    pose poseCurrentAxis;\n    pose poseShift;\n    pos posSize;\n    dnum dnColor;\n    ENDRECORD\n";
				str += "  TASK PERS GfxShapeData sdValue:=[";
				//num nType
				GfxShapeData.ShapeTypes shType = ((string)component.Properties["ShapeType"].Value == "Box") ? GfxShapeData.ShapeTypes.Box : GfxShapeData.ShapeTypes.Capsule;
				str += (shType == GfxShapeData.ShapeTypes.Box) ? 2 : 1;
				str += ",";
				//num nParentAxis
				str += (Int32)component.Properties["ParentAxis"].Value;
				str += ",";
				//pose poseCurrentAxis
				str += Matrix4ToPose((Matrix4)component.Properties["CurrentAxis"].Value).ToString();
				str += ",";
				//pose poseShift
				str += Matrix4ToPose((Matrix4)component.Properties["Shift"].Value).ToString();
				str += ",";
				//pos posSize
				Pos pos = new Pos();
				if( shType == GfxShapeData.ShapeTypes.Capsule)
				{
					pos.X = (float)((double)component.Properties["Radius"].Value * 1000);
					pos.Y = (float)((double)component.Properties["Length"].Value * 1000);
					pos.Z = 0;
				}
				else
				{
					pos.X = (float)((double)component.Properties["Length"].Value * 1000);
					pos.Y = (float)((double)component.Properties["Height"].Value * 1000);
					pos.Z = (float)((double)component.Properties["Width"].Value * 1000);
				}
				str += pos.ToString();
				str += ",";
				//dnum dnColor
				Vector3 vec = (Vector3)component.Properties["Color"].Value;
				double color = vec.z * 0x00000001;
				color += vec.y * 0x00000100;
				color += vec.x * 0x00010000;
				color += (Int32)component.Properties["Opacity"].Value * 0x01000000;
				str += color.ToString();
				str += "];\n";
				Clipboard.SetText(str);
				component.Properties["Status"].Value = "In Clipboard";
				Logger.AddMessage("RSDrawGraphics: ShapeData copied in clipboard. Paste it in your module.");
			}
			return sd;
		}

		/// <summary>
		/// Rebuild Part from actual values.
		/// </summary>
		/// <param name="component">Smart Component</param>
		protected void RebuildPart(SmartComponent component)
		{
			// Remove old content
			Material oldMaterial = null;
			while (component.GraphicComponents.TryGetGraphicComponent("GeneratedPart", out GraphicComponent gc))
			{
				component.Properties["GeneratedPart"].Value = null;
				oldMaterial = ((Part)gc).GetMaterial();
				component.GraphicComponents.Remove(gc);
			}

			// Create a (hidden) part for the box geometry
			Part p = new Part
			{
				UIVisible = true,
				PickingEnabled = (bool)component.Properties["IsSelectable"].Value,
				Name = "GeneratedPart"
			};

			try
			{
				component.GraphicComponents.Add(p);
			}
			catch (System.UnauthorizedAccessException)
			{
				return;
			}
			component.RoleObject = p;

			CreateGeometry(component, p);

			//p.DeleteGeometry();

			if (oldMaterial != null && !oldMaterial.IsEmpty)
			{
				Vector3 color = (Vector3)component.Properties["Color"].Value;
				int opacity = (int)component.Properties["Opacity"].Value;

				oldMaterial.Ambient = Color.FromArgb(opacity, (int)color.x, (int)color.y, (int)color.z);
				//oldMaterial.Emissive = oldMaterial.Ambient;
				oldMaterial.Diffuse = oldMaterial.Ambient;
				p.SetMaterial(oldMaterial);
			}
			
			component.Properties["GeneratedPart"].Value = p;
		}

			/// <summary>
			/// Create geometry
			/// </summary>
			/// <param name="component">Smart Component</param>
			/// <param name="p">Part</param>
			protected void CreateGeometry(SmartComponent component, Part p)
		{
			double dblRadius = (double)component.Properties["Radius"].Value;
			double dblLength = (double)component.Properties["Length"].Value;
			double dblHeight = (double)component.Properties["Height"].Value;
			double dblWidth = (double)component.Properties["Width"].Value;

			bool blShowAtOrigin = (bool)component.Properties["ShowAtOrigin"].Value;
			Matrix4 m4CurrentAxis = (Matrix4)component.Properties["CurrentAxis"].Value;
			Matrix4 m4Shift = (Matrix4)component.Properties["Shift"].Value;

			bool blMakeAxisFrame = (bool)component.Properties["MakeAxisFrame"].Value;
			string frameName = (string)component.Properties["Data"].Value;
			if (blMakeAxisFrame && (frameName != ""))
			{
				Frame frame = new Frame();
				foreach (Frame curs in component.Frames)
				{
					if(curs.Name == frameName)
					{
						frame = curs;
					}
				}
				if (frame.Name=="")
				{
					frame.Name = frameName;
					component.Frames.Add(frame);
				}
				frame.Transform.Matrix = m4CurrentAxis;
			}
			
			if ( (dblRadius >= 0) && (dblLength >= 0) && (dblHeight >= 0) && (dblWidth >= 0) )
			{
				if (!blShowAtOrigin)
					m4Shift = m4CurrentAxis.Multiply(m4Shift);

				if ((string)component.Properties["ShapeType"].Value == "Capsule")
				{
					//Replace Capsule's origin
					m4Shift.TranslateLocal(new Vector3(0, 0, -dblRadius));
					p.Bodies.Add(Body.CreateSolidCapsule(m4Shift, dblRadius, dblLength + 2 * dblRadius));
					//Add Box to help to see origin orientation
					p.Bodies.Add(Body.CreateSolidBox(m4Shift, new Vector3(dblRadius / 2, dblRadius, dblRadius * 2)));
				} else
				{
					p.Bodies.Add(Body.CreateSolidBox(m4Shift, new Vector3(dblLength, dblHeight, dblWidth)));
					//Add Box to help to see origin orientation
					double dblMin = (dblLength <= dblHeight)
						? ((dblLength <= dblWidth) ? dblLength : dblWidth)
						: ((dblHeight <= dblWidth) ? dblHeight : dblWidth);
					m4Shift.TranslateLocal(new Vector3(-dblMin / 4, -dblMin / 2, -dblMin));
					p.Bodies.Add(Body.CreateSolidBox(m4Shift, new Vector3(dblMin / 4, dblMin / 2, dblMin)));
				}
				
			}
		}

		/// <summary>
		/// Transform Pose to Matrix4
		/// </summary>
		/// <param name="pose">Pose to transform</param>
		/// <returns>A Matrix4 with values from pose.</returns>
		private Matrix4 PoseToMatrix4(Pose pose)
		{
			return new Matrix4(new Vector3(pose.Trans.X/1000, pose.Trans.Y/1000, pose.Trans.Z/1000)
			                 , new Quaternion(pose.Rot.Q1, pose.Rot.Q2, pose.Rot.Q3, pose.Rot.Q4));
		}

		/// <summary>
		/// Transform Matrix4 to Pose
		/// </summary>
		/// <param name="matrix">Matrix4 to transform</param>
		/// <returns>A Pose with values from matrix.</returns>
		private Pose Matrix4ToPose(Matrix4 matrix)
		{
			Pose res = new Pose();
			res.Trans.X = (float)matrix.Translation.x*1000;
			res.Trans.Y = (float)matrix.Translation.y*1000;
			res.Trans.Z = (float)matrix.Translation.z*1000;

			res.Rot.Q1 = matrix.Quaternion.q1;
			res.Rot.Q2 = matrix.Quaternion.q2;
			res.Rot.Q3 = matrix.Quaternion.q3;
			res.Rot.Q4 = matrix.Quaternion.q4;

			return res;
		}
	}
}
