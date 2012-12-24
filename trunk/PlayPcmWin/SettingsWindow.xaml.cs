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
using WasapiPcmUtil;
using System.Globalization;

namespace PlayPcmWin {
    /// <summary>
    /// SettingsWindow.xaml の相互作用ロジック
    /// </summary>
    public sealed partial class SettingsWindow : Window {
        enum SettingsBitFormatType {
            Variable,
            VariableSint16Sint32V24,
            VariableSint16Sint24,
            Sint16,
            Sint24,

            Sint32V24,
            Sint32,
            Sfloat32,
            AutoSelect,
        };

        public SettingsWindow() {
            InitializeComponent();

            Title = Properties.Resources.SettingsWindowTitle;
            
            groupBoxCuesheetSettings.Header = Properties.Resources.SettingsGroupBoxCuesheetSettings;
            groupBoxDeviceBufferFlush.Header = Properties.Resources.SettingsGroupBoxDeviceBufferFlush;
            groupBoxListDisplay.Header = Properties.Resources.SettingsGroupBoxListDisplay;
            groupBoxOtherSettings.Header = Properties.Resources.SettingsGroupBoxOtherSettings;
            groupBoxWasapiExclusive.Header = Properties.Resources.SettingsGroupBoxWasapiExclusive;
            labelQuantizationBitrate.Content = Properties.Resources.SettingsLabelQuantizationBitrate;
            checkBoxNoiseShaping.Content = Properties.Resources.SettingsCheckBoxPerformNoiseShaping;

            groupBoxRenderThreadTaskType.Header = Properties.Resources.SettingsGroupBoxRenderThreadTaskType;
            groupBoxWasapiShared.Header = Properties.Resources.SettingsGroupBoxWasapiShared;

            comboBoxOutputFormat.Items.Add(Properties.Resources.SettingsRadioButtonBpsVariable);
            comboBoxOutputFormat.Items.Add(Properties.Resources.SettingsRadioButtonBpsVariableSint16Sint32V24);
            comboBoxOutputFormat.Items.Add(Properties.Resources.SettingsRadioButtonBpsVariableSint16Sint24);
            comboBoxOutputFormat.Items.Add(Properties.Resources.SettingsRadioButtonBpsSint16);
            comboBoxOutputFormat.Items.Add(Properties.Resources.SettingsRadioButtonBpsSint24);

            comboBoxOutputFormat.Items.Add(Properties.Resources.SettingsRadioButtonBpsSint32V24);
            comboBoxOutputFormat.Items.Add(Properties.Resources.SettingsRadioButtonBpsSint32);
            comboBoxOutputFormat.Items.Add(Properties.Resources.SettingsRadioButtonBpsSfloat32);
            comboBoxOutputFormat.Items.Add(Properties.Resources.SettingsRadioButtonBpsAutoSelect);

            radioButtonTaskAudio.Content = Properties.Resources.SettingsRadioButtonTaskAudio;

            radioButtonTaskNone.Content = Properties.Resources.SettingsRadioButtonTaskNone;
            radioButtonTaskPlayback.Content = Properties.Resources.SettingsRadioButtonTaskPlayback;
            radioButtonTaskProAudio.Content = Properties.Resources.SettingsRadioButtonTaskProAudio;

            checkBoxAlternateBackground.Content = Properties.Resources.SettingsCheckBoxAlternateBackground;

            checkBoxSootheLimiterApo.Content = Properties.Resources.SettingsSootheLimiterApo;

            checkBoxCoverart.Content = Properties.Resources.SettingsCheckBoxCoverart;
            checkBoxManuallySetMainWindowDimension.Content = Properties.Resources.SettingsCheckBoxManuallySetMainWindowDimension;
            checkBoxParallelRead.Content = Properties.Resources.SettingsCheckBoxParallelRead;
            checkBoxPlaceKokomadeAfterIndex00.Content = Properties.Resources.SettingsCheckBoxPlaceKokomadeAterIndex00;

            checkBoxPlayingTimeBold.Content = Properties.Resources.SettingsCheckBoxPlayingTimeBold;
            checkBoxRefrainRedraw.Content = Properties.Resources.SettingsCheckBoxRefrainRedraw;
            checkBoxStorePlaylistContent.Content = Properties.Resources.SettingsCheckBoxStorePlaylistContent;
            cbItemTimerResolutionDefault.Content = Properties.Resources.SettingsTimerResolutionDefault;
            cbItemTimerResolution1Millisec.Content = Properties.Resources.SettingsTimerResolution1Millisec;
            cbItemTimerResolution500Microsec.Content = Properties.Resources.SettingsTimerResolution500Microsec;

            labelConversionQuality.Content = Properties.Resources.SettingsLabelConversionQuality;
            labelCueEncoding.Content = Properties.Resources.SettingsCueEncoding;
            labelFontPoints.Content = Properties.Resources.SettingsLabelFontPoints;
            labelPlayingTimeFont.Content = Properties.Resources.SettingsLabelPlayingTimeFont;
            labelZeroFlushSeconds.Content = Properties.Resources.SettingsLabelZeroFlushSeconds;
            labelZeroFlushUnit.Content = Properties.Resources.SettingsLabelZeroFlushUnit;

            buttonCancel.Content = Properties.Resources.SettingsButtonCancel;
            buttonChangeColor.Content = Properties.Resources.SettingsButtonChangeColor;
            buttonOK.Content = Properties.Resources.SettingsButtonOK;
            buttonReset.Content = Properties.Resources.SettingsButtonReset;
        }

        Preference m_preference = null;
        public void SetPreference(Preference preference) {
            m_preference = preference;
        }

        private long mPlaylistAlternateBackgroundArgb;

        private void UpdateUIFromPreference(Preference preference) {
            switch (preference.BitsPerSampleFixType) {
            case BitsPerSampleFixType.Variable:
                comboBoxOutputFormat.SelectedIndex = (int)SettingsBitFormatType.Variable;
                break;
            case BitsPerSampleFixType.VariableSint16Sint24:
                comboBoxOutputFormat.SelectedIndex = (int)SettingsBitFormatType.VariableSint16Sint24;
                break;
            case BitsPerSampleFixType.VariableSint16Sint32V24:
                comboBoxOutputFormat.SelectedIndex = (int)SettingsBitFormatType.VariableSint16Sint32V24;
                break;
            case BitsPerSampleFixType.Sint16:
                comboBoxOutputFormat.SelectedIndex = (int)SettingsBitFormatType.Sint16;
                break;
            case BitsPerSampleFixType.Sint24:
                comboBoxOutputFormat.SelectedIndex = (int)SettingsBitFormatType.Sint24;
                break;
            case BitsPerSampleFixType.Sint32:
                comboBoxOutputFormat.SelectedIndex = (int)SettingsBitFormatType.Sint32;
                break;
            case BitsPerSampleFixType.Sfloat32:
                comboBoxOutputFormat.SelectedIndex = (int)SettingsBitFormatType.Sfloat32;
                break;
            case BitsPerSampleFixType.Sint32V24:
                comboBoxOutputFormat.SelectedIndex = (int)SettingsBitFormatType.Sint32V24;
                break;
            case BitsPerSampleFixType.AutoSelect:
                comboBoxOutputFormat.SelectedIndex = (int)SettingsBitFormatType.AutoSelect;
                break;
            }
            checkBoxNoiseShaping.IsChecked =
                preference.EnableNoiseShaping;

            checkBoxPlaceKokomadeAfterIndex00.IsChecked =
                preference.ReplaceGapWithKokomade;

            checkBoxManuallySetMainWindowDimension.IsChecked =
                preference.ManuallySetMainWindowDimension;

            checkBoxStorePlaylistContent.IsChecked =
                preference.StorePlaylistContent;

            checkBoxCoverart.IsChecked =
                preference.DispCoverart;

            checkBoxRefrainRedraw.IsChecked =
                preference.RefrainRedraw;

            checkBoxParallelRead.IsChecked =
                preference.ParallelRead;

            if (5000 == preference.TimePeriodHundredNanosec) {
                comboBoxTimePeriod.SelectedItem = cbItemTimerResolution500Microsec;
            } else if (10000 == preference.TimePeriodHundredNanosec) {
                comboBoxTimePeriod.SelectedItem = cbItemTimerResolution1Millisec;
            } else {
                comboBoxTimePeriod.SelectedItem = cbItemTimerResolutionDefault;
            }

            textBoxPlayingTimeSize.Text =
                preference.PlayingTimeSize.ToString(CultureInfo.CurrentCulture);

            textBoxZeroFlushSeconds.Text =
                string.Format(CultureInfo.CurrentCulture, "{0}", preference.ZeroFlushMillisec * 0.001);

            textBoxConversionQuality.Text =
                string.Format(CultureInfo.CurrentCulture, "{0}", preference.ResamplerConversionQuality);

            checkBoxSootheLimiterApo.IsChecked = preference.SootheLimiterApo;

            sliderWindowScaling.Value =
                preference.WindowScale;

            checkBoxPlayingTimeBold.IsChecked =
                preference.PlayingTimeFontBold;

            var fontFamilies = new Dictionary<string, FontFamily>();

            foreach (FontFamily fontFamily in Fonts.SystemFontFamilies) {
                if (!fontFamilies.ContainsKey(fontFamily.ToString())) {
                    fontFamilies.Add(fontFamily.ToString(), fontFamily);
                }
            }

            foreach (var kvp in fontFamilies) {
                var item = new ComboBoxItem();
                item.Content = kvp.Value;
                //item.FontFamily = fontFamily;
                comboBoxPlayingTimeFontNames.Items.Add(item);
                if (kvp.Key.Equals(preference.PlayingTimeFontName)) {
                    comboBoxPlayingTimeFontNames.SelectedItem = item;
                }
            }

            {
                mPlaylistAlternateBackgroundArgb = preference.AlternatingRowBackgroundArgb;
                rectangleColor.Fill = new SolidColorBrush(Util.ColorFromArgb(
                        preference.AlternatingRowBackgroundArgb));
                checkBoxAlternateBackground.IsChecked =
                        preference.AlternatingRowBackground;
                if (preference.AlternatingRowBackground) {
                    rectangleColor.IsEnabled = true;
                    buttonChangeColor.IsEnabled = true;
                } else {
                    rectangleColor.IsEnabled = false;
                    buttonChangeColor.IsEnabled = false;
                }
            }

            switch (preference.RenderThreadTaskType) {
            case RenderThreadTaskType.Audio:
                radioButtonTaskAudio.IsChecked = true;
                break;
            case RenderThreadTaskType.None:
                radioButtonTaskNone.IsChecked = true;
                break;
            case RenderThreadTaskType.Playback:
                radioButtonTaskPlayback.IsChecked = true;
                break;
            case RenderThreadTaskType.ProAudio:
                radioButtonTaskProAudio.IsChecked = true;
                break;
            }

            comboBoxCueEncoding.Items.Clear();
            foreach (var encoding in Encoding.GetEncodings()) {
                int pos = comboBoxCueEncoding.Items.Add(encoding.DisplayName);
                if (preference.CueEncodingCodePage == encoding.CodePage) {
                    comboBoxCueEncoding.SelectedIndex = pos;
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            System.Diagnostics.Debug.Assert(null != m_preference);
            UpdateUIFromPreference(m_preference);
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e) {
            System.Diagnostics.Debug.Assert(0 <= comboBoxOutputFormat.SelectedIndex);
            var settingsBitFormat = (SettingsBitFormatType)comboBoxOutputFormat.SelectedIndex;
            switch (settingsBitFormat) {
            case SettingsBitFormatType.Variable:
                m_preference.BitsPerSampleFixType = BitsPerSampleFixType.Variable;
                break;
            case SettingsBitFormatType.VariableSint16Sint24:
                m_preference.BitsPerSampleFixType = BitsPerSampleFixType.VariableSint16Sint24;
                break;
            case SettingsBitFormatType.VariableSint16Sint32V24:
                m_preference.BitsPerSampleFixType = BitsPerSampleFixType.VariableSint16Sint32V24;
                break;
            case SettingsBitFormatType.Sint16:
                m_preference.BitsPerSampleFixType = BitsPerSampleFixType.Sint16;
                break;
            case SettingsBitFormatType.Sint24:
                m_preference.BitsPerSampleFixType = BitsPerSampleFixType.Sint24;
                break;
            case SettingsBitFormatType.Sint32:
                m_preference.BitsPerSampleFixType = BitsPerSampleFixType.Sint32;
                break;
            case SettingsBitFormatType.Sfloat32:
                m_preference.BitsPerSampleFixType = BitsPerSampleFixType.Sfloat32;
                break;
            case SettingsBitFormatType.Sint32V24:
                m_preference.BitsPerSampleFixType = BitsPerSampleFixType.Sint32V24;
                break;
            case SettingsBitFormatType.AutoSelect:
            default:
                m_preference.BitsPerSampleFixType = BitsPerSampleFixType.AutoSelect;
                break;
            }
            m_preference.EnableNoiseShaping
                = checkBoxNoiseShaping.IsChecked == true;

            m_preference.ReplaceGapWithKokomade
                = checkBoxPlaceKokomadeAfterIndex00.IsChecked == true;

            m_preference.ManuallySetMainWindowDimension
                = checkBoxManuallySetMainWindowDimension.IsChecked == true;

            m_preference.StorePlaylistContent
                = checkBoxStorePlaylistContent.IsChecked == true;

            m_preference.DispCoverart
                = checkBoxCoverart.IsChecked == true;

            m_preference.RefrainRedraw
                = checkBoxRefrainRedraw.IsChecked == true;

            m_preference.ParallelRead
                = checkBoxParallelRead.IsChecked == true;

            if (comboBoxTimePeriod.SelectedItem == cbItemTimerResolution500Microsec) {
                m_preference.TimePeriodHundredNanosec = 5000;
            } else if (comboBoxTimePeriod.SelectedItem == cbItemTimerResolution1Millisec) {
                m_preference.TimePeriodHundredNanosec = 10000;
            } else {
                m_preference.TimePeriodHundredNanosec = 0;
            }

            m_preference.WindowScale = sliderWindowScaling.Value;

            {
                int playingTimeSize;
                if (Int32.TryParse(textBoxPlayingTimeSize.Text, out playingTimeSize)) {
                    if (playingTimeSize <= 0 || 100 < playingTimeSize) {
                        MessageBox.Show("再生時間表示文字の大きさは 1～100の範囲の数字を入力してください。");
                        return;
                    }
                    m_preference.PlayingTimeSize = playingTimeSize;
                } else {
                    MessageBox.Show("再生時間表示文字の大きさは 1～100の範囲の数字を入力してください。");
                }
            }
            {
                double zeroFlushSeconds;
                if (Double.TryParse(textBoxZeroFlushSeconds.Text, out zeroFlushSeconds)) {
                    if (zeroFlushSeconds <= 0 || 1000 < zeroFlushSeconds) {
                        MessageBox.Show("再生前無音送信時間の大きさは 0.0～1000.0の範囲の数字を入力してください。");
                        return;
                    }
                    m_preference.ZeroFlushMillisec = (int)(zeroFlushSeconds * 1000);
                } else {
                    MessageBox.Show("再生前無音送信時間の大きさは 0.0～1000.0の範囲の数字を入力してください。");
                }
            }
            {
                int v;
                if (Int32.TryParse(textBoxConversionQuality.Text, out v)) {
                    if (v <= 0 || 60 < v) {
                        MessageBox.Show("Wasapi Shared Resampler Qualityの大きさは 1～60の範囲の数字を入力してください。");
                        return;
                    }
                    m_preference.ResamplerConversionQuality = v;
                } else {
                    MessageBox.Show("Wasapi Shared Resampler Qualityの大きさは 1～60の範囲の数字を入力してください。");
                }
            }

            m_preference.SootheLimiterApo = (checkBoxSootheLimiterApo.IsChecked == true);

            m_preference.PlayingTimeFontBold = (checkBoxPlayingTimeBold.IsChecked == true);

            if (null != comboBoxPlayingTimeFontNames.SelectedItem) {
                ComboBoxItem item = (ComboBoxItem)comboBoxPlayingTimeFontNames.SelectedItem;
                FontFamily ff = (FontFamily)item.Content;
                m_preference.PlayingTimeFontName = ff.ToString();
            }
            {
                m_preference.AlternatingRowBackground
                    = checkBoxAlternateBackground.IsChecked == true;
                m_preference.AlternatingRowBackgroundArgb
                    = mPlaylistAlternateBackgroundArgb;
            }

            if (true == radioButtonTaskAudio.IsChecked) {
                m_preference.RenderThreadTaskType = RenderThreadTaskType.Audio;
            }
            if (true == radioButtonTaskNone.IsChecked) {
                m_preference.RenderThreadTaskType = RenderThreadTaskType.None;
            }
            if (true == radioButtonTaskPlayback.IsChecked) {
                m_preference.RenderThreadTaskType = RenderThreadTaskType.Playback;
            }
            if (true == radioButtonTaskProAudio.IsChecked) {
                m_preference.RenderThreadTaskType = RenderThreadTaskType.ProAudio;
            }

            if (0 <= comboBoxCueEncoding.SelectedIndex) {
                var encodingInfoArray = Encoding.GetEncodings();
                if (comboBoxCueEncoding.SelectedIndex < encodingInfoArray.Length) {
                    var encodingInfo = encodingInfoArray[comboBoxCueEncoding.SelectedIndex];
                    m_preference.CueEncodingCodePage = encodingInfo.CodePage;
                }
            }

            Close();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e) {
            Close();
        }

        private void buttonScale1X_Click(object sender, RoutedEventArgs e) {
            sliderWindowScaling.Value = 1.0;
        }

        private void sliderWindowScaling_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (null != labelWindowScaling) {
                labelWindowScaling.Content = string.Format(CultureInfo.InvariantCulture, "{0:0.00}", e.NewValue);
            }
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e) {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) ||
                Keyboard.IsKeyDown(Key.RightCtrl)) {
                // CTRL + マウスホイールで画面のスケーリング

                double scaling = sliderWindowScaling.Value;
                if (e.Delta < 0) {
                    // 1.25の128乗根
                    scaling /= 1.001744829441175331741294013303;
                } else {
                    scaling *= 1.001744829441175331741294013303;
                }
                sliderWindowScaling.Value = scaling;
            }
        }

        private void comboBoxPlayingTimeFontNames_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (null == labelPlayingTime) {
                return;
            }

            ComboBox cb = sender as ComboBox;
            if (null == cb) {
                return;
            }
            ComboBoxItem item = cb.SelectedItem as ComboBoxItem;
            if (null == item) {
                return;
            }

            labelPlayingTime.FontFamily = (FontFamily)item.Content;
        }

        private void checkBoxPlayingTimeBold_Checked(object sender, RoutedEventArgs e) {
            if (null != labelPlayingTime) {
                labelPlayingTime.FontWeight = FontWeights.Bold;
            }
        }

        private void checkBoxPlayingTimeBold_Unchecked(object sender, RoutedEventArgs e) {
            if (null != labelPlayingTime) {
                labelPlayingTime.FontWeight = FontWeights.Normal;
            }
        }

        private void textBoxPlayingTimeSize_TextChanged(object sender, TextChangedEventArgs e) {
            if (null == labelPlayingTime) {
                return;
            }

            int fontSize;
            if (!Int32.TryParse(textBoxPlayingTimeSize.Text, out fontSize)) {
                return;
            }
            if (0 < fontSize && fontSize <= 100) {
                labelPlayingTime.FontSize = fontSize;
            }
        }

        private void buttonReset_Click(object sender, RoutedEventArgs e) {
            var preference = new Preference();
            UpdateUIFromPreference(preference);
        }

        private void buttonChangeColor_Click(object sender, RoutedEventArgs e) {
            System.Windows.Forms.ColorDialog d = new System.Windows.Forms.ColorDialog();
            d.Color = System.Drawing.Color.FromArgb((int)mPlaylistAlternateBackgroundArgb);
            System.Windows.Forms.DialogResult result = d.ShowDialog();
            if (result != System.Windows.Forms.DialogResult.OK) {
                return;
            }
            mPlaylistAlternateBackgroundArgb =
                ((uint)d.Color.A << 24) +
                ((uint)d.Color.R << 16) +
                ((uint)d.Color.G << 8) +
                ((uint)d.Color.B << 0);
            rectangleColor.Fill = new SolidColorBrush(
                Util.ColorFromArgb(mPlaylistAlternateBackgroundArgb));
        }

        private void checkBoxAlternateBackground_Checked(object sender, RoutedEventArgs e) {
            rectangleColor.IsEnabled = true;
            buttonChangeColor.IsEnabled = true;
        }

        private void checkBoxAlternateBackground_Unchecked(object sender, RoutedEventArgs e) {
            rectangleColor.IsEnabled = false;
            buttonChangeColor.IsEnabled = false;
        }

        private void rectangleColor_MouseUp(object sender, MouseButtonEventArgs e) {
            buttonChangeColor_Click(sender, e);
        }
    }
}
