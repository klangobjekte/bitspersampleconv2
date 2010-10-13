﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PlayPcmWin {
    /// <summary>
    /// SettingsWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingsWindow : Window {
        public SettingsWindow() {
            InitializeComponent();
        }

        Preference m_preference = null;
        public void SetPreference(Preference preference) {
            m_preference = preference;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            System.Diagnostics.Debug.Assert(null != m_preference);

            switch (m_preference.bitsPerSampleFixType) {
            case BitsPerSampleFixType.Variable:
                radioButtonBpsVariable.IsChecked = true;
                break;
            case BitsPerSampleFixType.Sint16:
                radioButtonBpsSint16.IsChecked = true;
                break;
            case BitsPerSampleFixType.Sint32:
                radioButtonBpsSint32.IsChecked = true;
                break;
            case BitsPerSampleFixType.Sfloat32:
                radioButtonBpsSfloat32.IsChecked = true;
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                break;
            }

            checkBoxReplaceGapWithKokomade.IsChecked =
                m_preference.ReplaceGapWithKokomade;

            checkBoxManuallySetMainWindowDimension.IsChecked =
                m_preference.ManuallySetMainWindowDimension;

            checkBoxParallelRead.IsChecked =
                m_preference.ParallelRead;
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e) {
            if (true == radioButtonBpsVariable.IsChecked) {
                m_preference.bitsPerSampleFixType = BitsPerSampleFixType.Variable;
            }
            if (true == radioButtonBpsSint16.IsChecked) {
                m_preference.bitsPerSampleFixType = BitsPerSampleFixType.Sint16;
            }
            if (true == radioButtonBpsSint32.IsChecked) {
                m_preference.bitsPerSampleFixType = BitsPerSampleFixType.Sint32;
            }
            if (true == radioButtonBpsSfloat32.IsChecked) {
                m_preference.bitsPerSampleFixType = BitsPerSampleFixType.Sfloat32;
            }

            m_preference.ReplaceGapWithKokomade
                = checkBoxReplaceGapWithKokomade.IsChecked == true;

            m_preference.ManuallySetMainWindowDimension
                = checkBoxManuallySetMainWindowDimension.IsChecked == true;

            m_preference.ParallelRead
                = checkBoxParallelRead.IsChecked == true;

            Close();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e) {
            Close();
        }

    }
}
