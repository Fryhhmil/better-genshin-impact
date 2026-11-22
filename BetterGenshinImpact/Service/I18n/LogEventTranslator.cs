using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Serilog.Events;
using Serilog.Parsing;

namespace BetterGenshinImpact.Service.I18n;

/// <summary>
/// 把日志事件翻译成界面语言，只给日志遮罩使用；日志文件、外部日志等其他输出保持中文原文。
/// 以中文消息模板为 Key 查日志翻译表，属性值保持不变，再把属性中能在界面翻译表找到的中文值（角色名、地图名等）翻译。
/// 找不到翻译、或译文的占位符与原文不一致时显示原文。
/// </summary>
public sealed partial class LogEventTranslator
{
    /// <summary>
    /// 当前语言是否需要翻译日志。
    /// </summary>
    private readonly Func<bool> _isEnabled;

    /// <summary>
    /// 翻译日志模板，没有译文时返回 null。
    /// </summary>
    private readonly Func<string, string?> _translateTemplate;

    /// <summary>
    /// 用界面翻译表翻译属性中的文本，没有译文时原样返回。
    /// </summary>
    private readonly Func<string, string> _translateText;

    /// <summary>
    /// 语言版本号，变化时清空模板缓存。
    /// </summary>
    private readonly Func<long> _revision;

    /// <summary>
    /// 解析译文模板，和 Serilog 记录日志时使用的解析器一致。
    /// </summary>
    private readonly MessageTemplateParser _parser = new();

    /// <summary>
    /// 当前语言下已解析的译文模板，Key 为中文模板，值为 null 表示没有可用的译文。语言变化时整体替换。
    /// </summary>
    private volatile TemplateCache _cache = new(-1, new ConcurrentDictionary<string, MessageTemplate?>(StringComparer.Ordinal));

    /// <summary>
    /// 使用多语言服务的当前语言创建日志翻译器。
    /// </summary>
    /// <param name="i18n">多语言服务。</param>
    public LogEventTranslator(I18nService i18n)
        : this(() => i18n.TranslatesLogs, i18n.TranslateLogTemplate, i18n.Translate, () => i18n.Revision)
    {
    }

    /// <summary>
    /// 使用指定的翻译来源创建日志翻译器（便于测试，不依赖全局语言状态）。
    /// </summary>
    /// <param name="isEnabled">当前语言是否需要翻译日志。</param>
    /// <param name="translateTemplate">翻译日志模板，没有译文时返回 null。</param>
    /// <param name="translateText">翻译属性文本，没有译文时原样返回。</param>
    /// <param name="revision">语言版本号。</param>
    public LogEventTranslator(Func<bool> isEnabled, Func<string, string?> translateTemplate, Func<string, string> translateText, Func<long> revision)
    {
        _isEnabled = isEnabled;
        _translateTemplate = translateTemplate;
        _translateText = translateText;
        _revision = revision;
    }

    /// <summary>
    /// 中文列表常用的分隔符，用于逐项翻译“那维莱特,枫原万叶”这样拼接起来的属性值。
    /// </summary>
    [GeneratedRegex(@"(\s*[,，、/]\s*)", RegexOptions.CultureInvariant)]
    private static partial Regex ListSeparatorRegex();

    /// <summary>
    /// 翻译日志事件。没有任何可翻译的内容时返回原事件。
    /// </summary>
    /// <param name="logEvent">原日志事件。</param>
    public LogEvent Translate(LogEvent logEvent)
    {
        if (!_isEnabled())
        {
            return logEvent;
        }

        // 1. 模板：查缓存，未命中时翻译并校验占位符
        var template = GetTranslatedTemplate(logEvent.MessageTemplate) ?? logEvent.MessageTemplate;

        // 2. 属性：只翻译模板中出现的属性，其余（SourceContext 等）原样保留
        var names = template.Tokens.OfType<PropertyToken>().Select(token => token.PropertyName).ToHashSet(StringComparer.Ordinal);
        var properties = new List<LogEventProperty>(logEvent.Properties.Count);
        var propertyChanged = false;
        foreach (var (name, value) in logEvent.Properties)
        {
            var translated = names.Contains(name) ? TranslateValue(value) : value;
            propertyChanged |= !ReferenceEquals(translated, value);
            properties.Add(new LogEventProperty(name, translated));
        }

        if (ReferenceEquals(template, logEvent.MessageTemplate) && !propertyChanged)
        {
            return logEvent;
        }

        return new LogEvent(logEvent.Timestamp, logEvent.Level, logEvent.Exception, template, properties);
    }

    /// <summary>
    /// 取得模板的译文；没有译文或译文无效时返回 null。
    /// </summary>
    private MessageTemplate? GetTranslatedTemplate(MessageTemplate original)
    {
        var revision = _revision();
        var cache = _cache;
        if (cache.Revision != revision)
        {
            cache = new TemplateCache(revision, new ConcurrentDictionary<string, MessageTemplate?>(StringComparer.Ordinal));
            _cache = cache;
        }

        return cache.Templates.GetOrAdd(original.Text, _ => ParseTranslation(original));
    }

    /// <summary>
    /// 翻译并解析模板。译文的占位符名称必须与原文完全一致，否则属性会对不上，此时放弃译文。
    /// </summary>
    private MessageTemplate? ParseTranslation(MessageTemplate original)
    {
        var text = _translateTemplate(original.Text);
        if (text is null || string.Equals(text, original.Text, StringComparison.Ordinal))
        {
            return null;
        }

        var translated = _parser.Parse(text);
        return HaveSamePlaceholders(original, translated) ? translated : null;
    }

    /// <summary>
    /// 两个模板的占位符名称集合是否一致。
    /// </summary>
    public static bool HaveSamePlaceholders(MessageTemplate original, MessageTemplate translated)
    {
        var originalNames = original.Tokens.OfType<PropertyToken>().Select(token => token.PropertyName).ToHashSet(StringComparer.Ordinal);
        var translatedNames = translated.Tokens.OfType<PropertyToken>().Select(token => token.PropertyName).ToHashSet(StringComparer.Ordinal);
        return originalNames.SetEquals(translatedNames);
    }

    /// <summary>
    /// 翻译属性值：字符串整体或按分隔符逐项在界面翻译表中查找；数组逐个元素处理。未变化时返回原对象。
    /// </summary>
    private LogEventPropertyValue TranslateValue(LogEventPropertyValue value)
    {
        switch (value)
        {
            case ScalarValue { Value: string text }:
                var translated = TranslateText(text);
                return ReferenceEquals(translated, text) ? value : new ScalarValue(translated);
            case SequenceValue sequence:
                var elements = sequence.Elements.Select(TranslateValue).ToArray();
                return elements.Where((element, index) => !ReferenceEquals(element, sequence.Elements[index])).Any()
                    ? new SequenceValue(elements)
                    : value;
            default:
                return value;
        }
    }

    /// <summary>
    /// 翻译属性中的中文文本，找不到译文时返回原字符串对象。
    /// </summary>
    private string TranslateText(string text)
    {
        if (!ContainsChinese(text))
        {
            return text;
        }

        // 1. 整体翻译：先查界面翻译表（角色名、地图名等），再查日志翻译表（如 "→ {Text}" 传入的固定中文文案）
        var whole = _translateText(text);
        if (!string.Equals(whole, text, StringComparison.Ordinal))
        {
            return whole;
        }

        if (_translateTemplate(text) is { Length: > 0 } logText && !string.Equals(logText, text, StringComparison.Ordinal))
        {
            return logText;
        }

        // 2. 逐项翻译（保留原分隔符）
        var parts = ListSeparatorRegex().Split(text);
        if (parts.Length == 1)
        {
            return text;
        }

        var changed = false;
        for (var index = 0; index < parts.Length; index += 2)
        {
            var part = _translateText(parts[index]);
            changed |= !string.Equals(part, parts[index], StringComparison.Ordinal);
            parts[index] = part;
        }

        return changed ? string.Concat(parts) : text;
    }

    /// <summary>
    /// 文本是否包含中文字符；不含中文的值（数字、英文、游戏内识别出的外文）无需查表。
    /// </summary>
    private static bool ContainsChinese(string text)
    {
        foreach (var c in text)
        {
            if (c is >= '一' and <= '鿿')
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 某个语言版本下的译文模板缓存。
    /// </summary>
    /// <param name="Revision">对应的 <see cref="I18nService.Revision"/>。</param>
    /// <param name="Templates">中文模板到译文模板的映射。</param>
    private sealed record TemplateCache(long Revision, ConcurrentDictionary<string, MessageTemplate?> Templates);
}
