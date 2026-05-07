using EventService.Mcp.Prompts;
using Microsoft.Extensions.AI;

namespace EventService.Mcp.Tests.Unit;

public class PromptsTests
{
    [Fact]
    public void DraftSourcingEvent_emits_system_then_user_message_with_brief()
    {
        var msgs = EventPrompts.DraftSourcingEvent("buy 50 laptops").ToArray();
        Assert.Equal(2, msgs.Length);
        Assert.Equal(ChatRole.System, msgs[0].Role);
        Assert.Equal(ChatRole.User, msgs[1].Role);
        Assert.Contains("buy 50 laptops", msgs[1].Text);
    }

    [Fact]
    public void PrePublishReadinessCheck_includes_event_id_in_user_message()
    {
        var msgs = EventPrompts.PrePublishReadinessCheck("evt-1").ToArray();
        Assert.Contains("evt-1", msgs[1].Text);
        Assert.Contains("reference://event-status", msgs[0].Text);
    }

    [Fact]
    public void SupplierShortlist_substitutes_category_and_criteria()
    {
        var msgs = EventPrompts.SupplierShortlist("IT", "GST-registered").ToArray();
        Assert.Contains("IT", msgs[1].Text);
        Assert.Contains("GST-registered", msgs[1].Text);
    }

    [Fact]
    public void EventStatusSummary_defaults_to_executive_audience()
    {
        var msgs = EventPrompts.EventStatusSummary("evt-2").ToArray();
        Assert.Contains("executive", msgs[1].Text);
    }
}
