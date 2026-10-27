using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace notatnik;

public partial class MainWindow : Window
{
    private static readonly string NotesFilePath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "notatki.json");

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
        SaveNotes();

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

    private void SaveNotes()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(_notes.ToList(), options);
        File.WriteAllText(NotesFilePath, json);
    }

    private void LoadNotes()
    {
        if (!File.Exists(NotesFilePath)) return;

        try
        {
            var json = File.ReadAllText(NotesFilePath);
            var notes = JsonSerializer.Deserialize<List<Note>>(json);
            if (notes is null) return;

            foreach (var note in notes)
                _notes.Add(note);
        }
        catch
        {
            // Plik uszkodzony — zaczynamy z pustą listą.
        }
    }
}

public class Note
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string DateDisplay => CreatedAt.ToString("dd.MM.yyyy HH:mm");
}
