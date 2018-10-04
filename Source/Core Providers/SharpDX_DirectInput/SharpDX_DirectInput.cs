﻿using SharpDX.DirectInput;
using System.ComponentModel.Composition;
using HidWizards.IOWrapper.ProviderInterface;
using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Win32;
using System.Linq;
using System.Diagnostics;
using DeviceLibrary;
using HidWizards.IOWrapper.DataTransferObjects;
using HidWizards.IOWrapper.ProviderInterface.Interfaces;
using PollingDeviceHandlers.Devices;
using ProviderLogger;

namespace SharpDX_DirectInput
{
    [Export(typeof(IProvider))]
    public class SharpDX_DirectInput : IInputProvider, IBindModeProvider
    {
        private readonly Dictionary<DeviceDescriptor, PollingDeviceHandler<JoystickUpdate, (BindingType, int)>> _activeDevices
            = new Dictionary<DeviceDescriptor, PollingDeviceHandler<JoystickUpdate, (BindingType, int)>>();
        private readonly IInputDeviceLibrary<Guid> _deviceLibrary;
        private Action<ProviderDescriptor, DeviceDescriptor, BindingDescriptor, int> _bindModeCallback;

        public bool IsLive { get; } = true;

        private Logger _logger;

        private bool _disposed;

        // Handles subscriptions and callbacks

        public SharpDX_DirectInput()
        {
            _deviceLibrary = new DiDeviceLibrary(new ProviderDescriptor {ProviderName = ProviderName});
            _logger = new Logger(ProviderName);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;
            if (disposing)
            {
                //_subscriptionHandler.Dispose();
            }
            _disposed = true;
        }

        #region IProvider Members

        // ToDo: Need better way to handle this. MEF meta-data?
        public string ProviderName { get { return typeof(SharpDX_DirectInput).Namespace; } }

        public ProviderReport GetInputList()
        {
            return _deviceLibrary.GetInputList();
        }

        public DeviceReport GetInputDeviceReport(InputSubscriptionRequest subReq)
        {
            return _deviceLibrary.GetInputDeviceReport(subReq.DeviceDescriptor);
        }

        public bool SubscribeInput(InputSubscriptionRequest subReq)
        {
            if (!_activeDevices.TryGetValue(subReq.DeviceDescriptor, out var deviceHandler))
            {
                deviceHandler = new DiDeviceHandler(subReq.DeviceDescriptor, _deviceLibrary.GetDeviceIdentifier(subReq.DeviceDescriptor)).Initialize(DeviceEmptyHandler, BindModeHandler);
                _activeDevices.Add(subReq.DeviceDescriptor, deviceHandler);
            }
            deviceHandler.SubscribeInput(subReq);
            return true;
        }

        public bool UnsubscribeInput(InputSubscriptionRequest subReq)
        {
            if (_activeDevices.TryGetValue(subReq.DeviceDescriptor, out var deviceHandler))
            {
                deviceHandler.UnsubscribeInput(subReq);
            }
            return true;
        }

        public void SetDetectionMode(DetectionMode detectionMode, DeviceDescriptor deviceDescriptor, Action<ProviderDescriptor, DeviceDescriptor, BindingDescriptor, int> callback = null)
        {
            if (!_activeDevices.TryGetValue(deviceDescriptor, out var deviceHandler))
            {
                deviceHandler = new DiDeviceHandler(deviceDescriptor, _deviceLibrary.GetDeviceIdentifier(deviceDescriptor)).Initialize(DeviceEmptyHandler, BindModeHandler);
                _activeDevices.Add(deviceDescriptor, deviceHandler);
            }

            if (detectionMode == DetectionMode.Bind)
            {
                _bindModeCallback = callback;
            }

            if (detectionMode == DetectionMode.Subscription && deviceHandler.IsEmpty())
            {
                deviceHandler.Dispose();
                _activeDevices.Remove(deviceDescriptor);
            }
            else
            {
                deviceHandler.SetDetectionMode(detectionMode);
            }
        }

        private void DeviceEmptyHandler(object sender, DeviceDescriptor e)
        {
            _activeDevices[e].Dispose();
            _activeDevices.Remove(e);
        }

        private void BindModeHandler(object sender, BindModeUpdate e)
        {
            _bindModeCallback?.Invoke(new ProviderDescriptor{ProviderName = ProviderName}, e.Device, e.Binding, e.Value);
        }

        public void RefreshLiveState()
        {
            // Built-in API, take no action
        }

        public void RefreshDevices()
        {
            _deviceLibrary.RefreshConnectedDevices();
        }
        #endregion
    }
}