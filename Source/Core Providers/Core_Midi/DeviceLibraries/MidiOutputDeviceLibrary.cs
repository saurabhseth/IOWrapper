﻿using System;
using Hidwizards.IOWrapper.Libraries.DeviceLibrary;
using HidWizards.IOWrapper.DataTransferObjects;
using NAudio.Midi;

namespace Core_Midi.DeviceLibraries
{
    public partial class MidiDeviceLibrary
    {
        private ProviderReport _outputProviderReport;
        private DeviceReportNode _outputDeviceReportTemplate;

        public int GetOutputDeviceIdentifier(DeviceDescriptor deviceDescriptor)
        {
            if (_connectedOutputDevices.TryGetValue(deviceDescriptor.DeviceHandle, out var instances) &&
                instances.Count >= deviceDescriptor.DeviceInstance)
            {
                return instances[deviceDescriptor.DeviceInstance];
            }
            throw new Exception($"Could not find output device Handle {deviceDescriptor.DeviceHandle}, Instance {deviceDescriptor.DeviceInstance}");
        }

        public ProviderReport GetOutputList()
        {
            return _outputProviderReport;
        }

        public DeviceReport GetOutputDeviceReport(DeviceDescriptor deviceDescriptor)
        {
            if (!_connectedOutputDevices.TryGetValue(deviceDescriptor.DeviceHandle, out var deviceInstances)
                || deviceDescriptor.DeviceInstance >= deviceInstances.Count) return null;
            var devId = deviceInstances[deviceDescriptor.DeviceInstance];
            var infoOut = MidiOut.DeviceInfo(devId);
            var deviceReport = new DeviceReport
            {
                DeviceDescriptor = deviceDescriptor,
                DeviceName = infoOut.ProductName
            };
            deviceReport.Nodes = _outputDeviceReportTemplate.Nodes;

            return deviceReport;
        }

        private void BuildOutputDeviceList()
        {
            var providerReport = new ProviderReport
            {
                Title = "MIDI Output (Core)",
                Description = "Provides support for MIDI devices",
                API = "Midi",
                ProviderDescriptor = _providerDescriptor
            };
            foreach (var deviceIdList in _connectedOutputDevices)
            {
                for (var i = 0; i < deviceIdList.Value.Count; i++)
                {
                    var deviceDescriptor = new DeviceDescriptor { DeviceHandle = deviceIdList.Key, DeviceInstance = i };
                    providerReport.Devices.Add(GetOutputDeviceReport(deviceDescriptor));
                }

            }
            _outputProviderReport = providerReport;
        }

        private void BuildOutputDeviceReportTemplate()
        {
            var node = new DeviceReportNode();
            for (var channel = 0; channel < 16; channel++)
            {
                var channelInfo = new DeviceReportNode
                {
                    Title = $"CH {channel + 1}"
                };
                //var notesInfo = new DeviceReportNode
                //{
                //    Title = "Notes"
                //};
                //for (var octave = 0; octave < 10; octave++)
                //{
                //    var octaveInfo = new DeviceReportNode
                //    {
                //        Title = $"Octave {octave}"
                //    };
                //    for (var noteIndex = 0; noteIndex < NoteNames.Length; noteIndex++)
                //    {
                //        var noteName = NoteNames[noteIndex];
                //        octaveInfo.Bindings.Add(new BindingReport
                //        {
                //            Title = $"{noteName}",
                //            Category = BindingCategory.Signed,
                //            BindingDescriptor = BuildNoteDescriptor(channel, octave, noteIndex)
                //        });
                //    }
                //    notesInfo.Nodes.Add(octaveInfo);
                //}
                //channelInfo.Nodes.Add(notesInfo);

                var controlChangeInfo = new DeviceReportNode
                {
                    Title = "CtrlChange"
                };
                for (var controllerId = 0; controllerId < 128; controllerId++)
                {
                    controlChangeInfo.Bindings.Add(new BindingReport
                    {
                        Title = $"ID {controllerId}",
                        Category = BindingCategory.Signed,
                        Path = $"CH:{channel}, CC:{controllerId}",
                        BindingDescriptor = BuildControlChangeDescriptor(channel, controllerId)
                    });
                }
                channelInfo.Nodes.Add(controlChangeInfo);

                node.Nodes.Add(channelInfo);
            }

            _outputDeviceReportTemplate = node;

        }
    }
}
