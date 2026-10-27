using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace notatnik;

public partial class MainWindow : Window
{
    private static readonly string ProjectDir =
        Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."));

    private static readonly string NotesFilePath =
        Path.Combine(ProjectDir, "notatki.md");

    private readonly ObservableCollection<Note> _notes = new();

    public MainWindow()
    {
        InitializeComponent();
        NotesList.ItemsSource = _notes;
        LoadNotes();
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        var title = TitleBox.Text?.Trim();
        var content = ContentBox.Text?.Trim();

        if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(content))
        {
            StatusText.Text = "⚠️ Wpisz tytuł lub treść notatki.";
            return;
        }

        var note = new Note
        {
            Title = string.IsNullOrEmpty(title) ? "(Bez tytułu)" : title,
            Content = content ?? string.Empty,
            CreatedAt = DateTime.Now
        };

        _notes.Insert(0, note);
        SaveAllNotes();

        TitleBox.Text = string.Empty;
        ContentBox.Text = string.Empty;
        StatusText.Text = $"✅ Zapisano: \"{note.Title}\"";
    }

    private void OnNoteSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (NotesList.SelectedItem is Note note)
        {
            TitleBox.Text = note.Title;
            ContentBox.Text = note.Content;
            NotesList.SelectedItem = null;
        }
    }

    /// <summary>
    /// Zapisuje wszystkie notatki do pliku notatki.md.
    /// Format wpisu:
    ///   2026-09-08 14:32 — Tytuł notatki
    ///
    ///   treść notatki (może mieć kilka linii)
    ///
    /// Między wpisami pusta linia. Najnowsza notatka na górze.
    /// </summary>
    private void SaveAllNotes()
    {
        var sb = new StringBuilder();

        for (int i = 0; i < _notes.Count; i++)
        {
            var note = _notes[i];

            // Nagłówek: data — tytuł
            sb.AppendLine($"{note.CreatedAt:yyyy-MM-dd HH:mm} — {note.Title}");

            // Treść (oddzielona pustą linią od nagłówka)
            if (!string.IsNullOrEmpty(note.Content))
            {
                sb.AppendLine();
                sb.AppendLine(note.Content);
            }

            // Pusta linia między wpisami (ale nie po ostatnim)
            if (i < _notes.Count - 1)
                sb.AppendLine();
        }

        File.WriteAllText(NotesFilePath, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    /// <summary>
    /// Wczytuje notatki z pliku notatki.md parsując format:
    ///   yyyy-MM-dd HH:mm — Tytuł
    ///   (pusta linia)
    ///   treść...
    /// </summary>
    private void LoadNotes()
    {
        if (!File.Exists(NotesFilePath)) return;

        try
        {
            var text = File.ReadAllText(NotesFilePath, Encoding.UTF8);
            var parsed = ParseNotes(text);

            foreach (var note in parsed)
                _notes.Add(note);
        }
        catch
        {
            // Plik uszkodzony — zaczynamy z pustą listą.
        }
    }

    private static List<Note> ParseNotes(string text)
    {
        var notes = new List<Note>();
        var lines = text.Split('\n');

        int i = 0;
        while (i < lines.Length)
        {
            var line = lines[i].TrimEnd('\r');

            // Szukamy nagłówka: "yyyy-MM-dd HH:mm — Tytuł"
            if (line.Length >= 18 && line.Length > 16 && line[4] == '-' && line[7] == '-'
                && line[10] == ' ' && line[13] == ':')
            {
                // Znajdź separator " — "
                var separatorIndex = line.IndexOf(" — ", StringComparison.Ordinal);
                DateTime date;
                string title;

                if (separatorIndex >= 16 &&
                    DateTime.TryParseExact(line[..16], "yyyy-MM-dd HH:mm",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    title = line[(separatorIndex + 3)..];
                }
                else
                {
                    i++;
                    continue;
                }

                i++;

                // Pomiń jedną pustą linię po nagłówku (jeśli jest)
                if (i < lines.Length && lines[i].TrimEnd('\r').Length == 0)
                    i++;

                // Zbieraj treść aż do kolejnego nagłówka lub końca
                var contentLines = new List<string>();
                while (i < lines.Length)
                {
                    var nextLine = lines[i].TrimEnd('\r');

                    // Sprawdź, czy to nowy nagłówek
                    if (nextLine.Length >= 18 && nextLine[4] == '-' && nextLine[7] == '-'
                        && nextLine[10] == ' ' && nextLine[13] == ':'
                        && nextLine.Contains(" — "))
                    {
                        break;
                    }

                    contentLines.Add(nextLine);
                    i++;
                }

                // Usuń końcowe puste linie z treści
                while (contentLines.Count > 0 && contentLines[^1].Length == 0)
                    contentLines.RemoveAt(contentLines.Count - 1);

                notes.Add(new Note
                {
                    Title = title,
                    Content = string.Join("\n", contentLines),
                    CreatedAt = date
                });
            }
            else
            {
                i++;
            }
        }

        return notes;
    }
}

public class Note
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string DateDisplay => CreatedAt.ToString("dd.MM.yyyy HH:mm");
}
