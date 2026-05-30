using Microsoft.AspNetCore.Mvc;

namespace PromAlertmanager.Controllers;

[ApiController]
[Route("alert")]
public class AlertWebhookController : ControllerBase
{
    [HttpPost]
    public IActionResult Receive([FromBody] AlertmanagerWebhook payload)
    {
        // 这里先简单打印，后续可入库/转发企业微信
        Console.WriteLine($"Receiver: {payload.receiver}, Status: {payload.status}, Alerts: {payload.alerts?.Count ?? 0}");

        if (payload.alerts is not null)
        {
            foreach (var a in payload.alerts)
            {
                var name = a.labels?.GetValueOrDefault("alertname");
                var severity = a.labels?.GetValueOrDefault("severity");
                var summary = a.annotations?.GetValueOrDefault("summary");
                Console.WriteLine($"[{severity}] {name} - {summary}");
            }
        }

        return Ok(new { ok = true });
    }
}

public class AlertmanagerWebhook
{
    public string? version { get; set; }
    public string? groupKey { get; set; }
    public string? status { get; set; }
    public string? receiver { get; set; }
    public List<AlertItem>? alerts { get; set; }
    public Dictionary<string, string>? groupLabels { get; set; }
    public Dictionary<string, string>? commonLabels { get; set; }
    public Dictionary<string, string>? commonAnnotations { get; set; }
    public string? externalURL { get; set; }
}

public class AlertItem
{
    public string? status { get; set; }
    public Dictionary<string, string>? labels { get; set; }
    public Dictionary<string, string>? annotations { get; set; }
    public string? startsAt { get; set; }
    public string? endsAt { get; set; }
    public string? generatorURL { get; set; }
}
