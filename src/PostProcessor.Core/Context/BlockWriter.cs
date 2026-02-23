namespace PostProcessor.Core.Context;

/// <summary>
/// Умный формирователь NC-блоков с модальной проверкой
/// Автоматически пропускает неизменённые слова для оптимизации вывода
/// </summary>
public class BlockWriter
{
    private readonly TextWriter _writer;
    private readonly List<NCWord> _words = new();
    private string _separator = " ";
    private int _blockNumber = 0;
    private int _blockIncrement = 10;
    private bool _blockNumberingEnabled = true;

    /// <summary>
    /// Создать BlockWriter для записи в указанный TextWriter
    /// </summary>
    public BlockWriter(TextWriter writer)
    {
        _writer = writer;
    }

    /// <summary>
    /// Разделитель между словами в блоке (по умолчанию " ")
    /// </summary>
    public string Separator
    {
        get => _separator;
        set => _separator = value ?? " ";
    }

    /// <summary>
    /// Включить/отключить нумерацию блоков
    /// </summary>
    public bool BlockNumberingEnabled
    {
        get => _blockNumberingEnabled;
        set => _blockNumberingEnabled = value;
    }

    /// <summary>
    /// Начальный номер блока
    /// </summary>
    public int BlockNumberStart
    {
        get => _blockNumber - _blockIncrement;
        set => _blockNumber = value;
    }

    /// <summary>
    /// Шаг нумерации блоков (по умолчанию 10)
    /// </summary>
    public int BlockIncrement
    {
        get => _blockIncrement;
        set => _blockIncrement = value;
    }

    /// <summary>
    /// Добавить слово в список отслеживаемых
    /// </summary>
    public void AddWord(NCWord word)
    {
        if (!_words.Contains(word))
            _words.Add(word);
    }

    /// <summary>
    /// Добавить несколько слов
    /// </summary>
    public void AddWords(params NCWord[] words)
    {
        foreach (var word in words)
            AddWord(word);
    }

    /// <summary>
    /// Скрыть слова (не выводить до изменения значения)
    /// </summary>
    public void Hide(params NCWord[] words)
    {
        foreach (var w in words)
            w.ForceUnchanged();
    }

    /// <summary>
    /// Показать слова (вывести обязательно)
    /// </summary>
    public void Show(params NCWord[] words)
    {
        foreach (var w in words)
            w.ForceChanged();
    }

    /// <summary>
    /// Сбросить состояние всех слов (для новой операции)
    /// </summary>
    public void ResetAll()
    {
        foreach (var word in _words)
        {
            word.ResetChangeFlag();
        }
    }

    /// <summary>
    /// Сбросить состояние указанных слов
    /// </summary>
    public void Reset(params NCWord[] words)
    {
        foreach (var w in words)
        {
            w.ResetChangeFlag();
        }
    }

    /// <summary>
    /// Сформировать и записать блок, если есть изменения
    /// </summary>
    /// <param name="includeBlockNumber">Включить номер блока</param>
    /// <returns>true если блок был записан, false если нет изменений</returns>
    public bool WriteBlock(bool includeBlockNumber = true)
    {
        var changed = _words.Where(w => w.HasChanged).ToList();

        if (changed.Count == 0)
            return false;

        var parts = new List<string>();

        // Номер блока
        if (_blockNumberingEnabled && includeBlockNumber)
        {
            _blockNumber += _blockIncrement;
            parts.Add($"N{_blockNumber}");
        }

        // Изменённые слова
        foreach (var word in changed)
        {
            var wordStr = word.ToNCString();
            if (!string.IsNullOrEmpty(wordStr))
                parts.Add(wordStr);
            
            // Сброс флага после вывода (для модальных слов)
            if (word.IsModal)
                word.ResetChangeFlag();
        }

        if (parts.Count > 0)
        {
            _writer.WriteLine(string.Join(_separator, parts));
            return true;
        }

        return false;
    }

    /// <summary>
    /// Записать только номер блока (для пустых блоков)
    /// </summary>
    public void WriteBlockNumberOnly()
    {
        if (_blockNumberingEnabled)
        {
            _blockNumber += _blockIncrement;
            _writer.WriteLine($"N{_blockNumber}");
        }
    }

    /// <summary>
    /// Получить текущий номер блока (следующий будет выведен)
    /// </summary>
    public int CurrentBlockNumber => _blockNumber;

    /// <summary>
    /// Записать строку напрямую (для комментариев, M-кодов вне блоков)
    /// </summary>
    public void WriteLine(string line)
    {
        _writer.WriteLine(line);
    }

    /// <summary>
    /// Записать комментарий в формате станка
    /// </summary>
    public void WriteComment(string comment)
    {
        _writer.WriteLine($"({comment})");
    }

    /// <summary>
    /// Получить список всех отслеживаемых слов
    /// </summary>
    public IReadOnlyList<NCWord> Words => _words.AsReadOnly();

    /// <summary>
    /// Получить список изменённых слов
    /// </summary>
    public IReadOnlyList<NCWord> ChangedWords => _words.Where(w => w.HasChanged).ToList();

    /// <summary>
    /// Получить список неизменённых слов
    /// </summary>
    public IReadOnlyList<NCWord> UnchangedWords => _words.Where(w => !w.HasChanged).ToList();
}
