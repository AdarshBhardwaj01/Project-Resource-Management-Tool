namespace PRM.ConsoleUI.UI.Helpers;

public static class ConsoleHelper
{
    private const int BoxWidth = 46;

    public static void ClearScreen()
    {
        if (Console.IsOutputRedirected)
        {
            return;
        }

        try
        {
            Console.SetCursorPosition(0, 0);
        }
        catch (ArgumentOutOfRangeException)
        {
            // Cursor reset may fail before the buffer is cleared.
        }

        try
        {
            Console.Clear();
        }
        catch (IOException)
        {
            // Erase display + scrollback (3J) then home cursor — works in Windows Terminal and VS Code.
            Console.Write("\x1b[2J\x1b[3J\x1b[H");
        }

        try
        {
            Console.SetCursorPosition(0, 0);
        }
        catch (ArgumentOutOfRangeException)
        {
            // Some terminals reject cursor reset after clear.
        }
    }

    /// <summary>
    /// Call when leaving a full-screen flow so the parent menu redraws on a clean console.
    /// </summary>
    public static void EndScreenSession()
    {
        ClearScreen();
    }

    public static void WriteHeader(string title, string? subtitle = null)
    {
        ClearScreen();
        WriteBoxTop();
        WriteBoxText(title.ToUpperInvariant());

        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            WriteBoxText(subtitle);
        }

        WriteBoxBottom();
        Console.WriteLine();
    }

    public static string ReadInput(string label)
    {
        Console.Write($"{label} : ");
        return Console.ReadLine()?.Trim() ?? string.Empty;
    }

    private const int FormLabelWidth = 18;

    public static string ReadFormField(string label, string? hint = null)
    {
        var prompt = string.IsNullOrWhiteSpace(hint)
            ? $"{label,-FormLabelWidth}: "
            : $"{label,-FormLabelWidth}: ({hint}) ";

        Console.Write(prompt);
        return Console.ReadLine()?.Trim() ?? string.Empty;
    }

    public static string ReadFormChoice(string label, string options)
    {
        Console.WriteLine($"{label,-FormLabelWidth}: {options}");
        Console.Write($"{string.Empty,-FormLabelWidth}  ");
        return Console.ReadLine()?.Trim() ?? string.Empty;
    }

    public static void WritePipeTableHeader(params (string Title, int Width)[] columns)
    {
        Console.WriteLine(FormatPipeRow(columns.Select(column => (column.Title, column.Width)).ToArray()));
    }

    public static void WritePipeTableRow(params (string Value, int Width)[] columns)
    {
        Console.WriteLine(FormatPipeRow(columns));
    }

    public static void WritePipeTableRowPrefix(params (string Value, int Width)[] columns)
    {
        Console.Write(FormatPipeRow(columns));
    }

    public static string FormatPipeTableCells(params (string Value, int Width)[] columns)
    {
        return FormatPipeRow(columns);
    }

    public static int WritePipeTable(
        (string Title, int Width)[] headers,
        IEnumerable<(string Value, int Width)[]> rows)
    {
        var rowList = rows.ToList();
        var headerLine = FormatPipeRow(headers.Select(column => (column.Title, column.Width)).ToArray());
        Console.WriteLine(headerLine);

        var tableWidth = headerLine.Length;
        WriteLineOf('-', tableWidth);

        if (rowList.Count == 0)
        {
            Console.WriteLine("(none)");
        }
        else
        {
            foreach (var row in rowList)
            {
                Console.WriteLine(FormatPipeRow(row));
            }
        }

        WriteLineOf('-', tableWidth);
        return tableWidth;
    }

    public static int GetPipeTableWidth((string Title, int Width)[] columns)
    {
        return FormatPipeRow(columns.Select(column => (column.Title, column.Width)).ToArray()).Length;
    }

    public static void WriteProjectLabel(string projectName, int lineWidth)
    {
        var label = $"--- {projectName} ---";

        if (label.Length >= lineWidth)
        {
            Console.WriteLine(label);
        }
        else
        {
            Console.WriteLine(label + new string('-', lineWidth - label.Length));
        }

        Console.WriteLine();
    }

    public static void WriteActions(params (string Key, string Label)[] actions)
    {
        for (var index = 0; index < actions.Length; index++)
        {
            if (index > 0)
            {
                Console.Write("     ");
            }

            WriteShortcut(actions[index].Key, actions[index].Label);
        }

        Console.WriteLine();
    }

    public static string ReadActionChoice(string prompt = "Enter choice: ")
    {
        Console.Write(prompt);
        return Console.ReadLine()?.Trim().ToUpperInvariant() ?? string.Empty;
    }

    public static void WriteReverseAction(string label, string key)
    {
        Console.Write($"{label} [");
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.Write(key);
        Console.ResetColor();
        Console.WriteLine("]");
    }

    public static string ReadKeyedPrompt(string key, string prompt)
    {
        Console.Write("[");
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.Write(key);
        Console.ResetColor();
        Console.Write($"] {prompt}");
        return Console.ReadLine()?.Trim() ?? string.Empty;
    }

    public static string ReadPassword(string label)
    {
        Console.Write($"{label} : ");
        var password = string.Empty;
        ConsoleKeyInfo key;

        do
        {
            key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password = password[..^1];
                Console.Write("\b \b");
            }
            else if (!char.IsControl(key.KeyChar))
            {
                password += key.KeyChar;
                Console.Write('*');
            }
        }
        while (key.Key != ConsoleKey.Enter);

        Console.WriteLine();
        return password;
    }

    public static void WriteSeparator()
    {
        Console.WriteLine(new string('-', BoxWidth));
    }

    public static void WriteSuccess(string message)
    {
        Console.WriteLine();
        Console.WriteLine(message);
    }

    public static void WriteError(string message)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public static string FormatHealthStatusDisplay(string statusLabel)
    {
        var icon = statusLabel switch
        {
            "AT RISK" => "🔴",
            "ATTENTION" => "🟡",
            _ => "🟢"
        };

        return $"{icon} {statusLabel}";
    }

    public static void WriteHealthStatus(string statusLabel, int? padWidth = null)
    {
        var text = FormatHealthStatusDisplay(statusLabel);
        Console.Write(text);

        if (padWidth.HasValue)
        {
            var padding = padWidth.Value - GetVisualLength(text);

            if (padding > 0)
            {
                Console.Write(new string(' ', padding));
            }
        }
    }

    public static void Pause(string message = "Press any key to continue...")
    {
        Console.WriteLine();
        Console.WriteLine(message);
        Console.ReadKey(true);
    }

    public static void WriteBanner(string label)
    {
        var prefix = $"- {label} ";
        var dashCount = Math.Max(0, BoxWidth - prefix.Length);
        Console.WriteLine(prefix + new string('-', dashCount));
    }

    public static void WriteSectionHeader(string title)
    {
        ClearScreen();
        Console.WriteLine($"--- {title} ---");
    }

    public static void WriteProjectHealthSelectionTable(
        IEnumerable<(int RowNumber, string Name, string HealthStatus)> projects)
    {
        var columns = new (string Title, int Width)[]
        {
            ("#", 3),
            ("Project", 16),
            ("Health", 16)
        };

        WritePipeTableHeader(columns);
        var tableWidth = GetPipeTableWidth(columns);
        WriteLineOf('-', tableWidth);

        foreach (var project in projects)
        {
            Console.Write(FormatPipeTableCells(
                (project.RowNumber.ToString(), columns[0].Width),
                (project.Name, columns[1].Width)));
            Console.Write(" | ");
            WriteHealthStatus(project.HealthStatus, columns[2].Width);
            Console.WriteLine();
        }

        WriteLineOf('-', tableWidth);
    }

    public static void WriteAiRiskSummaryContent(string projectName, string summaryText)
    {
        WriteSectionHeader($"AI Risk Summary - {projectName}");
        Console.WriteLine();
        Console.WriteLine($"\"{summaryText}\"");
        Console.WriteLine();
        Console.WriteLine("Note: This summary is AI-generated from milestone and timesheet data.");
    }

    private static void WriteBoxTop()
    {
        Console.WriteLine($"╔{new string('═', BoxWidth)}╗");
    }

    private static void WriteBoxBottom()
    {
        Console.WriteLine($"╚{new string('═', BoxWidth)}╝");
    }

    private static void WriteBoxText(string text)
    {
        var content = text.Length > BoxWidth ? text[..BoxWidth] : text;
        Console.WriteLine($"║{content.PadRight(BoxWidth)}║");
    }

    private static void WriteShortcut(string key, string label)
    {
        Console.Write("[");
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.Write(key);
        Console.ResetColor();
        Console.Write($"] {label}");
    }

    private static string FormatPipeRow((string Value, int Width)[] columns)
    {
        var parts = new List<string>(columns.Length);

        for (var index = 0; index < columns.Length; index++)
        {
            var (value, width) = columns[index];
            var cell = width <= 0 ? value : Truncate(value, width).PadRight(width);
            parts.Add(index < columns.Length - 1 ? $"{cell} | " : cell);
        }

        return string.Concat(parts);
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static int GetVisualLength(string value)
    {
        var length = 0;

        for (var index = 0; index < value.Length; index++)
        {
            if (char.IsHighSurrogate(value[index]) &&
                index + 1 < value.Length &&
                char.IsLowSurrogate(value[index + 1]))
            {
                length += 2;
                index++;
                continue;
            }

            length++;
        }

        return length;
    }

    private static void WriteLineOf(char character, int width)
    {
        Console.WriteLine(new string(character, width));
    }
}
