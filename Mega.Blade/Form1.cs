using Mega.Platform.Core.Client;
using Mega.Platform.Core.Client.Command;
using Mega.Platform.Core.Models;

namespace Mega.Blade;

public partial class Form1 : Form
{
    private readonly IDevice _device;

    public Form1(IDevice device)
    {
        InitializeComponent();
        _device = device;
        Text = $"当前设备：{device.ProductId}/{device.DeviceId}";
    }

    private void button1_Click(object sender, EventArgs e)
    {
        MessageBox.Show(this, "button1_Click!");
    }

    private void button2_Click(object sender, EventArgs e)
    {
        MessageBox.Show(this, "button2_Click!");
    }

    private void CustomEvent(string eventName, object eventData)
    {
        MessageBox.Show(this, $"event_name: {eventName}, data: {eventData}");
    }

    private void Form1_Load(object sender, EventArgs e)
    {
        _device.AddCommand(new CommandDescriptor("button1_Click", "aoi/form1/button1_Click", CommandUsageScope.Both),
            async (deviceServiceInvokeRequest) =>
        {
            button1_Click(this, new EventArgs());
            return DeviceServiceInvokeResponse.SUCCESS;
        });

        _device.AddCommand(new CommandDescriptor("button2_Click", "aoi/form1/button2_Click", CommandUsageScope.Both),
           async (deviceServiceInvokeRequest) =>
           {
               button2_Click(this, new EventArgs());
               return DeviceServiceInvokeResponse.SUCCESS;
           });

        _device.AddCommand(new CommandDescriptor("custom_event", "aoi/form1/custom_event", CommandUsageScope.Both),
           async (deviceServiceInvokeRequest) =>
           {
               button1_Click(this, new EventArgs());
               deviceServiceInvokeRequest.Params.TryGetValue("event_name", out var eventName);
               deviceServiceInvokeRequest.Params.TryGetValue("data", out var data);
               CustomEvent(eventName?.ToString() ?? string.Empty, data ?? string.Empty);
               return DeviceServiceInvokeResponse.SUCCESS;
           });
    }
}
