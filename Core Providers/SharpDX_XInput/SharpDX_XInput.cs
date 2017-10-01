﻿using System;
using System.ComponentModel.Composition;
using Providers;
using System.Collections.Generic;
using SharpDX.XInput;
using System.Threading;
using System.Diagnostics;

namespace SharpDX_XInput
{
    [Export(typeof(IProvider))]
    public class SharpDX_XInput : IProvider
    {
        public bool IsLive { get { return isLive; } }
        private bool isLive = true;

        bool disposed = false;

        // The thread which handles input detection
        private Thread pollThread;
        // Is the thread currently running? This is set by the thread itself.
        private volatile bool pollThreadRunning = false;
        // Do we want the thread to be on or off?
        // This is independent of whether or not the thread is running...
        // ... for example, we may be updating bindings, so the thread may be temporarily stopped
        private bool pollThreadDesired = false;
        // Is the thread in an Active or Inactive state?
        private bool pollThreadActive = false;

        private Dictionary<int, StickMonitor> MonitoredSticks = new Dictionary<int, StickMonitor>();
        private static List<Guid> ActiveProfiles = new List<Guid>();
        //private static List<> PluggedInControllers

        //ProviderReport providerReport;
        private List<DeviceReport> deviceReports;

        private static List<string> buttonNames = new List<string>() { "A", "B", "X", "Y", "LB", "RB", "LS", "RS", "Back", "Start" };
        private static List<string> axisNames = new List<string>() { "LX", "LY", "RX", "RY", "LT", "RT"};
        private static List<string> povNames = new List<string>() { "Up", "Right", "Down", "Left" };

        private static DeviceReportNode buttonInfo;
        /*= new DeviceReportNode()
        {
            Title = "Buttons",
            Bindings =
            {
                new BindingInfo() { Index = 0, Title = "A", Type = BindingType.Button, Category = BindingCategory.Momentary },
                new BindingInfo() { Index = 1, Title = "B", Type = BindingType.Button, Category = BindingCategory.Momentary },
                new BindingInfo() { Index = 2, Title = "X", Type = BindingType.Button, Category = BindingCategory.Momentary },
                new BindingInfo() { Index = 3, Title = "Y", Type = BindingType.Button, Category = BindingCategory.Momentary },
                new BindingInfo() { Index = 4, Title = "LB", Type = BindingType.Button, Category = BindingCategory.Momentary },
                new BindingInfo() { Index = 5, Title = "RB", Type = BindingType.Button, Category = BindingCategory.Momentary },
                new BindingInfo() { Index = 6, Title = "LS", Type = BindingType.Button, Category = BindingCategory.Momentary },
                new BindingInfo() { Index = 7, Title = "RS", Type = BindingType.Button, Category = BindingCategory.Momentary },
                new BindingInfo() { Index = 8, Title = "Back", Type = BindingType.Button, Category = BindingCategory.Momentary },
                new BindingInfo() { Index = 9, Title = "Start", Type = BindingType.Button, Category = BindingCategory.Momentary },
            }
        };*/

        private static DeviceReportNode axisInfo;
        /*= new DeviceReportNode()
        {
            Title = "Axes",
            Bindings = 
            {
                new BindingInfo() { Index = 0, Title = "LX", Type = BindingType.Axis, Category = BindingCategory.Signed },
                new BindingInfo() { Index = 1, Title = "LY", Type = BindingType.Axis, Category = BindingCategory.Signed },
                new BindingInfo() { Index = 2, Title = "RX", Type = BindingType.Axis, Category = BindingCategory.Signed },
                new BindingInfo() { Index = 3, Title = "RY", Type = BindingType.Axis, Category = BindingCategory.Signed },
                new BindingInfo() { Index = 4, Title = "LT", Type = BindingType.Axis, Category = BindingCategory.Unsigned },
                new BindingInfo() { Index = 5, Title = "RT", Type = BindingType.Axis, Category = BindingCategory.Unsigned },
            }
        };*/

        private static DeviceReportNode povInfo;
        /*= new DeviceReportNode()
        {
            Title = "D-Pad",
            Bindings =
            {
                new BindingInfo() { Index = 0, Title = "Up", Type = BindingType.POV, Category = BindingCategory.Momentary },
                new BindingInfo() { Index = 2, Title = "Down", Type = BindingType.POV, Category = BindingCategory.Momentary },
                new BindingInfo() { Index = 3, Title = "Left", Type = BindingType.POV, Category = BindingCategory.Momentary },
                new BindingInfo() { Index = 1, Title = "Right", Type = BindingType.POV, Category = BindingCategory.Momentary },
            }
        };
        */

        private static List<string> xinputAxisIdentifiers = new List<string>()
        {
            "LeftThumbX", "LeftThumbY", "LeftTrigger", "RightThumbX", "RightThumbY", "RightTrigger"
        };

        private static List<GamepadButtonFlags> xinputButtonIdentifiers = new List<GamepadButtonFlags>()
        {
            GamepadButtonFlags.A, GamepadButtonFlags.B, GamepadButtonFlags.X, GamepadButtonFlags.Y
            , GamepadButtonFlags.LeftShoulder, GamepadButtonFlags.RightShoulder
            , GamepadButtonFlags.LeftThumb, GamepadButtonFlags.RightThumb
            , GamepadButtonFlags.Back, GamepadButtonFlags.Start
        };

        private static List<GamepadButtonFlags> xinputPovDirectionIdentifiers = new List<GamepadButtonFlags>()
        {
            GamepadButtonFlags.DPadUp, GamepadButtonFlags.DPadRight, GamepadButtonFlags.DPadDown, GamepadButtonFlags.DPadLeft
        };


        public SharpDX_XInput()
        {
            BuildButtonList();
            pollThreadDesired = true;
            QueryDevices();
            pollThread = new Thread(PollThread);
            pollThread.Start();
        }

        private void BuildButtonList()
        {
            buttonInfo = new DeviceReportNode()
            {
                Title = "Buttons"
            };
            for (int b = 0; b < 10; b++)
            {
                buttonInfo.Bindings.Add(new BindingReport()
                {
                    Title = buttonNames[b],
                    Category = BindingCategory.Momentary,
                    BindingDescriptor = new BindingDescriptor()
                    {
                        Index = b,
                        Type = BindingType.Button,
                    }
                });
            }

            axisInfo = new DeviceReportNode()
            {
                Title = "Axes"
            };
            for (int a = 0; a < 6; a++)
            {
                axisInfo.Bindings.Add(new BindingReport()
                {
                    Title = axisNames[a],
                    Category = ( a < 4 ? BindingCategory.Signed : BindingCategory.Unsigned),
                    BindingDescriptor = new BindingDescriptor()
                    {
                        Index = a,
                        Type = BindingType.Axis,
                    }
                });
            }

            povInfo = new DeviceReportNode()
            {
                Title = "DPad"
            };
            for (int d = 0; d < 4; d++)
            {
                povInfo.Bindings.Add(new BindingReport()
                {
                    Title = povNames[d],
                    Category = BindingCategory.Momentary,
                    BindingDescriptor = new BindingDescriptor()
                    {
                        Index = d,
                        Type = BindingType.POV,
                    }
                });
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;
            if (disposing)
            {
                pollThread.Abort();
                pollThreadRunning = false;
                Log("Stopped PollThread for {0}", ProviderName);
            }
            disposed = true;
            Log("Provider {0} was Disposed", ProviderName);
        }

        private void SetPollThreadState(bool state)
        {
            if (!pollThreadRunning)
                return;

            if (state && !pollThreadActive)
            {
                pollThreadDesired = true;
                while (!pollThreadActive)
                {
                    Thread.Sleep(10);
                }
                Log("PollThread for {0} Activated", ProviderName);
            }
            else if (!state && pollThreadActive)
            {
                pollThreadDesired = false;
                while (pollThreadActive)
                {
                    Thread.Sleep(10);
                }
                Log("PollThread for {0} De-Activated", ProviderName);
            }
        }

        private static void Log(string formatStr, params object[] arguments)
        {
            Debug.WriteLine(String.Format("IOWrapper| " + formatStr, arguments));
        }
        #region IProvider Members
        public string ProviderName { get { return typeof(SharpDX_XInput).Namespace; } }

        // This should probably be a default interface method once they get added to C#
        // https://github.com/dotnet/csharplang/blob/master/proposals/default-interface-methods.md
        public bool SetProfileState(Guid profileGuid, bool state)
        {
            //if (pollThreadRunning)
            //    SetPollThreadState(false);

            if (state)
            {
                if (!ActiveProfiles.Contains(profileGuid))
                {
                    ActiveProfiles.Add(profileGuid);
                }
            }
            else
            {
                if (ActiveProfiles.Contains(profileGuid))
                {
                    ActiveProfiles.Remove(profileGuid);
                }
            }

            //if (pollThreadDesired)
            //    SetPollThreadState(true);

            return true;
        }

        public ProviderReport GetInputList()
        {
            var providerReport = new ProviderReport()
            {
                Title = "XInput (Core)",
                Description = "Reads Xbox gamepads",
                API = "XInput",
                ProviderDescriptor = new ProviderDescriptor()
                {
                    ProviderName = ProviderName,
                },
                Devices = deviceReports
            };

            return providerReport;
        }

        public ProviderReport GetOutputList()
        {
            return null;
        }

        public DeviceReport GetInputDeviceReport(InputSubscriptionRequest subReq)
        {
            return deviceReports[subReq.DeviceDescriptor.DeviceInstance];
        }

        public DeviceReport GetOutputDeviceReport(OutputSubscriptionRequest subReq)
        {
            return null;
        }

        private void QueryDevices()
        {
            for (int i = 0; i < 4; i++)
            {
                var ctrlr = new Controller((UserIndex)i);
                //if (ctrlr.IsConnected)
                //{
                    deviceReports.Add(BuildXInputDevice(i));
                //}
            }
        }

        public bool SubscribeInput(InputSubscriptionRequest subReq)
        {
            var prev_state = pollThreadActive;
            if (pollThreadActive)
                SetPollThreadState(false);

            var stickId = Convert.ToInt32(subReq.DeviceDescriptor.DeviceHandle);
            if (!MonitoredSticks.ContainsKey(stickId))
            {
                MonitoredSticks.Add(stickId, new StickMonitor(stickId));
            }
            var result = MonitoredSticks[stickId].Add(subReq);
            if (result)
            {
                if (prev_state)
                {
                    SetPollThreadState(true);
                }
                return true;
            }
            return false;
        }

        public bool UnsubscribeInput(InputSubscriptionRequest subReq)
        {
            var prev_state = pollThreadActive;
            if (pollThreadActive)
                SetPollThreadState(false);

            bool ret = false;
            var stickId = Convert.ToInt32(subReq.DeviceDescriptor.DeviceHandle);
            if (MonitoredSticks.ContainsKey(stickId))
            {
                // Remove from monitor lookup table
                MonitoredSticks[stickId].Remove(subReq);
                // If this was the last thing monitored on this stick...
                ///...remove the stick from the monitor lookup table
                if (!MonitoredSticks[stickId].HasSubscriptions())
                {
                    MonitoredSticks.Remove(stickId);
                }
                ret = true;
            }
            if (prev_state)
            {
                SetPollThreadState(true);
            }
            return ret;
        }

        public bool SubscribeOutputDevice(OutputSubscriptionRequest subReq)
        {
            return false;
        }

        public bool UnSubscribeOutputDevice(OutputSubscriptionRequest subReq)
        {
            return false;
        }

        public bool SetOutputState(OutputSubscriptionRequest subReq, BindingDescriptor bindingDescriptor, int state)
        {
            return false;
        }

        public void RefreshLiveState()
        {

        }
        #endregion

        private DeviceReport BuildXInputDevice(int id)
        {
            return new DeviceReport()
            {
                DeviceName = "Xbox Controller " + (id + 1),
                DeviceDescriptor = new DeviceDescriptor()
                {
                    DeviceHandle = id.ToString(),
                },
                Nodes = { buttonInfo, axisInfo , povInfo}
                //ButtonCount = 11,
                //ButtonList = buttonInfo,
                //AxisList = axisInfo,
            };
        }

        #region Stick Monitoring
        private void PollThread()
        {
            pollThreadRunning = true;
            Log("Started PollThread for {0}", ProviderName);
            while (true)
            {
                if (pollThreadDesired)
                {
                    pollThreadActive = true;
                    while (pollThreadDesired)
                    {
                        foreach (var monitoredStick in MonitoredSticks)
                        {
                            monitoredStick.Value.Poll();
                        }
                        Thread.Sleep(1);
                    }
                }
                else
                {
                    pollThreadActive = false;
                    while (!pollThreadDesired)
                    {
                        Thread.Sleep(1);
                    }
                }
            }
        }

        #region Stick
        public class StickMonitor
        {
            private int controllerId;
            private Controller controller;

            private Dictionary<int, InputMonitor> axisMonitors = new Dictionary<int, InputMonitor>();
            private Dictionary<int, InputMonitor> buttonMonitors = new Dictionary<int, InputMonitor>();
            private Dictionary<int, InputMonitor> povDirectionMonitors = new Dictionary<int, InputMonitor>();

            Dictionary<BindingType, Dictionary<int, InputMonitor>> monitors = new Dictionary<BindingType, Dictionary<int, InputMonitor>>();

            public StickMonitor(int cid)
            {
                controllerId = cid;
                controller = new Controller((UserIndex)controllerId);
                monitors.Add(BindingType.Axis, axisMonitors);
                monitors.Add(BindingType.Button, buttonMonitors);
                monitors.Add(BindingType.POV, povDirectionMonitors);
            }

            public bool Add(InputSubscriptionRequest subReq)
            {
                var inputId = subReq.BindingDescriptor.Index;
                var monitor = monitors[subReq.BindingDescriptor.Type];
                if (!monitor.ContainsKey(inputId))
                {
                    monitor.Add(inputId, new InputMonitor());
                }
                Log("Adding subscription to XI device Handle {0}, Type {1}, Input {2}", controllerId, subReq.BindingDescriptor.Type.ToString(), subReq.BindingDescriptor.Index);
                return monitor[inputId].Add(subReq);
            }

            public bool Remove(InputSubscriptionRequest subReq)
            {
                var inputId = subReq.BindingDescriptor.Index;
                var monitor = monitors[subReq.BindingDescriptor.Type];
                if (monitor.ContainsKey(inputId))
                {
                    Log("Removing subscription to XI device Handle {0}, Type {1}, Input {2}", controllerId, subReq.BindingDescriptor.Type.ToString(), subReq.BindingDescriptor.Index);
                    var ret = monitor[inputId].Remove(subReq);
                    if (!monitor[inputId].HasSubscriptions())
                    {
                        monitor.Remove(inputId);
                    }
                    return ret;
                }
                return false;
            }

            public bool HasSubscriptions()
            {
                foreach (var monitorSet in monitors)
                {
                    foreach (var monitor in monitorSet.Value)
                    {
                        if (monitor.Value.HasSubscriptions())
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            public void Poll()
            {
                if (!controller.IsConnected)
                    return;
                var state = controller.GetState();

                foreach (var monitor in axisMonitors)
                {
                    var value = Convert.ToInt32(state.Gamepad.GetType().GetField(xinputAxisIdentifiers[(int)monitor.Key]).GetValue(state.Gamepad));
                    monitor.Value.ProcessPollResult(value);
                }

                foreach (var monitor in buttonMonitors)
                {
                    var flag = state.Gamepad.Buttons & xinputButtonIdentifiers[(int)monitor.Key];
                    var value = Convert.ToInt32(flag != GamepadButtonFlags.None);
                    monitor.Value.ProcessPollResult(value);
                }

                foreach (var monitor in povDirectionMonitors)
                {
                    var flag = state.Gamepad.Buttons & xinputPovDirectionIdentifiers[(int)monitor.Key];
                    var value = Convert.ToInt32(flag != GamepadButtonFlags.None);
                    monitor.Value.ProcessPollResult(value);
                }
            }
        }
        #endregion

        #region Input
        public class InputMonitor
        {
            Dictionary<Guid, InputSubscriptionRequest> subscriptions = new Dictionary<Guid, InputSubscriptionRequest>();
            private int currentValue = 0;

            public bool Add(InputSubscriptionRequest subReq)
            {
                Log("XI adding subreq. Provider {0}, Device {1}, Input {2}, Guid {3}", subReq.ProviderDescriptor.ProviderName, subReq.DeviceDescriptor.DeviceHandle, subReq.BindingDescriptor.Index, subReq.SubscriptionDescriptor.SubscriberGuid);
                subscriptions.Add(subReq.SubscriptionDescriptor.SubscriberGuid, subReq);
                return true;
            }

            public bool Remove(InputSubscriptionRequest subReq)
            {
                Log("XI removing subreq. Provider {0}, Device {1}, Input {2}, Guid {3}", subReq.ProviderDescriptor.ProviderName, subReq.DeviceDescriptor.DeviceHandle, subReq.BindingDescriptor.Index, subReq.SubscriptionDescriptor.SubscriberGuid);
                if (subscriptions.ContainsKey(subReq.SubscriptionDescriptor.SubscriberGuid))
                {
                    return subscriptions.Remove(subReq.SubscriptionDescriptor.SubscriberGuid);
                }
                return false;
            }

            public bool HasSubscriptions()
            {
                return subscriptions.Count > 0;
            }

            public void ProcessPollResult(int value)
            {
                // XInput does not report just the changed values, so filter out anything that has not changed
                if (currentValue == value)
                    return;
                currentValue = value;
                foreach (var subscription in subscriptions.Values)
                {
                    if (ActiveProfiles.Contains(subscription.SubscriptionDescriptor.ProfileGuid))
                    {
                        subscription.Callback(value);
                    }
                }
            }
        }

        #endregion

        #endregion
    }
}
