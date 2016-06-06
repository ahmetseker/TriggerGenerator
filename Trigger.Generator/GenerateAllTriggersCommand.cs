using System.Data;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Trigger.Generator
{
    public class GenerateAllTriggersCommand : SmartRoutedCommand
    {
        public static readonly ICommand Instance = new GenerateAllTriggersCommand();

        private GenerateAllTriggersCommand()
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

            MessageBoxResult result =
                MessageBox.Show(wnd, AppStrings.Generate_Trigger_For_All_Tables,
                AppStrings.Question,
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question,
                MessageBoxResult.Cancel);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            TriggerGenerator triggerGenerator = new TriggerGenerator();

            foreach (var lvwItem in lvw.Items)
            {
                DataRowView drw = lvwItem as DataRowView;

                string ownerName = drw.Row.Field<string>(0);
                string tableName = drw.Row.Field<string>(1);

                triggerGenerator.GenerateAndExecute(ownerName, tableName);
            }

            MessageBox.Show(wnd, AppStrings.Trigger_Generation_Completed,
                AppStrings.Information,
                MessageBoxButton.OK,
                MessageBoxImage.Information,
                MessageBoxResult.OK);

        }

    }

}
