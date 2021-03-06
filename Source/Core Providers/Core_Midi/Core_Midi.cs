﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hidwizards.IOWrapper.Libraries.DeviceHandlers.Devices;
using Hidwizards.IOWrapper.Libraries.DeviceLibrary;
using HidWizards.IOWrapper.DataTransferObjects;
using HidWizards.IOWrapper.ProviderInterface.Interfaces;
using NAudio.Midi;

namespace Core_Midi
{
    [Export(typeof(IProvider))]
    public class Core_Midi : IInputProvider, IOutputProvider, IBindModeProvider
    {
        private readonly IInputOutputDeviceLibrary<int> _deviceLibrary;
        private readonly ConcurrentDictionary<DeviceDescriptor, IDeviceHandler<MidiInMessageEventArgs>> _activeInputDevices
            = new ConcurrentDictionary<DeviceDescriptor, IDeviceHandler<MidiInMessageEventArgs>>();
        private readonly ConcurrentDictionary<DeviceDescriptor, MidiOutputDeviceHandler> _activeOutputDevices
            = new ConcurrentDictionary<DeviceDescriptor, MidiOutputDeviceHandler>();
        private Action<ProviderDescriptor, DeviceDescriptor, BindingReport, short> _bindModeCallback;
        private readonly object _lockObj = new object();  // When changing mode (Bind / Sub) or adding / removing devices, lock this object

        public Core_Midi()
        {
            _deviceLibrary = new DeviceLibraries.MidiDeviceLibrary(new ProviderDescriptor { ProviderName = ProviderName });
        }

        public void Dispose()
        {
            foreach (var device in _activeInputDevices)
            {
                device.Value.Dispose();
            }

            foreach (var device in _activeOutputDevices)
            {
                device.Value.Dispose();
            }
        }

        public string ProviderName { get; } = "Core_Midi";
        public bool IsLive { get; }
        public void RefreshLiveState()
        {
            
        }

        public void RefreshDevices()
        {
            _deviceLibrary.RefreshConnectedDevices();
        }

        public void SetDetectionMode(DetectionMode detectionMode, DeviceDescriptor deviceDescriptor, Action<ProviderDescriptor, DeviceDescriptor, BindingReport, short> callback = null)
        {
            lock (_lockObj)
            {
                var deviceExists = _activeInputDevices.TryGetValue(deviceDescriptor, out var deviceHandler);
                if (detectionMode == DetectionMode.Subscription)
                {
                    // Subscription Mode
                    if (!deviceExists) return;
                    deviceHandler.SetDetectionMode(DetectionMode.Subscription);
                }
                else
                {
                    // Bind Mode
                    if (!deviceExists)
                    {
                        deviceHandler = new MidiInputDeviceHandler(deviceDescriptor, InputDeviceEmptyHandler, BindModeHandler, _deviceLibrary);
                        _activeInputDevices.TryAdd(deviceDescriptor, deviceHandler);
                    }

                    _bindModeCallback = callback;
                    deviceHandler.SetDetectionMode(DetectionMode.Bind);
                }
            }
        }

        #region IIinputProvider
        public ProviderReport GetInputList()
        {
            return _deviceLibrary.GetInputList();
        }

        public DeviceReport GetInputDeviceReport(DeviceDescriptor deviceDescriptor)
        {
            return _deviceLibrary.GetInputDeviceReport(deviceDescriptor);
        }

        public bool SubscribeInput(InputSubscriptionRequest subReq)
        {
            lock (_lockObj)
            {
                if (!_activeInputDevices.TryGetValue(subReq.DeviceDescriptor, out var deviceHandler))
                {
                    deviceHandler = new MidiInputDeviceHandler(subReq.DeviceDescriptor, InputDeviceEmptyHandler, BindModeHandler, _deviceLibrary);
                    _activeInputDevices.TryAdd(subReq.DeviceDescriptor, deviceHandler);
                }
                deviceHandler.SubscribeInput(subReq);
                return true;
            }
        }

        private void BindModeHandler(object sender, BindModeUpdate e)
        {
            _bindModeCallback?.Invoke(new ProviderDescriptor { ProviderName = ProviderName }, e.Device, e.Binding, e.Value);
        }

        public bool UnsubscribeInput(InputSubscriptionRequest subReq)
        {
            lock (_lockObj)
            {
                if (_activeInputDevices.TryGetValue(subReq.DeviceDescriptor, out var deviceHandler))
                {
                    deviceHandler.UnsubscribeInput(subReq);
                }
                return true;
            }
        }

        private void InputDeviceEmptyHandler(object sender, DeviceDescriptor e)
        {
            _activeInputDevices[e].Dispose();
            _activeInputDevices.TryRemove(e, out _);
        }
        #endregion

        #region IOutputProvider

        public ProviderReport GetOutputList()
        {
            return _deviceLibrary.GetOutputList();
        }

        public DeviceReport GetOutputDeviceReport(DeviceDescriptor deviceDescriptor)
        {
            return _deviceLibrary.GetOutputDeviceReport(deviceDescriptor);
        }

        public bool SubscribeOutputDevice(OutputSubscriptionRequest subReq)
        {
            if (!_activeOutputDevices.TryGetValue(subReq.DeviceDescriptor, out var deviceHandler))
            {
                deviceHandler = new MidiOutputDeviceHandler(subReq.DeviceDescriptor, _deviceLibrary.GetOutputDeviceIdentifier(subReq.DeviceDescriptor), OutputDeviceEmptyHandler);
                _activeOutputDevices.TryAdd(subReq.DeviceDescriptor, deviceHandler);
            }

            _activeOutputDevices[subReq.DeviceDescriptor].SubscribeOutput(subReq);
            return true;
        }

        private void OutputDeviceEmptyHandler(object sender, DeviceDescriptor e)
        {
            _activeOutputDevices[e].Dispose();
            _activeOutputDevices.TryRemove(e, out _);
        }

        public bool UnSubscribeOutputDevice(OutputSubscriptionRequest subReq)
        {
            if (_activeOutputDevices.TryGetValue(subReq.DeviceDescriptor, out var deviceHandler))
            {
                deviceHandler.UnsubscribeOutput(subReq);
                return true;
            }
            return false;
        }

        public bool SetOutputState(OutputSubscriptionRequest subReq, BindingDescriptor bindingDescriptor, int state)
        {
            if (_activeOutputDevices.TryGetValue(subReq.DeviceDescriptor, out var deviceHandler))
            {
                deviceHandler.SetOutputState(subReq, bindingDescriptor, state);
                return true;
            }
            return false;
        }

        #endregion
    }
}
