using System.Windows.Controls;
using System.Windows.Input;

namespace Trigger.Generator
{
    public class ListTableCommand : SmartRoutedCommand
    {
        public static readonly ICommand Instance = new ListTableCommand();

        private ListTableCommand()
        {
        }

        protected override bool CanExecuteCore(object parameter)
        {
            if (parameter is ListView)
                return true;

            return true;
        }

        protected override void ExecuteCore(object parameter)
        {
            ListView lvw = parameter as ListView;

            var dtTableList = DataManager.GetTableList();
            lvw.DataContext = dtTableList.DefaultView;
        }

    }
}
