﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HidWizards.IOWrapper.DataTransferObjects;
using HidWizards.IOWrapper.ProviderInterface.Devices;
using HidWizards.IOWrapper.ProviderInterface.Subscriptions;
using SharpDX.XInput;

namespace SharpDX_XInput
{
    public class XiDevice : IDisposable
    {
        private DeviceDescriptor _deviceDescriptor;
        private IDeviceLibrary<int> _deviceLibrary;
        private SubscriptionHandler _subHandler;
        private XiDeviceUpdateHandler _deviceUpdateHandler;
        private Thread _pollThread;
        private readonly Controller _controller;
        public EventHandler<BindModeUpdate> BindModeUpdate;
        private EventHandler<DeviceDescriptor> _deviceEmptyHandler;

        public XiDevice(DeviceDescriptor deviceDescriptor, IDeviceLibrary<int> deviceLibrary, EventHandler<DeviceDescriptor> deviceEmptyHandler)
        {
            _deviceDescriptor = deviceDescriptor;
            _deviceLibrary = deviceLibrary;
            _subHandler = new SubscriptionHandler(deviceDescriptor, DeviceEmptyHandler);
            _deviceUpdateHandler = new XiDeviceUpdateHandler(deviceDescriptor, _subHandler);
            _deviceUpdateHandler.BindModeUpdate = BindModeHandler;
            _deviceEmptyHandler = deviceEmptyHandler;
            _controller = new Controller((UserIndex)deviceDescriptor.DeviceInstance);

            _pollThread = new Thread(PollThread);
            _pollThread.Start();
        }

        private void BindModeHandler(object sender, BindModeUpdate e)
        {
            BindModeUpdate?.Invoke(sender, e);
        }

        public void SubscribeInput(InputSubscriptionRequest subReq)
        {
            _subHandler.Subscribe(subReq);
        }

        public void UnsubscribeInput(InputSubscriptionRequest subReq)
        {
            _subHandler.Unsubscribe(subReq);
        }

        private void DeviceEmptyHandler(object sender, DeviceDescriptor e)
        {
            _deviceEmptyHandler?.Invoke(sender, e);
        }

        private void PollThread()
        {
            while (true)
            {
                if (!_controller.IsConnected)
                    return;
                _deviceUpdateHandler.ProcessUpdate(_controller.GetState());
                Thread.Sleep(10);
            }
        }

        public void Dispose()
        {
            _pollThread.Abort();
            _pollThread.Join();
            _pollThread = null;
        }

        public bool IsEmpty()
        {
            return _subHandler.Count() == 0;
        }

        public void SetDetectionMode(DetectionMode detectionMode)
        {
            _deviceUpdateHandler.SetDetectionMode(detectionMode);
        }
    }
}
