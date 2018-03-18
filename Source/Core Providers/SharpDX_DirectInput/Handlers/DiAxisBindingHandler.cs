﻿using HidWizards.IOWrapper.ProviderInterface;
using HidWizards.IOWrapper.ProviderInterface.Handlers;
using HidWizards.IOWrapper.DataTransferObjects;
using SharpDX_DirectInput.Helpers;

namespace SharpDX_DirectInput.Handlers
{
    public class DiAxisBindingHandler : BindingHandler
    {
        public DiAxisBindingHandler(InputSubscriptionRequest subReq) : base(subReq) { }

        public override int ConvertValue(int value)
        {
            return Lookups.ConvertAxisValue(value);
        }
    }
}