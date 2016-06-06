using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Trigger.Generator
{
    public class GenerateTriggersWithFilterCommand : SmartRoutedCommand
    {
        public static readonly ICommand Instance = new GenerateTriggersWithFilterCommand();

        private GenerateTriggersWithFilterCommand()
        {
        }

        protected override bool CanExecuteCore(object parameter)
        {
            if (parameter == null)
                return false;

            if (parameter is ListView)
                return true;

            ListView lvw = parameter as ListView;

            if (lvw.SelectedItem != null)
                return true;

            return false;
        }

        private Window GetTopParent(Control control)
        {
            DependencyObject dpParent = control.Parent;

            do
            {
                dpParent = LogicalTreeHelper.GetParent(dpParent);
            } while (dpParent.GetType().BaseType != typeof(Window));

            return dpParent as Window;
        }

        protected override void ExecuteCore(object parameter)
        {
            ListView lvw = parameter as ListView;
            Window wnd = (Window)GetTopParent(lvw);

            string tableFilterSql = ConfigurationManager.AppSettings[SettingNames.TableFilterSql];

            if (string.IsNullOrWhiteSpace(tableFilterSql))
            {
                MessageBox.Show(wnd, AppStrings.Table_Filter_Sql_Is_Not_Defined,
                    AppStrings.Warning,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            MessageBoxResult result =
                MessageBox.Show(wnd, AppStrings.Generate_Trigger_For_Selected_Tables,
                AppStrings.Question,
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question,
                MessageBoxResult.Cancel);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            var dataTable = DataManager.ExecuteCommand(tableFilterSql);

            var query = from dr in dataTable.Rows.OfType<DataRow>()
                        select new
                        {
                            TableName = dr.Field<string>("TABLE_NAME"),
                        };

            var tableList = query.ToDictionary(z => z.TableName, x => x.TableName);

            TriggerGenerator tg = new TriggerGenerator();

            foreach (var lvwItem in lvw.Items)
            {
                DataRowView drw = lvwItem as DataRowView;

                string ownerName = drw.Row.Field<string>(0);
                string tableName = drw.Row.Field<string>(1);

                if (tableList.ContainsKey(tableName))
                {
                    tg.GenerateAndExecute(ownerName, tableName);

                }
            }

            MessageBox.Show(wnd, AppStrings.Trigger_Generation_Completed,
                AppStrings.Information,
                MessageBoxButton.OK,
                MessageBoxImage.Information,
                MessageBoxResult.OK);

        }

    }

}
