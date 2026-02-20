namespace PostProcessor.Core.Models;

public enum PostEventType
{
    Motion,           // Движение инструмента (GOTO, RAPID)
    SpindleChange,    // Смена оборотов/направления шпинделя
    ToolChange,       // Смена инструмента
    CoolantChange,    // Включение/выключение охлаждения
    FeedChange,       // Изменение подачи
    GeometryDefined,  // Определение геометрии (POINT, LINE, CIRCLE)
    Custom            // Пользовательское событие
}

public record PostEvent(
    PostEventType Type,
    APTCommand SourceCommand,
    Dictionary<string, object> Payload
);
