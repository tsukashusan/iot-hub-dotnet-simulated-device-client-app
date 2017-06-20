namespace SimulatedDevice
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Newtonsoft.Json;

    public class Program
    {
        private const string IotHubUri = "{iot hub hostname}";
        private const string DeviceKey = "{device key}";
        private const string DeviceId = "myFirstDevice";
        private const double MinTemperature = 20;
        private const double MinHumidity = 60;
        private static readonly Random Rand = new Random();
        private static DeviceClient _deviceClient;
        private static int _messageId = 1;
        private const uint RETRY_MAX = 5;

        private static async void SendDeviceToCloudMessagesAsync()
        {
            while (true)
            {
                var currentTemperature = MinTemperature + Rand.NextDouble() * 15;
                var currentHumidity = MinHumidity + Rand.NextDouble() * 20;

                var telemetryDataPoint = new
                {
                    messageId = _messageId++,
                    deviceId = DeviceId,
                    temperature = currentTemperature,
                    humidity = currentHumidity
                };

                for(uint retryCount = 0; retryCount < RETRY_MAX; retryCount++) {
                    try
                    {
                        var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
                        var message = new Message(Encoding.ASCII.GetBytes(messageString));
                        message.Properties.Add("temperatureAlert", (currentTemperature > 30) ? "true" : "false");

                        var task = _deviceClient.SendEventAsync(message);
                        await task;

                        Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);

                        await Task.Delay(1000);
                        break;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("{0} > Exception: {1}", DateTime.Now, e.Message);
                        Console.WriteLine("StackTrace\n {0}", e.StackTrace);
                        await Task.Delay(500);
                    }
                }
            }
        }

        private static void Main(string[] args)
        {
            Console.WriteLine("Simulated device\n");
            _deviceClient = DeviceClient.Create(IotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey(DeviceId, DeviceKey), TransportType.Mqtt);
            _deviceClient.RetryPolicy = RetryPolicyType.Exponential_Backoff_With_Jitter;
            SendDeviceToCloudMessagesAsync();
            Console.ReadLine();
        }
    }
}
