using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace notatnik;

public partial class MainWindow : Window
{
    private static readonly string NotesFilePath =
        Path.Combine(GetProjectDir(), "notatki.md");

    private readonly ObservableCollection<Note> _notes = new();

    public MainWindow()
    {
        InitializeComponent();
        NotesList.ItemsSource = _notes;
        LoadNotes();
    }

    /// <summary>
    /// Zwraca folder projektu na podstawie lokalizacji tego pliku źródłowego (compile-time).
    /// </summary>
    private static string GetProjectDir([CallerFilePath] string? sourceFile = null)
        => Path.GetDirectoryName(sourceFile) ?? AppDomain.CurrentDomain.BaseDirectory;

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        var title = TitleBox.Text?.Trim();
        var content = ContentBox.Text?.Trim();

        if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(content))
        {
            StatusText.Text = "\u26a0\ufe0f Wpisz tytu\u0142 lub tre\u015b\u0107 notatki.";
            return;
        }

        var note = new Note
        {
            Title = string.IsNullOrEmpty(title) ? "(Bez tytu\u0142u)" : title,
            Content = content ?? string.Empty,
            CreatedAt = DateTime.Now
        };

        _notes.Insert(0, note);
        SaveAllNotes();

        TitleBox.Text = string.Empty;
        ContentBox.Text = string.Empty;
        StatusText.Text = $"\u2705 Zapisano: \"{note.Title}\"";
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
    /// Zapisuje wszystkie notatki do pliku notatki.md w formacie:
    ///
    ///   2026-09-08 14:32 — Tytuł
    ///
    ///   treść notatki
    ///
    /// Między wpisami jedna pusta linia. Najnowsza na górze. UTF-8.
    /// </summary>
    private void SaveAllNotes()
    {
        var sb = new StringBuilder();

        for (int i = 0; i < _notes.Count; i++)
        {
            var note = _notes[i];

            // Nagłówek: data — tytuł
            sb.Append(note.CreatedAt.ToString("yyyy-MM-dd HH:mm"));
            sb.Append(" \u2014 ");
            sb.AppendLine(note.Title);

            // Pusta linia + treść
            if (!string.IsNullOrEmpty(note.Content))
            {
                sb.AppendLine();
                sb.AppendLine(note.Content);
            }

            // Jedna pusta linia między wpisami (nie po ostatnim)
            if (i < _notes.Count - 1)
                sb.AppendLine();
        }

        File.WriteAllText(NotesFilePath, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    /// <summary>
    /// Wczytuje notatki z pliku notatki.md.
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

    private static bool IsHeaderLine(string line)
    {
        // Nagłówek: "yyyy-MM-dd HH:mm — ..."
        if (line.Length < 19) return false;
        if (line[4] != '-' || line[7] != '-' || line[10] != ' ' || line[13] != ':') return false;

        var sepIdx = line.IndexOf(" \u2014 ", StringComparison.Ordinal);
        if (sepIdx < 16) return false;

        return DateTime.TryParseExact(
            line[..16], "yyyy-MM-dd HH:mm",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    }

    private static List<Note> ParseNotes(string text)
    {
        var notes = new List<Note>();
        var lines = text.Split('\n');

        int i = 0;
        while (i < lines.Length)
        {
            var line = lines[i].TrimEnd('\r');

            if (!IsHeaderLine(line))
            {
                i++;
                continue;
            }

            // Wyciągnij datę i tytuł
            var sepIdx = line.IndexOf(" \u2014 ", StringComparison.Ordinal);
            var date = DateTime.ParseExact(
                line[..16], "yyyy-MM-dd HH:mm",
                CultureInfo.InvariantCulture);
            var title = line[(sepIdx + 3)..];

            i++;

            // Pomiń pustą linię po nagłówku
            if (i < lines.Length && lines[i].TrimEnd('\r').Length == 0)
                i++;

            // Zbieraj treść aż do kolejnego nagłówka lub końca pliku
            var contentLines = new List<string>();
            while (i < lines.Length)
            {
                var nextLine = lines[i].TrimEnd('\r');
                if (IsHeaderLine(nextLine))
                    break;

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
