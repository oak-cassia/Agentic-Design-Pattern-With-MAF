using System.ComponentModel;

namespace Agentic_Design_Pattern_With_MAF.Services;

public class WeatherService
{
    [Description("Get the weather for a given location.")]
    public static string GetWeather([Description("The location to get the weather for.")] string location)
        => $"The weather in {location} is cloudy with a high of 15°C.";
}
