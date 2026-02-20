namespace PostProcessor.Core.Context;

public class RegisterSet
{
    private readonly Dictionary<string, Register> _registers = new();

    // Стандартные регистры для фрезерного станка
    public Register X => GetOrAdd("X", 0.0, true, "F4.3");
    public Register Y => GetOrAdd("Y", 0.0, true, "F4.3");
    public Register Z => GetOrAdd("Z", 0.0, true, "F4.3");
    public Register A => GetOrAdd("A", 0.0, true, "F4.3"); // 4-я ось
    public Register B => GetOrAdd("B", 0.0, true, "F4.3"); // 5-я ось
    public Register C => GetOrAdd("C", 0.0, true, "F4.3"); // 6-я ось
    public Register F => GetOrAdd("F", 0.0, false, "F3.1"); // Подача
    public Register S => GetOrAdd("S", 0.0, false, "F0");   // Обороты
    public Register T => GetOrAdd("T", 0.0, false, "F0");   // Номер инструмента

    public Register GetOrAdd(string name, double initialValue = 0.0, bool isModal = true, string format = "F4.3")
    {
        if (!_registers.TryGetValue(name, out var reg))
        {
            reg = new Register(name, initialValue, isModal, format);
            _registers[name] = reg;
        }
        return reg;
    }

    public IEnumerable<Register> ChangedRegisters()
    {
        foreach (var reg in _registers.Values)
        {
            if (reg.HasChanged || !reg.IsModal)
                yield return reg;
        }
    }

    public void ResetChangeFlags()
    {
        foreach (var reg in _registers.Values)
            reg.ResetChangeFlag();
    }

    public override string ToString()
    {
        return string.Join(" ", _registers.Values.Select(r => r.ToString()));
    }
}
