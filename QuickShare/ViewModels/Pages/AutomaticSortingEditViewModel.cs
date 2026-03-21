using QuickShare.Helpers;
using QuickShare.Models;
using QuickShare.Services;
using Serilog;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Forms;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;
using Button = Wpf.Ui.Controls.Button;
using TextBlock = Wpf.Ui.Controls.TextBlock;
using TextBox = Wpf.Ui.Controls.TextBox;

namespace QuickShare.ViewModels.Pages
{
    public partial class AutomaticSortingEditViewModel(
        ILogger logger,
        SqliteService sqliteService,
        ISnackbarService snackbarService,
        AppConfigService appConfigService,
        MessageBoxService messageBoxService,
        INavigationService navigationService) : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        [ObservableProperty]
        private ControlAppearance _modifyBtnAppearance = ControlAppearance.Secondary;

        [ObservableProperty]
        private bool _modifyBtnEnabled = false;

        [ObservableProperty]
        private bool _autoSorting = appConfigService.TransmitConfig.AutoSorting;

        [ObservableProperty]
        private ObservableCollection<SortingRuleModel> _sortingRules = new();

        [ObservableProperty]
        private ObservableCollection<SortingRuleModel> _waitRemoveSortingRules = new();

        partial void OnAutoSortingChanged(bool value)
        {
            ModifyBtnEnabled = true;
            ModifyBtnAppearance = ControlAppearance.Caution;
        }

        [RelayCommand]
        private void OnConfirmModify()
        {
            if (SortingRules.Any(rule => string.IsNullOrEmpty(rule.SortingName) || rule.Extension.Count == 0))
            {
                snackbarService.Show(
                    "提示",
                    "部分规则未设置规则名称或拓展名，请检查。",
                    ControlAppearance.Caution,
                    new SymbolIcon(SymbolRegular.Warning24),
                    TimeSpan.FromSeconds(5));
                return;
            }

            foreach (var sortingRule in WaitRemoveSortingRules)
            {
                sqliteService.RemoveSortingRule(sortingRule.Id);
            }

            var oldSortingRules = sqliteService.ReadAllSortingRules();
            foreach (SortingRuleModel sortingRule in SortingRules)
            {
                bool exists = oldSortingRules.Any(old => old.Id == sortingRule.Id);
                if (exists)
                {
                    sqliteService.UpdateSortingRule(sortingRule);
                }
                else
                {
                    sqliteService.AddSortingRule(sortingRule);
                }
            }
            if (AutoSorting != appConfigService.TransmitConfig.AutoSorting)
            {
                appConfigService.TransmitConfig.AutoSorting = AutoSorting;
                appConfigService.SaveConfig();
            }
            ModifyBtnEnabled = false;
            ModifyBtnAppearance = ControlAppearance.Secondary;
        }

        [RelayCommand]
        private void OnExitEdit() => navigationService.GoBack();

        [RelayCommand]
        private void OnModifySortingName(RoutedEventArgs e)
        {
            if (e.Source is TextBox textBox)
            {
                if (textBox.DataContext is SortingRuleModel sortingModel)
                {
                    sortingModel.SortingName = textBox.Text;
                    ModifyBtnEnabled = true;
                    ModifyBtnAppearance = ControlAppearance.Caution;
                }
            }
        }

        [RelayCommand]
        private void OnSelectSavePath(SortingRuleModel sortingModel)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                string relativePath = appConfigService.TransmitConfig.SavePath + "\\";
                folderDialog.SelectedPath = relativePath;
                try
                {
                    DialogResult result = folderDialog.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        if (!folderDialog.SelectedPath.StartsWith(relativePath))
                        {
                            _ = messageBoxService.ShowMessage("错误", $"只能选择上传目录{relativePath}中的二级目录作为存储路径。");
                            return;
                        }
                        sortingModel.SavePath = "\\" + Path.GetRelativePath(relativePath, folderDialog.SelectedPath);
                        ModifyBtnEnabled = true;
                        ModifyBtnAppearance = ControlAppearance.Caution;
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex.Message);
                    _ = messageBoxService.ShowMessage("错误", ex.Message);
                }
            }
        }

        [RelayCommand]
        private void OnAddExtension(RoutedEventArgs e)
        {
            if (e.Source is TextBox textBox)
            {
                if (string.IsNullOrEmpty(textBox.Text))
                {
                    return;
                }
                if (Regex.IsMatch(textBox.Text, @"[^a-zA-Z0-9.]"))
                {
                    snackbarService.Show(
                        "提示",
                        "无效的拓展名，仅支持字母、数字和英文句号（.）。",
                        ControlAppearance.Caution,
                        new SymbolIcon(SymbolRegular.Warning24),
                        TimeSpan.FromSeconds(5));
                    return;
                }
                if (!textBox.Text.StartsWith("."))
                {
                    snackbarService.Show(
                        "提示",
                        "无效的拓展名，应以 . 作为起始。",
                        ControlAppearance.Caution,
                        new SymbolIcon(SymbolRegular.Warning24),
                        TimeSpan.FromSeconds(5));
                    return;
                }
                var sortingRule = SortingRules.FirstOrDefault(rule => rule.Extension.Any(x => x == textBox.Text.ToLower()));
                if (sortingRule != null)
                {
                    snackbarService.Show(
                        "提示",
                        $"该拓展名已存在于规则 \"{sortingRule.SortingName}\" 中，请重新设置。",
                        ControlAppearance.Caution,
                        new SymbolIcon(SymbolRegular.Warning24),
                        TimeSpan.FromSeconds(5));
                    return;
                }
                if (textBox.DataContext is SortingRuleModel sortingModel)
                {
                    sortingModel.Extension.Add(textBox.Text.ToLower());
                    textBox.Text = "";
                    ModifyBtnEnabled = true;
                    ModifyBtnAppearance = ControlAppearance.Caution;
                }
            }
        }

        [RelayCommand]
        private void OnRemoveExtension(RoutedEventArgs e)
        {
            if (e.Source is Button button)
            {
                if (button.Content is TextBlock textBlock)
                {
                    ItemsControl? itemsControl = CustomHelper.FindVisualParent<ItemsControl>(button);
                    if (itemsControl == null)
                    {
                        return;
                    }
                    SortingRuleModel sortingModel = (SortingRuleModel)itemsControl.DataContext;
                    sortingModel.Extension.Remove(textBlock.Text);
                    ModifyBtnEnabled = true;
                    ModifyBtnAppearance = ControlAppearance.Caution;
                }
            }
        }

        [RelayCommand]
        private void OnRemoveSorting(SortingRuleModel sortingModel)
        {
            if (sortingModel.Id != 0)
            {
                WaitRemoveSortingRules.Add(sortingModel);
            }
            SortingRules.Remove(sortingModel);
            ModifyBtnEnabled = true;
            ModifyBtnAppearance = ControlAppearance.Caution;
        }

        [RelayCommand]
        private void OnAddSorting()
        {
            SortingRuleModel sortingModel = new SortingRuleModel();
            SortingRules.Add(sortingModel);
            ModifyBtnEnabled = true;
            ModifyBtnAppearance = ControlAppearance.Caution;
        }

        #region INavigationAware Members

        public Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
                InitializeViewModel();

            ModifyBtnEnabled = false;
            ModifyBtnAppearance = ControlAppearance.Secondary;
            AutoSorting = appConfigService.TransmitConfig.AutoSorting;
            SortingRules = new ObservableCollection<SortingRuleModel>(sqliteService.ReadAllSortingRules());
            WaitRemoveSortingRules = new();
            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private void InitializeViewModel()
        {
            _isInitialized = true;
        }

        #endregion
    }
}
