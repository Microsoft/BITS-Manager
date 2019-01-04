﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BITSManager
{
    /// <summary>
    /// Interaction logic for AddFiletoJobWindow.xaml
    /// </summary>
    public partial class AddFileToJobWindow : Window
    {
        public string RemoteUrl { get { return _uiUrl.Text; } }
        public string LocalFile { get { return _uiFile.Text; } }
        private bool _fileHasChanged = false;

        public AddFileToJobWindow()
        {
            InitializeComponent();
            Loaded += AddFileToJobWindowControl_Loaded;
        }

        private void AddFileToJobWindowControl_Loaded(object sender, RoutedEventArgs e)
        {
            _uiUrl.Focus();
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void OnOK(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void OnUrlChanged(object sender, TextChangedEventArgs e)
        {
            string newUrlText = _uiUrl.Text;
            Uri uri;
            var parseSucceeded = Uri.TryCreate(newUrlText, UriKind.Absolute, out uri);
            if (parseSucceeded && uri.Segments.Length >= 1 && !_fileHasChanged)
            {
                // Make a corresponding file name. If the user has changed the file text,
                // don't update it when the URL changes.
                var file = uri.Segments[uri.Segments.Length - 1];
                if (file != "/")
                {
                    var dir = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    var fullpath = System.IO.Path.Combine(dir, file);
                    _uiFile.Text = fullpath;
                }
            }
        }

        private void OnFileChangedViaKeyboard(object sender, KeyEventArgs e)
        {
            _fileHasChanged = true;
        }
    }
}
