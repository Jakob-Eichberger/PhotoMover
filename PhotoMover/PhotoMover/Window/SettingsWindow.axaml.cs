using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace PhotoMover;

public partial class SettingsWindow : Window
{
    public SettingsWindow(IServiceProvider serviceProvider)
    {
        Settings = serviceProvider.GetService<Settings>()!;
        DataContext = this;
        InitializeComponent();
    }

    public Settings Settings { get; }
}