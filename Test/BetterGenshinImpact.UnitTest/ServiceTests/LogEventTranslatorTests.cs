using System.IO;
using BetterGenshinImpact.Service.I18n;
using Serilog.Events;
using Serilog.Parsing;

namespace BetterGenshinImpact.UnitTest.ServiceTests;

/// <summary>
/// 日志遮罩翻译：模板按中文 Key 翻译，属性值保留，占位符不一致时放弃译文，属性中的中文值按界面翻译表翻译。
/// </summary>
public sealed class LogEventTranslatorTests
{
    /// <summary>
    /// 测试用日志模板翻译表。
    /// </summary>
    private static readonly Dictionary<string, string> Templates = new()
    {
        ["→ 开始执行地图追踪任务: {Name}"] = "→ Iniciando rota: {Name}",
        ["识别到的队伍角色:{Text}"] = "Personagens da equipe: {Text}",
        ["占位符被改名:{Name}"] = "Placeholder renomeado: {Nome}",
    };

    /// <summary>
    /// 测试用界面翻译表。
    /// </summary>
    private static readonly Dictionary<string, string> Texts = new()
    {
        ["钟离"] = "Zhongli",
        ["温迪"] = "Venti",
        ["提瓦特大陆"] = "Teyvat",
    };

    [Fact]
    public void TranslatesTemplateAndKeepsProperties()
    {
        var text = Render(CreateTranslator().Translate(CreateEvent("→ 开始执行地图追踪任务: {Name}", ("Name", "01-路线.json"))));
        Assert.Equal("→ Iniciando rota: 01-路线.json", text);
    }

    [Fact]
    public void TranslatesChineseListValues()
    {
        var text = Render(CreateTranslator().Translate(CreateEvent("识别到的队伍角色:{Text}", ("Text", "钟离,温迪，那维莱特"))));
        // 能翻译的逐项翻译，未知名称保持原文，分隔符保留
        Assert.Equal("Personagens da equipe: Zhongli,Venti，那维莱特", text);
    }

    [Fact]
    public void TranslatesLiteralArgumentWithLogTable()
    {
        // "→ {Text}" 模板本身不需要翻译，传入的固定中文文案查日志翻译表
        var translator = new LogEventTranslator(() => true,
            x => x == "自动钓鱼，启动！" ? "Pesca automática iniciada!" : null,
            x => x, () => 0);
        var text = Render(translator.Translate(CreateEvent("→ {Text}", ("Text", "自动钓鱼，启动！"))));
        Assert.Equal("→ Pesca automática iniciada!", text);
    }

    [Fact]
    public void FallsBackWhenPlaceholdersDiffer()
    {
        var text = Render(CreateTranslator().Translate(CreateEvent("占位符被改名:{Name}", ("Name", "提瓦特大陆"))));
        // 模板保持原文，属性值仍会翻译
        Assert.Equal("占位符被改名:Teyvat", text);
    }

    [Fact]
    public void ReturnsSameEventWhenNothingToTranslate()
    {
        var logEvent = CreateEvent("没有翻译的模板 {Count}", ("Count", 3));
        Assert.Same(logEvent, CreateTranslator().Translate(logEvent));
    }

    [Fact]
    public void DisabledForSimplifiedChinese()
    {
        var logEvent = CreateEvent("→ 开始执行地图追踪任务: {Name}", ("Name", "钟离"));
        var translator = new LogEventTranslator(() => false, Templates.GetValueOrDefault, x => Texts.GetValueOrDefault(x, x), () => 0);
        Assert.Same(logEvent, translator.Translate(logEvent));
    }

    [Fact]
    public void ReloadsTemplatesWhenRevisionChanges()
    {
        var revision = 0L;
        var current = new Dictionary<string, string> { ["开始 {Name}"] = "Início {Name}" };
        var translator = new LogEventTranslator(() => true, current.GetValueOrDefault, x => x, () => revision);
        Assert.Equal("Início a", Render(translator.Translate(CreateEvent("开始 {Name}", ("Name", "a")))));

        // 切换语言：版本号变化后使用新译文
        current["开始 {Name}"] = "Start {Name}";
        revision++;
        Assert.Equal("Start a", Render(translator.Translate(CreateEvent("开始 {Name}", ("Name", "a")))));
    }

    /// <summary>
    /// 使用测试翻译表、始终启用的翻译器。
    /// </summary>
    private static LogEventTranslator CreateTranslator()
        => new(() => true, Templates.GetValueOrDefault, x => Texts.GetValueOrDefault(x, x), () => 0);

    /// <summary>
    /// 按模板和属性创建日志事件，与 Serilog 记录时的解析方式一致。
    /// </summary>
    private static LogEvent CreateEvent(string template, params (string Name, object Value)[] properties)
        => new(DateTimeOffset.Now, LogEventLevel.Information, null, new MessageTemplateParser().Parse(template),
            properties.Select(p => new LogEventProperty(p.Name, new ScalarValue(p.Value))));

    /// <summary>
    /// 以遮罩使用的 :lj 方式渲染消息（字符串不加引号）。
    /// </summary>
    private static string Render(LogEvent logEvent)
    {
        using var writer = new StringWriter();
        logEvent.MessageTemplate.Render(logEvent.Properties, writer);
        return writer.ToString().Replace("\"", string.Empty);
    }
}
