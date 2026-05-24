using PS4_PS5download0TOOL.Services;
using System.Text;

namespace PS4_PS5download0TOOL
{
    public partial class MainPage : ContentPage
    {
        private readonly DatProcessingService _service = new();
        private readonly StringBuilder _logBuffer = new();
        private readonly List<LogEntry> _logEntries = [];
        private readonly List<LanguageOption> _languages =
        [
            new("pt-BR", "Português (Brasil)", FlowDirection.LeftToRight),
            new("en", "English", FlowDirection.LeftToRight),
            new("es", "Español", FlowDirection.LeftToRight),
            new("ar", "العربية", FlowDirection.RightToLeft),
        ];

        private CancellationTokenSource? _cts;
        private TaskCompletionSource? _alertCompletion;
        private string _languageCode = "pt-BR";

        private static readonly Dictionary<string, Dictionary<string, string>> UiText = new()
        {
            ["pt-BR"] = new()
            {
                ["Title"] = "PS4/PS5 download0.dat Tool",
                ["Subtitle"] = "Extraia, edite e recrie imagens download0.dat de PS4 e PS5",
                ["LanguageTitle"] = "IDIOMA",
                ["SourceTitle"] = "DAT ORIGINAL (SÓ PARA EXTRAIR)",
                ["SourcePlaceholder"] = "Importe o download0.dat apenas quando for extrair",
                ["ImportButton"] = "Importar",
                ["OutputTitle"] = "PASTA DE TRABALHO / SAÍDA",
                ["OutputPlaceholder"] = "Escolha a pasta dat_output ou a pasta extracted",
                ["FolderButton"] = "Pasta",
                ["ExtractTitle"] = "EXTRAIR",
                ["ExtractCaption"] = "Lê a imagem UFS e grava os arquivos na pasta extracted.",
                ["PatchTitle"] = "RECRIAR .DAT",
                ["PatchCaption"] = "Recria um novo .dat com os arquivos editados, mesmo maiores ou menores.",
                ["ProgressTitle"] = "PROGRESSO",
                ["LogTitle"] = "LOG DE OPERAÇÃO",
                ["ClearLogButton"] = "Limpar",
                ["ExtractButton"] = "Extrair",
                ["PatchButton"] = "Recriar .dat",
                ["CancelButton"] = "Cancelar",
                ["ResetButton"] = "Resetar",
                ["OkButton"] = "OK",
                ["Ready"] = "Pronto.",
                ["InitialLog1"] = "PS4/PS5 download0.dat Tool iniciado.",
                ["InitialLog2"] = "Importe o arquivo, escolha a pasta e execute uma operação.",
                ["PickerTitle"] = "Selecionar download0.dat",
                ["FileInfo"] = "Tamanho: {0}  |  Modificado: {1}  |  Extensão: {2}",
                ["SelectedFile"] = "Arquivo importado: {0} ({1})",
                ["OutputSelected"] = "Pasta para salvar: {0}",
                ["FolderPickerUnavailableTitle"] = "Seletor de pasta",
                ["FolderPickerUnavailable"] = "O seletor de pasta está disponível apenas no Windows. Digite o caminho manualmente.",
                ["SelectInputTitle"] = "Aviso",
                ["SelectInputMessage"] = "Importe um arquivo download0.dat.",
                ["InputMissingTitle"] = "Erro",
                ["InputMissingMessage"] = "Arquivo de origem não encontrado.",
                ["SelectOutputTitle"] = "Aviso",
                ["SelectOutputMessage"] = "Escolha a pasta de trabalho. Para recriar, selecione dat_output ou extracted.",
                ["OutputCreateTitle"] = "Erro",
                ["OutputCreateMessage"] = "Não foi possível criar a pasta para salvar:\n{0}",
                ["PatchNeedsExtractedTitle"] = "Pasta extracted ausente",
                ["PatchNeedsExtractedMessage"] = "Extraia o arquivo primeiro ou coloque os arquivos editados em:\n{0}",
                ["RunHeaderExtract"] = "INICIANDO EXTRAÇÃO",
                ["RunHeaderPatch"] = "INICIANDO REBUILD DO .DAT",
                ["DoneTitle"] = "Concluído",
                ["ExtractDoneMessage"] = "Extração finalizada com sucesso.",
                ["PatchDoneMessage"] = "Novo arquivo rebuilt_download0.dat gerado com sucesso.",
                ["CancelledLog"] = "Operação cancelada.",
                ["CancelledStatus"] = "Cancelado.",
                ["CancelRequested"] = "Cancelamento solicitado...",
                ["CriticalError"] = "Falha durante o processamento:\n{0}",
                ["CriticalLog"] = "[ERRO] {0}",
                ["ResetLog"] = "Campos reiniciados.",
            },
            ["en"] = new()
            {
                ["Title"] = "PS4/PS5 download0.dat Tool",
                ["Subtitle"] = "Extract, edit and rebuild PS4/PS5 download0.dat images",
                ["LanguageTitle"] = "LANGUAGE",
                ["SourceTitle"] = "ORIGINAL DAT (EXTRACT ONLY)",
                ["SourcePlaceholder"] = "Import download0.dat only when extracting",
                ["ImportButton"] = "Import",
                ["OutputTitle"] = "WORK / OUTPUT FOLDER",
                ["OutputPlaceholder"] = "Choose the dat_output folder or the extracted folder",
                ["FolderButton"] = "Folder",
                ["ExtractTitle"] = "EXTRACT",
                ["ExtractCaption"] = "Reads the UFS image and writes files to the extracted folder.",
                ["PatchTitle"] = "REBUILD .DAT",
                ["PatchCaption"] = "Rebuilds a new .dat with edited files, even bigger or smaller.",
                ["ProgressTitle"] = "PROGRESS",
                ["LogTitle"] = "OPERATION LOG",
                ["ClearLogButton"] = "Clear",
                ["ExtractButton"] = "Extract",
                ["PatchButton"] = "Rebuild .dat",
                ["CancelButton"] = "Cancel",
                ["ResetButton"] = "Reset",
                ["OkButton"] = "OK",
                ["Ready"] = "Ready.",
                ["InitialLog1"] = "PS4/PS5 download0.dat Tool started.",
                ["InitialLog2"] = "Import the file, choose a folder and run an operation.",
                ["PickerTitle"] = "Select download0.dat",
                ["FileInfo"] = "Size: {0}  |  Modified: {1}  |  Extension: {2}",
                ["SelectedFile"] = "Imported file: {0} ({1})",
                ["OutputSelected"] = "Save folder: {0}",
                ["FolderPickerUnavailableTitle"] = "Folder picker",
                ["FolderPickerUnavailable"] = "The folder picker is available only on Windows. Type the path manually.",
                ["SelectInputTitle"] = "Warning",
                ["SelectInputMessage"] = "Import a download0.dat file.",
                ["InputMissingTitle"] = "Error",
                ["InputMissingMessage"] = "Source file not found.",
                ["SelectOutputTitle"] = "Warning",
                ["SelectOutputMessage"] = "Choose the work folder. To rebuild, select dat_output or extracted.",
                ["OutputCreateTitle"] = "Error",
                ["OutputCreateMessage"] = "Could not create the save folder:\n{0}",
                ["PatchNeedsExtractedTitle"] = "Missing extracted folder",
                ["PatchNeedsExtractedMessage"] = "Extract the file first or place edited files in:\n{0}",
                ["RunHeaderExtract"] = "STARTING EXTRACTION",
                ["RunHeaderPatch"] = "STARTING .DAT REBUILD",
                ["DoneTitle"] = "Done",
                ["ExtractDoneMessage"] = "Extraction finished successfully.",
                ["PatchDoneMessage"] = "rebuilt_download0.dat was created successfully.",
                ["CancelledLog"] = "Operation cancelled.",
                ["CancelledStatus"] = "Cancelled.",
                ["CancelRequested"] = "Cancellation requested...",
                ["CriticalError"] = "Processing failed:\n{0}",
                ["CriticalLog"] = "[ERROR] {0}",
                ["ResetLog"] = "Fields reset.",
            },
            ["es"] = new()
            {
                ["Title"] = "PS4/PS5 download0.dat Tool",
                ["Subtitle"] = "Extrae, edita y reconstruye imágenes download0.dat de PS4 y PS5",
                ["LanguageTitle"] = "IDIOMA",
                ["SourceTitle"] = "DAT ORIGINAL (SOLO EXTRAER)",
                ["SourcePlaceholder"] = "Importa download0.dat solo para extraer",
                ["ImportButton"] = "Importar",
                ["OutputTitle"] = "CARPETA DE TRABAJO / SALIDA",
                ["OutputPlaceholder"] = "Elige la carpeta dat_output o extracted",
                ["FolderButton"] = "Carpeta",
                ["ExtractTitle"] = "EXTRAER",
                ["ExtractCaption"] = "Lee la imagen UFS y guarda los archivos en la carpeta extracted.",
                ["PatchTitle"] = "RECREAR .DAT",
                ["PatchCaption"] = "Reconstruye un nuevo .dat con archivos editados, mayores o menores.",
                ["ProgressTitle"] = "PROGRESO",
                ["LogTitle"] = "REGISTRO DE OPERACIÓN",
                ["ClearLogButton"] = "Limpiar",
                ["ExtractButton"] = "Extraer",
                ["PatchButton"] = "Recrear .dat",
                ["CancelButton"] = "Cancelar",
                ["ResetButton"] = "Reiniciar",
                ["OkButton"] = "OK",
                ["Ready"] = "Listo.",
                ["InitialLog1"] = "PS4/PS5 download0.dat Tool iniciado.",
                ["InitialLog2"] = "Importa el archivo, elige la carpeta y ejecuta una operación.",
                ["PickerTitle"] = "Seleccionar download0.dat",
                ["FileInfo"] = "Tamaño: {0}  |  Modificado: {1}  |  Extensión: {2}",
                ["SelectedFile"] = "Archivo importado: {0} ({1})",
                ["OutputSelected"] = "Carpeta para guardar: {0}",
                ["FolderPickerUnavailableTitle"] = "Selector de carpeta",
                ["FolderPickerUnavailable"] = "El selector de carpeta solo está disponible en Windows. Escribe la ruta manualmente.",
                ["SelectInputTitle"] = "Aviso",
                ["SelectInputMessage"] = "Importa un archivo download0.dat.",
                ["InputMissingTitle"] = "Error",
                ["InputMissingMessage"] = "Archivo de origen no encontrado.",
                ["SelectOutputTitle"] = "Aviso",
                ["SelectOutputMessage"] = "Elige la carpeta de trabajo. Para recrear, selecciona dat_output o extracted.",
                ["OutputCreateTitle"] = "Error",
                ["OutputCreateMessage"] = "No se pudo crear la carpeta:\n{0}",
                ["PatchNeedsExtractedTitle"] = "Falta la carpeta extracted",
                ["PatchNeedsExtractedMessage"] = "Extrae el archivo primero o coloca los archivos editados en:\n{0}",
                ["RunHeaderExtract"] = "INICIANDO EXTRACCIÓN",
                ["RunHeaderPatch"] = "INICIANDO REBUILD DEL .DAT",
                ["DoneTitle"] = "Completado",
                ["ExtractDoneMessage"] = "Extracción finalizada correctamente.",
                ["PatchDoneMessage"] = "rebuilt_download0.dat se creó correctamente.",
                ["CancelledLog"] = "Operación cancelada.",
                ["CancelledStatus"] = "Cancelado.",
                ["CancelRequested"] = "Cancelación solicitada...",
                ["CriticalError"] = "Error durante el procesamiento:\n{0}",
                ["CriticalLog"] = "[ERROR] {0}",
                ["ResetLog"] = "Campos reiniciados.",
            },
            ["ar"] = new()
            {
                ["Title"] = "أداة PS4/PS5 download0.dat",
                ["Subtitle"] = "استخراج وتعديل وإعادة بناء صور PS4/PS5 download0.dat",
                ["LanguageTitle"] = "اللغة",
                ["SourceTitle"] = "ملف DAT الأصلي (للاستخراج فقط)",
                ["SourcePlaceholder"] = "استورد download0.dat عند الاستخراج فقط",
                ["ImportButton"] = "استيراد",
                ["OutputTitle"] = "مجلد العمل / الإخراج",
                ["OutputPlaceholder"] = "اختر مجلد dat_output أو extracted",
                ["FolderButton"] = "مجلد",
                ["ExtractTitle"] = "استخراج",
                ["ExtractCaption"] = "يقرأ صورة UFS ويحفظ الملفات في مجلد extracted.",
                ["PatchTitle"] = "إعادة بناء .dat",
                ["PatchCaption"] = "يعيد بناء ملف .dat جديد بالملفات المعدلة، أكبر أو أصغر.",
                ["ProgressTitle"] = "التقدم",
                ["LogTitle"] = "سجل العملية",
                ["ClearLogButton"] = "مسح",
                ["ExtractButton"] = "استخراج",
                ["PatchButton"] = "إعادة بناء .dat",
                ["CancelButton"] = "إلغاء",
                ["ResetButton"] = "إعادة",
                ["OkButton"] = "حسنا",
                ["Ready"] = "جاهز.",
                ["InitialLog1"] = "تم تشغيل أداة PS4/PS5 download0.dat.",
                ["InitialLog2"] = "استورد الملف، اختر المجلد، ثم نفذ العملية.",
                ["PickerTitle"] = "اختيار download0.dat",
                ["FileInfo"] = "الحجم: {0}  |  التعديل: {1}  |  الامتداد: {2}",
                ["SelectedFile"] = "تم استيراد الملف: {0} ({1})",
                ["OutputSelected"] = "مجلد الحفظ: {0}",
                ["FolderPickerUnavailableTitle"] = "اختيار المجلد",
                ["FolderPickerUnavailable"] = "اختيار المجلد متاح فقط على Windows. اكتب المسار يدويا.",
                ["SelectInputTitle"] = "تنبيه",
                ["SelectInputMessage"] = "استورد ملف download0.dat.",
                ["InputMissingTitle"] = "خطأ",
                ["InputMissingMessage"] = "ملف المصدر غير موجود.",
                ["SelectOutputTitle"] = "تنبيه",
                ["SelectOutputMessage"] = "اختر مجلد العمل. لإعادة البناء، اختر dat_output أو extracted.",
                ["OutputCreateTitle"] = "خطأ",
                ["OutputCreateMessage"] = "تعذر إنشاء مجلد الحفظ:\n{0}",
                ["PatchNeedsExtractedTitle"] = "مجلد extracted غير موجود",
                ["PatchNeedsExtractedMessage"] = "استخرج الملف أولا أو ضع الملفات المعدلة في:\n{0}",
                ["RunHeaderExtract"] = "بدء الاستخراج",
                ["RunHeaderPatch"] = "بدء إعادة بناء .dat",
                ["DoneTitle"] = "تم",
                ["ExtractDoneMessage"] = "اكتمل الاستخراج بنجاح.",
                ["PatchDoneMessage"] = "تم إنشاء rebuilt_download0.dat بنجاح.",
                ["CancelledLog"] = "تم إلغاء العملية.",
                ["CancelledStatus"] = "ملغى.",
                ["CancelRequested"] = "تم طلب الإلغاء...",
                ["CriticalError"] = "فشلت المعالجة:\n{0}",
                ["CriticalLog"] = "[خطأ] {0}",
                ["ResetLog"] = "تمت إعادة الحقول.",
            },
        };

        public MainPage()
        {
            InitializeComponent();
            LanguagePicker.ItemsSource = _languages.Select(language => language.Name).ToList();
            LanguagePicker.SelectedIndex = 0;
            ApplyLanguage();
            LogLocalized("InitialLog1");
            LogLocalized("InitialLog2");
        }

        private async void OnBrowseInputClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = T("PickerTitle"),
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.WinUI, [".dat"] },
                        { DevicePlatform.macOS, ["public.item"] },
                        { DevicePlatform.iOS, ["public.item"] },
                        { DevicePlatform.Android, ["application/octet-stream"] },
                    })
                });

                if (result is null || string.IsNullOrWhiteSpace(result.FullPath))
                    return;

                InputPathEntry.Text = result.FullPath;

                var fileInfo = new FileInfo(result.FullPath);
                InputFileInfoLabel.Text = string.Format(
                    T("FileInfo"),
                    FormatSize(fileInfo.Length),
                    fileInfo.LastWriteTime.ToString("dd/MM/yyyy HH:mm"),
                    string.IsNullOrWhiteSpace(fileInfo.Extension) ? ".dat" : fileInfo.Extension.ToUpperInvariant());
                InputFileInfoLabel.IsVisible = true;

                if (string.IsNullOrWhiteSpace(OutputPathEntry.Text) && fileInfo.DirectoryName is not null)
                    OutputPathEntry.Text = Path.Combine(fileInfo.DirectoryName, "dat_output");

                LogLocalized("SelectedFile", result.FileName, FormatSize(fileInfo.Length));
            }
            catch (Exception ex)
            {
                await ShowCyberAlertAsync(T("InputMissingTitle"), ex.Message);
                LogLocalized("CriticalLog", ex.Message);
            }
        }

        private async void OnBrowseOutputClicked(object sender, EventArgs e)
        {
            try
            {
#if WINDOWS
                var picker = new Windows.Storage.Pickers.FolderPicker();
                picker.FileTypeFilter.Add("*");

                var window = Application.Current!.Windows[0];
                var hwnd = ((MauiWinUIWindow)window.Handler.PlatformView!).WindowHandle;
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                var folder = await picker.PickSingleFolderAsync();
                if (folder is not null)
                {
                    OutputPathEntry.Text = folder.Path;
                    LogLocalized("OutputSelected", folder.Path);
                }
#else
                await ShowCyberAlertAsync(
                    T("FolderPickerUnavailableTitle"),
                    T("FolderPickerUnavailable"));
#endif
            }
            catch (Exception ex)
            {
                await ShowCyberAlertAsync(T("OutputCreateTitle"), ex.Message);
                LogLocalized("CriticalLog", ex.Message);
            }
        }

        private async void OnExtractClicked(object sender, EventArgs e)
        {
            await RunOperationAsync(ProcessingMode.Extract);
        }

        private async void OnPatchClicked(object sender, EventArgs e)
        {
            await RunOperationAsync(ProcessingMode.Patch);
        }

        private async Task RunOperationAsync(ProcessingMode mode)
        {
            if (!await ValidatePathsAsync(mode))
                return;

            SetRunningState(true);
            SetProgress(0, T("Ready"));
            _cts = new CancellationTokenSource();

            var options = new ProcessingOptions
            {
                InputPath = InputPathEntry.Text ?? string.Empty,
                OutputPath = OutputPathEntry.Text ?? string.Empty,
                Mode = mode,
                LanguageCode = _languageCode
            };

            var progress = new Progress<ProgressInfo>(info =>
            {
                SetProgress(info.Percentage, string.IsNullOrWhiteSpace(info.Status) ? StatusLabel.Text : info.Status);
                if (!string.IsNullOrWhiteSpace(info.LogMessage))
                    Log(info.LogMessage);
            });

            Log(new string('=', 42));
            LogLocalized(mode == ProcessingMode.Extract ? "RunHeaderExtract" : "RunHeaderPatch");
            Log(new string('=', 42));

            try
            {
                await _service.ProcessAsync(options, progress, _cts.Token);
                await ShowCyberAlertAsync(
                    T("DoneTitle"),
                    T(mode == ProcessingMode.Extract ? "ExtractDoneMessage" : "PatchDoneMessage"));
            }
            catch (OperationCanceledException)
            {
                LogLocalized("CancelledLog");
                SetProgress(0, T("CancelledStatus"));
            }
            catch (Exception ex)
            {
                LogLocalized("CriticalLog", ex.Message);
                await ShowCyberAlertAsync(T("InputMissingTitle"), string.Format(T("CriticalError"), ex.Message));
            }
            finally
            {
                SetRunningState(false);
                _cts?.Dispose();
                _cts = null;
            }
        }

        private async Task<bool> ValidatePathsAsync(ProcessingMode mode)
        {
            if (mode == ProcessingMode.Extract && string.IsNullOrWhiteSpace(InputPathEntry.Text))
            {
                await ShowCyberAlertAsync(T("SelectInputTitle"), T("SelectInputMessage"));
                return false;
            }

            if (mode == ProcessingMode.Extract && !File.Exists(InputPathEntry.Text))
            {
                await ShowCyberAlertAsync(T("InputMissingTitle"), T("InputMissingMessage"));
                return false;
            }

            if (string.IsNullOrWhiteSpace(OutputPathEntry.Text))
            {
                await ShowCyberAlertAsync(T("SelectOutputTitle"), T("SelectOutputMessage"));
                return false;
            }

            try
            {
                Directory.CreateDirectory(OutputPathEntry.Text);
            }
            catch (Exception ex)
            {
                await ShowCyberAlertAsync(T("OutputCreateTitle"), string.Format(T("OutputCreateMessage"), ex.Message));
                return false;
            }

            if (mode == ProcessingMode.Patch)
            {
                NormalizeRebuildFolderSelection();

                var extractedFolder = Path.Combine(OutputPathEntry.Text, "extracted");
                if (!Directory.Exists(extractedFolder))
                {
                    await ShowCyberAlertAsync(
                        T("PatchNeedsExtractedTitle"),
                        string.Format(T("PatchNeedsExtractedMessage"), extractedFolder));
                    return false;
                }
            }

            return true;
        }

        private void NormalizeRebuildFolderSelection()
        {
            var selectedPath = OutputPathEntry.Text;
            if (string.IsNullOrWhiteSpace(selectedPath))
                return;

            var selectedDirectory = new DirectoryInfo(selectedPath);
            if (!selectedDirectory.Exists)
                return;

            if (selectedDirectory.Name.Equals("extracted", StringComparison.OrdinalIgnoreCase) &&
                selectedDirectory.Parent is not null)
            {
                OutputPathEntry.Text = selectedDirectory.Parent.FullName;
            }
        }

        private void OnCancelClicked(object sender, EventArgs e)
        {
            _cts?.Cancel();
            LogLocalized("CancelRequested");
        }

        private void OnClearLogClicked(object sender, EventArgs e)
        {
            _logBuffer.Clear();
            _logEntries.Clear();
            LogEditor.Text = string.Empty;
        }

        private void OnResetClicked(object sender, EventArgs e)
        {
            InputPathEntry.Text = string.Empty;
            OutputPathEntry.Text = string.Empty;
            InputFileInfoLabel.Text = string.Empty;
            InputFileInfoLabel.IsVisible = false;
            SetProgress(0, T("Ready"));
            _logBuffer.Clear();
            _logEntries.Clear();
            LogEditor.Text = string.Empty;
            LogLocalized("ResetLog");
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            if (LanguagePicker.SelectedIndex < 0 || LanguagePicker.SelectedIndex >= _languages.Count)
                return;

            var selectedLanguage = _languages[LanguagePicker.SelectedIndex];
            _languageCode = selectedLanguage.Code;
            FlowDirection = selectedLanguage.FlowDirection;
            ApplyLanguage();
            RenderLog();
        }

        private void ApplyLanguage()
        {
            TitleLabel.Text = T("Title");
            SubtitleLabel.Text = T("Subtitle");
            LanguageTitleLabel.Text = T("LanguageTitle");
            SourceTitleLabel.Text = T("SourceTitle");
            InputPathEntry.Placeholder = T("SourcePlaceholder");
            ImportButton.Text = T("ImportButton");
            OutputTitleLabel.Text = T("OutputTitle");
            OutputPathEntry.Placeholder = T("OutputPlaceholder");
            FolderButton.Text = T("FolderButton");
            ExtractTitleLabel.Text = T("ExtractTitle");
            ExtractCaptionLabel.Text = T("ExtractCaption");
            PatchTitleLabel.Text = T("PatchTitle");
            PatchCaptionLabel.Text = T("PatchCaption");
            ProgressTitleLabel.Text = T("ProgressTitle");
            LogTitleLabel.Text = T("LogTitle");
            ClearLogButton.Text = T("ClearLogButton");
            ExtractButton.Text = T("ExtractButton");
            PatchButton.Text = T("PatchButton");
            CancelButton.Text = T("CancelButton");
            ResetButton.Text = T("ResetButton");
            AlertOkButton.Text = T("OkButton");

            if (_cts is null)
                StatusLabel.Text = T("Ready");
        }

        private async Task ShowCyberAlertAsync(string title, string message)
        {
            if (_alertCompletion is not null)
                _alertCompletion.TrySetResult();

            _alertCompletion = new TaskCompletionSource();
            AlertTitleLabel.Text = title;
            AlertMessageLabel.Text = message;
            AlertOkButton.Text = T("OkButton");
            AlertOverlay.IsVisible = true;
            AlertOverlay.Opacity = 0;
            AlertCard.Scale = 0.96;
            AlertCard.Opacity = 0;

            await Task.WhenAll(
                AlertOverlay.FadeTo(1, 120, Easing.CubicOut),
                AlertCard.FadeTo(1, 140, Easing.CubicOut),
                AlertCard.ScaleTo(1, 140, Easing.CubicOut));

            await _alertCompletion.Task;

            await Task.WhenAll(
                AlertCard.FadeTo(0, 90, Easing.CubicIn),
                AlertCard.ScaleTo(0.97, 90, Easing.CubicIn),
                AlertOverlay.FadeTo(0, 110, Easing.CubicIn));

            AlertOverlay.IsVisible = false;
            _alertCompletion = null;
        }

        private void OnAlertOkClicked(object sender, EventArgs e)
        {
            _alertCompletion?.TrySetResult();
        }

        private void SetRunningState(bool running)
        {
            ExtractButton.IsEnabled = !running;
            PatchButton.IsEnabled = !running;
            CancelButton.IsEnabled = running;
            ImportButton.IsEnabled = !running;
            FolderButton.IsEnabled = !running;
            ResetButton.IsEnabled = !running;
            LanguagePicker.IsEnabled = !running;
        }

        private void SetProgress(double percent, string status)
        {
            var clampedPercent = Math.Clamp(percent, 0, 100);
            MainProgressBar.Progress = clampedPercent / 100.0;
            ProgressPercentLabel.Text = $"{clampedPercent:F0}%";
            StatusLabel.Text = status;
        }

        private void Log(string message)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _logEntries.Add(LogEntry.Raw(DateTime.Now, message));
                RenderLog();
            });
        }

        private void LogLocalized(string key, params object[] args)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _logEntries.Add(LogEntry.Localized(DateTime.Now, key, args));
                RenderLog();
            });
        }

        private void RenderLog()
        {
            _logBuffer.Clear();

            foreach (var entry in _logEntries)
            {
                var message = entry.LocalizedKey is null
                    ? entry.Message ?? string.Empty
                    : FormatLocalizedLog(entry.LocalizedKey, entry.Arguments);

                _logBuffer.AppendLine($"[{entry.Timestamp:HH:mm:ss}] {message}");
            }

            LogEditor.Text = _logBuffer.ToString();
        }

        private string FormatLocalizedLog(string key, IReadOnlyList<object> arguments)
        {
            var template = T(key);
            return arguments.Count == 0 ? template : string.Format(template, arguments.ToArray());
        }

        private string T(string key)
        {
            if (UiText.TryGetValue(_languageCode, out var language) && language.TryGetValue(key, out var value))
                return value;

            return UiText["en"].TryGetValue(key, out var fallback) ? fallback : key;
        }

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
            return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
        }

        private sealed record LanguageOption(string Code, string Name, FlowDirection FlowDirection);

        private sealed record LogEntry(
            DateTime Timestamp,
            string? Message,
            string? LocalizedKey,
            IReadOnlyList<object> Arguments)
        {
            public static LogEntry Raw(DateTime timestamp, string message)
                => new(timestamp, message, null, []);

            public static LogEntry Localized(DateTime timestamp, string key, IReadOnlyList<object> arguments)
                => new(timestamp, null, key, arguments.ToArray());
        }
    }
}
