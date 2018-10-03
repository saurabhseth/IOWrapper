﻿using HidWizards.IOWrapper.DataTransferObjects;

namespace HidWizards.IOWrapper.ProviderInterface.Interfaces
{
    /// <inheritdoc />
    /// <summary>
    /// Provider supports Reading of Input
    /// </summary>
    public interface IInputProvider : IProvider
    {
        ProviderReport GetInputList();
        DeviceReport GetInputDeviceReport(InputSubscriptionRequest subReq);
        bool SubscribeInput(InputSubscriptionRequest subReq);
        bool UnsubscribeInput(InputSubscriptionRequest subReq);
    }
}