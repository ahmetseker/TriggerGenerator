using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Trigger.Generator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            ListTableCommand.Instance.Execute(lvwTables);
            lvwTables.Focus();

            string connectionString = ConfigurationManager.ConnectionStrings[SettingNames.Orcl].ConnectionString;

            var orclSb = new DbConnectionStringBuilder();
            orclSb.ConnectionString = connectionString;

            var dataSource = orclSb[KeyNames.DataSource];
            var userID = orclSb[KeyNames.UserID];

            var databaseInfo = new StringBuilder();
            databaseInfo.AppendFormat(AppStrings.Database_Info, userID, dataSource);
            txtInfo.Text = databaseInfo.ToString();

        }

        private void lvwTables_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lvwTables.SelectedItem == null)
                return;

            FlowDocument fdoc = new FlowDocument();
            Paragraph p1 = new Paragraph();

            DataRowView drw = lvwTables.SelectedItem as DataRowView;
            string tableName = drw.Row.Field<string>(1);
            string ownerName = drw.Row.Field<string>(0);

            txtTableName.Text = tableName;

            TriggerGenerator tg = new TriggerGenerator();

            p1.Inlines.Add(new Run(tg.BuildTrigger(ownerName, tableName)));
            fdoc.Blocks.Add(p1);
            rtblog.Document = fdoc;

        }

    }
}
